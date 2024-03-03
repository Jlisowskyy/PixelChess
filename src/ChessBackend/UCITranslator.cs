#define DEBUG_
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using PixelChess.Figures;

namespace PixelChess.ChessBackend;

public class UciTranslator : IDisposable
{
// --------------------------------------
// Type creation and initialization
// --------------------------------------

    public void Initialize(Board board, string chessEngineDir)
    {
        _board = board;
        _chessEngine = new Process();
        _chessEngine.StartInfo.UseShellExecute = false;
        _chessEngine.StartInfo.RedirectStandardError = true;
        _chessEngine.StartInfo.RedirectStandardInput = true;
        _chessEngine.StartInfo.RedirectStandardOutput = true;
        _chessEngine.StartInfo.FileName = chessEngineDir;

        try
        {
            _chessEngine.Start();
            Console.WriteLine("[ OK ] Correctly started Chess Engine process!");
        }
        catch (Exception exc)
        {
            Console.Error.WriteLine($"[ ERROR ] Not able to startup chess engine! Cause:\n{exc}");
            _chessEngine = null;
            return;
        }

        _messageReceiver = new Thread(_messageReadingThread);
        _messageReceiver.Start();
        
        // Checking UCI compatibility
        var result = _verifyUciCompliance();

        if (!result.isUciCompatible)
        {
            Console.Error.WriteLine("[ ERROR ] Passed engine is not UCI compatible. Terminating engine...\n");
            Dispose();
            _chessEngine = null;
            return;
        }
        
        Console.WriteLine("[ OK ] Established UCI connection between the engine!");
        Console.WriteLine($"Chess engine init output:\n{result.initOutput}");
        SetupNewGame();
    }

    public void SetupNewGame()
    {
        if (!IsOperational) throw new ApplicationException("[ ERROR ] Engine was shut down!");


        lock (_recordQueue) _recordQueue.Clear();
        _chessEngine.StandardInput.WriteLine("stop");
        _chessEngine.StandardInput.WriteLine($"position fen {FenTranslator.Translate(_board)}");
        _chessEngine.StandardInput.WriteLine("ucinewgame");

        if (PingEngine() == false)
        {
            Console.Error.WriteLine("[ ERROR ] The engine has hung. Beginning its termination...");
            Dispose();
            _chessEngine = null;
        }
        else
        {
            Console.WriteLine("[ OK ] Correctly started new game inside the engine!");
            
            // TODO: temporary blind try to change used threads:
            _chessEngine.StandardInput.WriteLine($"setoption name Threads value {Environment.ProcessorCount/2}");
        }
        
    }

    private const int Ping10MsTries = 20;
    public bool PingEngine()
    {
        if (!IsOperational) return false;
        
        _chessEngine.StandardInput.WriteLine("isready");
        for (int i = 0; i < Ping10MsTries; ++i)
        {
            Thread.Sleep(10);

            lock (_recordQueue)
            {
                foreach (var record in _recordQueue)
                {
                    if (record.IndexOf("readyok", StringComparison.Ordinal) != -1)
                        return true;
                }
            }
            
        }
        
        return false;
    }

    public void StartSearch()
    {
        if (!IsOperational) throw new ApplicationException("[ ERROR ] Engine was shut down!");

        StringBuilder builder = new();
        foreach (var move in _board.MovesHistory.Skip(1))
        {
            string mvCode = GetUciMoveCode(move);
            builder.Append(mvCode);
            builder.Append(' ');
        }
        
        _chessEngine.StandardInput.WriteLine($"position startpos moves {builder}");
        _chessEngine.StandardInput.WriteLine($"go movetime {MaxSearchTime}");
        _spentTimeOnSearch = 0;
        _searchTries = 0;
    }

    private const int MaxSearchResultRepeatingTries = 5;
    public string ChecksForSearchResult(GameTime spentTime)
        // walks through all commands available and processes them all
        // until bestmove command is encountered or queue is empty.
    {
        _spentTimeOnSearch += spentTime.ElapsedGameTime.TotalMilliseconds;
        string bestMove = _processRunningCommands();
        
        
        // TODO: temporary disable behavior
        // if (_spentTimeOnSearch > MaxSearchTime && bestMove == String.Empty && ++_searchTries == MaxSearchResultRepeatingTries)
        // {
        //     Console.Error.WriteLine("[ ERROR ] Maximum amount of repeated tries to retrieve bestmove command exceeded. Terminating the engine...");
        //     Dispose();
        //     return "err";
        // }

        return bestMove;
    }

// ------------------------------
// Type methods
// ------------------------------

