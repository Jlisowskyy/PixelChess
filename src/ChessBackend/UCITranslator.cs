#define DEBUG_
using System;
using System.Diagnostics;
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
            Console.WriteLine("[ OK ] Correctly started Chess Engine!");
        }
        catch (Exception exc)
        {
            Console.Error.WriteLine($"[ ERROR ] Not able to startup chess engine! Cause:\n{exc}");
            _chessEngine = null;
        }
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
            
            Console.WriteLine("[ OK ] Correctly closed Chess Engine!");
        }
    }
    
// ------------------------------
// Static methods
// ------------------------------

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

            return fPos + nPos + updatedFigure switch
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
        }
        else return fPos + nPos;
    }

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
// public properties
// ------------------------------

    public bool IsOperational => _chessEngine != null && !_chessEngine.HasExited;
    
// ------------------------------
// private fields
// ------------------------------

    private Board _board;
    private Process _chessEngine;

}