    public void Dispose()
    {
        if (IsOperational)
        {
            _chessEngine.Kill();
            _chessEngine.WaitForExit();
            _messageReceiver.Join();
            
            Console.WriteLine($"Chess engine stdout uncleaned output:\n{_chessEngine.StandardOutput.ReadToEnd()}");
            Console.WriteLine($"Chess engine uncleaned stderr output:\n{_chessEngine.StandardError.ReadToEnd()}");
            Console.WriteLine("[ OK ] Correctly closed Chess Engine!");
        }
    }
    
// ------------------------------
// Static methods
// ------------------------------

    public static string GetUciMoveCode(HistoricalMove move)
    {
        if ((move.MadeMove.MoveT & BoardPos.MoveType.PromotionMove) == 0)
            return $"{(char)('a' + move.OldX)}{1 + move.OldY}{(char)('a' + move.MadeMove.X)}{1 + move.MadeMove.Y}";

        return $"{(char)('a' + move.OldX)}{1 + move.OldY}{(char)('a' + move.MadeMove.X)}{1 + move.MadeMove.Y}{move.figureUpdateChar}";
    }

    public static string GetUciMoveCode(BoardPos prevPos, BoardPos nextPos, Figure updatedFigure = null)
    {
        string fPos = $"{(char)('a' + prevPos.X)}{1 + prevPos.Y}";
        string nPos = $"{(char)('a' + nextPos.X)}{1 + nextPos.Y}";

        if ((nextPos.MoveT & BoardPos.MoveType.PromotionMove) != 0)
        {
#if DEBUG_
            if (updatedFigure == null)
                throw new ApplicationException("Not able to correctly translate move to UCI, updateFigure not passed!");
#endif

            return fPos + nPos + GetUpdateFigureChar(updatedFigure);
        }
        else return fPos + nPos;
    }
    
    public static char GetUpdateFigureChar(Figure updatedFigure) => updatedFigure switch
    {
        Rook => 'r',
        Knight => 'n',
        Bishop => 'b',
        Queen => 'q',
#if DEBUG_
        _ =>
            throw new ApplicationException("Passed figure is not valid update figure!")
#endif
    };

    public static (BoardPos fPos, BoardPos nPos, char prom) FromUciToInGame(string uciCode)
        // Does not perform any sanity checks, before use make sure that input is correct
        // There is assumption that if no promotion char was on position 4, 'x' is an output of prom field
    {
        BoardPos fPos = new BoardPos(uciCode[0] - 'a', uciCode[1] - '1');
        BoardPos nPos = new BoardPos(uciCode[2] - 'a', uciCode[3] - '1');
        char prom = uciCode.Length == 5 ? uciCode[4] : 'x';

        return (fPos, nPos, prom);
    }

    public static Figure GetUpdateType(char promChar, Figure promPawn, Board bd)
        => promChar switch
        {
            'r' => new Rook(promPawn.Pos.X, promPawn.Pos.Y, promPawn.Color) { Parent = bd },
            'b' => new Bishop(promPawn.Pos.X, promPawn.Pos.Y, promPawn.Color) { Parent = bd },
            'n' => new Knight(promPawn.Pos.X, promPawn.Pos.Y, promPawn.Color) { Parent = bd },
            'q' => new Queen(promPawn.Pos.X, promPawn.Pos.Y, promPawn.Color) { Parent = bd },
#if DEBUG_
            _ => throw new ApplicationException("Passed character is not recognizable as promotion character!!!"),
#endif
        };
    
// ------------------------------
// private methods
// ------------------------------

    private string _processRunningCommands()
        // In actual state translator is able to process  bestmove command and info command only.
        // Outputs best move result if encountered.
    {
        string bestMove = string.Empty;
        
        while (_recordQueue.Count != 0)
        {
            string record;
            lock (_recordQueue) record = _recordQueue.Dequeue();

            int result = record.IndexOf("bestmove", StringComparison.Ordinal);
            if (result != -1)
            {
                if (bestMove != string.Empty)
                    Console.Error.WriteLine("[ WARNING ] Double best move reply received!");

                var cleanedStr = record.Substring(result + 8).Trim();
                int wordEnd = cleanedStr.IndexOfAny("\n ".ToCharArray());

                bestMove = wordEnd == -1 ? cleanedStr : cleanedStr.Substring(0, wordEnd);
            }
            else if (record.IndexOf("info", StringComparison.Ordinal) != -1)
                Console.WriteLine($"Info message from the engine\n:{record}");
            else
                Console.Error.WriteLine($"[ WARNING ] Received invalid UCI command:\n{record}");
        }

        return bestMove;
    }

    private const int ChessEngineBootingMaxTimeMs = 1000;
    private (bool isUciCompatible, string initOutput) _verifyUciCompliance()
        // Functions asks engine whether it is compatible with UCI protocol.
        // Additionally processes all boot process output command.
        // Outputs flag indicating whether it is compatible or not and string containing initial output.
    {
        _chessEngine.StandardInput.WriteLine("uci");
        
        // Amount of time given to engine, after which if it does not identify itself as UCI compatible is terminated.
        Thread.Sleep(ChessEngineBootingMaxTimeMs);
        
        // Processing all commands
        bool uciokResponseEncountered = false;
        StringBuilder builder = new();
        while (_recordQueue.Count != 0)
        {
            string record;
            lock (_recordQueue)
            {
                record = _recordQueue.Dequeue();
            }
            
            builder.Append(record);
            builder.Append('\n');
                
            if (_processBootRecord(record) == EngineCommandType.uciokCommand)
                uciokResponseEncountered = true;
        }
        
        return (uciokResponseEncountered, builder.ToString());
    }

    private EngineCommandType _processBootRecord(string record)
    {
        var words = record.Split(" ",
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < words.Length; ++i)
        {
            switch (words[i])
            {
                case "id":
                    return _processIdCommand(words, i, record);
                case "option":
                    return _processOptionCommand(words, i, record);
                case "uciok":
                    return EngineCommandType.uciokCommand;
            }
        }

        Console.Error.WriteLine($"[ WARNING ] Invalid command received from engine:\n{record}");
        return EngineCommandType.InvalidCommand;
    }

    private EngineCommandType _processIdCommand(string[] words, int startInd, string wholeRecord)
    {
        if (startInd + 1 > words.Length)
            return _announceInvalidCommand(wholeRecord);
        
        switch (words[startInd + 1])
        {
            case "name":
                _engineName = string.Concat(words[(startInd+1)..], " ");
                return EngineCommandType.IdCommand;
            case "author":
                _author = string.Concat(words[(startInd+1)..], " ");
                return EngineCommandType.IdCommand;
            default:
                return _announceInvalidCommand(wholeRecord);
        }
    }
    
    private EngineCommandType _processOptionCommand(string[] words, int startInd, string wholeRecord)
    {
        // TODO: consider implementing this
        
        return _announceInvalidCommand(wholeRecord);
        // string optionName = null;
        // OptionType optType = OptionType.InvalidType;
        //
        // if (words[startInd + 1] != "name")
        //     return _announceInvalidCommand(wholeRecord);
        //
        // StringBuilder builder = new StringBuilder();
        //
        // int i;
        // for (i = startInd + 2; i < words.Length && words[i] != "type"; ++i)
        // {
        //     builder.Append(' ');
        //     builder.Append(words[i]);
        // }
    }

    private EngineCommandType _announceInvalidCommand(string record)
    {
        Console.Error.WriteLine($"[ WARNING ] Encountered invalid UCI command:\n{record}");
        return EngineCommandType.InvalidCommand;
    }

    private void _messageReadingThread()
    {
        while (IsOperational)
        { 
            var record = _chessEngine.StandardOutput.ReadLine();
            lock(_recordQueue) {_recordQueue.Enqueue(record);}
        }
    }
   
// ------------------------------
// public types
// ------------------------------

    private enum EngineCommandType
    {
        InvalidCommand,
        IdCommand,
        readyokCommand,
        bestmoveCommand,
        infoCommand,
        optionCommand,
        uciokCommand
    }

    // private enum OptionType
    // {
    //     InvalidType,
    //     check,
    //     spin,
    //     combo,
    //     button,
    //     str,
    // }
    
// ------------------------------
// public properties
// ------------------------------

    public bool IsOperational => _chessEngine != null && !_chessEngine.HasExited;
    public int MaxSearchTime { get; set; }
    
// ------------------------------
// private fields
// ------------------------------

    // output elements
    private readonly Queue<string> _recordQueue = new();

    // Engines inner parameters
    private string _author;
    private string _engineName;
    // private Dictionary<string, OptionType> _optionsAvailable;

    private double _spentTimeOnSearch;
    private int _searchTries;
    private Thread _messageReceiver;
    private Board _board;
    private Process _chessEngine;
}