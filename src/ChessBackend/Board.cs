#define DEBUG_

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelChess.Figures;
using static PixelChess.ChessBackend.BoardPos;
using IDrawable = PixelChess.Ui.IDrawable;

namespace PixelChess.ChessBackend;

/*          GENERAL TODOS
 *   - repair drawing offsets XD
 *   - change structure of game elements
 *   - change texture holding
 * 
 *   - add sounds
 *   - update readme and docs
 *
 *   - remove ismoved flag from figure, replace with iswhitekingmoved etc
 * 
 */

public partial class Board : IDrawable
{
// --------------------------------
// type construction / setups
// --------------------------------

    public Board(string fenNotationInput = BasicStartingLayoutFen)
    {
        _startingFiguresLayoutFen = fenNotationInput;
        try
        {
            _startFiguresLayout = FenTranslator.Translate(fenNotationInput);
        }
        catch (ApplicationException exc)
        {
            Console.Error.WriteLine($"Error occured during fen translation: {exc.Message}");
            Console.Error.WriteLine("Loading default layout...");

            _startingFiguresLayoutFen = BasicStartingLayoutFen;
            _startFiguresLayout = FenTranslator.Translate(BasicStartingLayoutFen);
        }
        ResetBoard();
    }

    static Board()
    {
        ComponentsTextures = new Texture2D[Enum.GetNames(typeof(ChessComponents)).Length];
        TileHighlightersTextures = new Texture2D[(int)Enum.GetValues(typeof(TileHighlighters)).Cast<TileHighlighters>().Max() + 1];
        GameEnds = new Texture2D[Enum.GetNames(typeof(EndGameTexts)).Length];
    }

    public void InitializeUiApp(int xOffset, int yOffset)
    {
        _xOffset = xOffset;
        _yOffset = yOffset;
        _xTilesCordOnScreenBeg = XTilesBoardCordBeg + xOffset;
        _yTilesCordOnScreenBeg = YTilesBoardCordBeg + yOffset;
    }

    public void ChangeGameLayout(string fenNotationInput)
    {
        try
        {
            _startFiguresLayout = FenTranslator.Translate(fenNotationInput);
            _startingFiguresLayoutFen = fenNotationInput;
            ResetBoard();
        }
        catch (ApplicationException exc)
        {
            Console.Error.WriteLine($"Error occured during fen translation: {exc.Message}");
            Console.Error.WriteLine("Changing nothing...");
        }
    }

    public void ResetBoard()
    {
        _boardFigures = new Figure[BoardSize, BoardSize];
        _figuresArray = new Figure[_startFiguresLayout.FigArr.Length];
        
        _movesHistory = new LinkedList<HistoricalMove>();
        _movesHistory.AddFirst(new HistoricalMove(-1, -1, new BoardPos(0, 0),
            new Knight(0,0, Figure.ColorT.White), -1)); // Sentinel
        _layoutDict = new Dictionary<string, int>();
        
        _blockedTiles = new[]
        {
            new TileState[BoardSize, BoardSize],
            new TileState[BoardSize, BoardSize]
        };
        
        _colorMetadataMap = new[]{ 
            new ColorMetadata(ChessComponents.BlackRook, ChessComponents.BlackBishop, ChessComponents.BlackQueen, Figure.ColorT.Black),
            new ColorMetadata(ChessComponents.WhiteRook, ChessComponents.WhiteBishop, ChessComponents.WhiteQueen, Figure.ColorT.White)
        };

        _colorMetadataMap[(int)Figure.ColorT.White].AliesRangeOnFigArr[0] = 0;
        _colorMetadataMap[(int)Figure.ColorT.White].AliesRangeOnFigArr[1] = _startFiguresLayout.FirstBlackFig;
        _colorMetadataMap[(int)Figure.ColorT.White].EnemyRangeOnFigArr[0] = _startFiguresLayout.FirstBlackFig;
        _colorMetadataMap[(int)Figure.ColorT.White].EnemyRangeOnFigArr[1] = _figuresArray.Length;

        _colorMetadataMap[(int)Figure.ColorT.Black].AliesRangeOnFigArr[0] = _startFiguresLayout.FirstBlackFig;
        _colorMetadataMap[(int)Figure.ColorT.Black].AliesRangeOnFigArr[1] = _figuresArray.Length;
        _colorMetadataMap[(int)Figure.ColorT.Black].EnemyRangeOnFigArr[0] = 0;
        _colorMetadataMap[(int)Figure.ColorT.Black].EnemyRangeOnFigArr[1] = _startFiguresLayout.FirstBlackFig;
        
        // Needs to be restarted, cuz its only counts in game moves
        _moveCounter = 0;

        // should be restarted before initial check mate tests
        _isGameEnded = false;
        _clearCheck();
        _movingColor = _startFiguresLayout.StartingColor;
        
        try
        {
            _copyAndExtractMetadata();
            _blockAllLinesOnKing(Figure.ColorT.White);
            _blockAllLinesOnKing(Figure.ColorT.Black);
            _blockTiles(_movingColor);
            
            if (IsChecked && IsKingNotAbleToMove)
                _processCheckMate();
            else if (_colorMetadataMap[(int)_movingColor].FiguresCount == 1 
                     && IsKingNotAbleToMove)
                _announcePat();
        }
        catch (ApplicationException exc)
        {
            Console.Error.WriteLine($"Incorrect layout passed: {exc.Message}");
            Console.Error.WriteLine("Loading default layout...");

            _startFiguresLayout = FenTranslator.Translate(BasicStartingLayoutFen);
            _startingFiguresLayoutFen = BasicStartingLayoutFen;
            // Must contain valid layout
            ResetBoard();
        }

        _fullMoves = _startFiguresLayout.FullMoves;
        _halfMoves = _startFiguresLayout.HalfMoves;
        _whiteTime = _startingWhiteTime;
        _blackTime = _startingBlackTime;

        _checkForPositionRepetitions(FenTranslator.GetPosString(this));
    }

    public void ProcTimers(double spentTime)
    {
        if (_moveCounter == 0 || _isGameEnded) return;

        if (_movingColor == Figure.ColorT.White)
        {
            _whiteTime -= spentTime;

            if (_whiteTime < 0)
            {
                _announceWin();
            }
        }
        else
        {
            _blackTime -= spentTime;

            if (_blackTime < 0)
            {
                _announceWin();
            }
        }
        
    }
    
// ------------------------------
// type interaction
// ------------------------------

    public void MakeUciMove(string uciMove)
    {
        var moveData = UciTranslator.FromUciToInGame(uciMove);
        SelectFigure(moveData.fPos);
        var (wasHit, type) = DropFigure(moveData.nPos);
        
        // TODO: there is different semantics of wasHit return flag
#if DEBUG_
        // Board didnt recognized such move, there is bug inside the board or the engine
        if (!wasHit)
        {
            throw new ApplicationException("Encountered invalid move on UCI interface, this should never happen!");
        }
#endif

        if (moveData.prom != 'x' && (type & MoveType.PromotionMove) != 0)
        {
            Figure fig = UciTranslator.GetUpdateType(moveData.prom, _promotionPawn, this);
            Promote(fig);
        }
#if DEBUG_
        else if (moveData.prom != 'x')
        {
            throw new ApplicationException(
                "On the other site of UCI interface promotion was marked, but move made on board wasn't the promoting one!!!");
        }
        else if ((type & MoveType.PromotionMove) != 0)
        {
            throw new ApplicationException("Board detected promotionMove but there was no promotion char on uci move!!");
        }
#endif
    }

    public void UndoMove()
    {
        if (!_isGameEnded) _undoMove();
    }
    public void SetTimers(double whiteTime, double blackTime)
    {
        _startingWhiteTime = _whiteTime = whiteTime;
        _startingBlackTime = _blackTime = blackTime;
    }

    // TODO: consider delegate here when check?
    public void SelectFigure(BoardPos pos)
        // if pos is correct and points to currently moving color's figure selects it as moving figure
    {
        if (!pos.isOnBoard() || _boardFigures[pos.X, pos.Y] == null || _boardFigures[pos.X, pos.Y].Color != _movingColor || _isGameEnded)
        {
            _selectedFigure = null;
            return;
        }
        
        // TODO: add checks in figure?
        
        _selectedFigure = _boardFigures[pos.X, pos.Y];
        _isHold = true;
        _selectedFigure.IsAlive = false;
    }

    public (bool WasHit, MoveType mType) DropFigure(BoardPos pos)
        // method used to 'drop' figure on the board, if there is selected ony
    {
        if (_selectedFigure == null || !pos.isOnBoard() || _isGameEnded) return (false, MoveType.NormalMove);
        
        // flag used to indicate whether figure should be drew or not
        _selectedFigure.IsAlive = true;
        _isHold = false;

        // performs sanity check whether the move was legal
        var moves = _selectedFigure.GetMoves();
        for (int i = 0; i < moves.movesCount; ++i)
            if (pos == moves.moves[i])
                return (true, _processMove(moves.moves[i]));

        return (false, MoveType.NormalMove);
    }
    
    public void Promote(Figure promFig)
        // method used to communicate with PromotionMenu class and BoardClass
        // accepts desired figure to replace promoting pawn and starts processing to next round 
        // from interrupted moment.
    {
#if DEBUG_
        if (_promotionPawn == null)
            throw new ApplicationException("None promoting figure found, but Promote method was called");
#endif
        
        if (promFig == null)
            return;

        _replaceFigure(promFig, _promotionPawn);
        _promotionPawn = null;
        
        // adds promoted pawn to important figures metadata
        _extractDataFromFig(promFig);
        
        // restarts processing to next round
        _processToNextRound();
    }

    private bool IsSelectedFigure() => _selectedFigure != null;
    
// --------------------------------
// Next round/move processing
// --------------------------------

    private MoveType _processMove(BoardPos move)
        // calls special function to handle passed type of move, performs move consequences on board,
        // and finally hands on action to _processToNextRound(), which performs further actions
    {
        Figure killedFig = null;
        
        switch (move.MoveT)
        {
            case MoveType.NormalMove:
                break;
            case MoveType.AttackMove:
                killedFig = _killFigure(move);
                break;
            case MoveType.PromotionMove:
                _promotionPawn = _selectedFigure;
                break;
            case MoveType.PromAndAttack:
                _promotionPawn = _selectedFigure;
                killedFig = _killFigure(move);
                break;
            case MoveType.CastlingMove:
                _castleKing(move);
                break;
            case MoveType.ElPass:
                killedFig = _killFigure(MovesHistory.Last!.Value.MadeMove);
                break;
        }
        
        _movesHistory.AddLast(new HistoricalMove(_selectedFigure.Pos.X, _selectedFigure.Pos.Y, move,
            _selectedFigure, _halfMoves, !_selectedFigure.IsMoved, killedFig));
        _moveFigure(move); 
        
        // If there is promotion incoming we should wait to get information about new figure 
        if ((int)(move.MoveT & MoveType.PromotionMove) == 0)
            _processToNextRound();
        
        return move.MoveT;
    }

    private void _moveFigure(BoardPos move)
        // adds moves to history and applies move consequences to all Board structures
    {
        _boardFigures[_selectedFigure.Pos.X, _selectedFigure.Pos.Y] = null;
        _boardFigures[move.X, move.Y] = _selectedFigure;
        _selectedFigure.Pos = move;

        if (_selectedFigure.Color == Figure.ColorT.Black)
            ++_fullMoves;

        ++_moveCounter;
        
        _selectedFigure.IsMoved = true;
        _selectedFigure = null;
    }

    private Figure _killFigure(BoardPos move)
        // removes figures from figures map and returns killed figure
    {
        Figure retFig = _boardFigures[move.X, move.Y];
        
        _colorMetadataMap[(int)_boardFigures[move.X, move.Y].Color].FiguresCount--;   
        _boardFigures[move.X, move.Y].IsAlive = false;
        _boardFigures[move.X, move.Y] = null;

        return retFig;
    }

    private void _castleKing(BoardPos newKingPos)
        // assumes passed move is correct castling and selected figures is moving king
        // only applies consequences of desired castling 
    {
        (int oldRookX, int newRookX) pos = newKingPos.X switch
        {
            King.LongCastlingX => (MinPos, King.LongCastlingRookX),
            King.ShortCastlingX => (MaxPos, King.ShortCastlingRookX)
        };
        
        _boardFigures[pos.oldRookX, newKingPos.Y].Pos = new(pos.newRookX, newKingPos.Y);
        _boardFigures[pos.newRookX, newKingPos.Y] = _boardFigures[pos.oldRookX, newKingPos.Y];
        _boardFigures[pos.oldRookX, newKingPos.Y] = null;
    }
    private void _processToNextRound()
    {
        // performs cleaning after previous round, like blocking figures, blocking tiles for king
        // and performing checks for game endings
            
        _clearCheck();
        _movingColor = _colorMetadataMap[(int)_movingColor].EnemyColor;
        
        _blockedTiles[(int)_movingColor] = new TileState[BoardSize,BoardSize];
        
        int[] range = _colorMetadataMap[(int)_movingColor].AliesRangeOnFigArr;
        for (int i = range[0]; i < range[1]; ++i)
        {
            _figuresArray[i].IsBlocked = false;
        }
        _blockAllLinesOnKing(_movingColor);
        _blockTiles(_movingColor);
        
        // only moving color can lose a game
        if (IsChecked && _colorMetadataMap[(int)_movingColor].King.GetMoves().movesCount == 0)
            _processCheckMate();
        else
            _processDrawConditions();
    }

    private void _blockTiles(Figure.ColorT col)
        // blocks tiles for king to prevent illegal moves, actually just checks all possibilities
    {
        var range = _colorMetadataMap[(int)col].EnemyRangeOnFigArr;
        for (int i = range[0]; i < range[1]; ++i)
        {
            if (_figuresArray[i].IsAlive == false) continue;
            
            var mv = _figuresArray[i].GetBlockedTiles();
            for (int j = 0; j < mv.tileCount; ++j)
            {
                _blockedTiles[(int)col][mv.blockedTiles[j].X, mv.blockedTiles[j].Y] |= TileState.BlockedTile;
                
                // checks detection from pawns and knights, sliding figures should be detected before on fig blocking phase
                // due to sliding properties
                if (_boardFigures[mv.blockedTiles[j].X, mv.blockedTiles[j].Y] == _colorMetadataMap[(int)col].King)
                {
                    _checkKing(_figuresArray[i]);
                    _blockedTiles[(int)col][_figuresArray[i].Pos.X, _figuresArray[i].Pos.Y] |= TileState.AllowedTile;
                }
            }
        }
    }

    private void _blockFigures(Figure.ColorT col)
        // blocks figures covering king from being killed, accordingly to last made move
    {
        // only used after one move made 
        var lastMove = _movesHistory.Last!.Value;

        if (_boardFigures[lastMove.MadeMove.X, lastMove.MadeMove.Y] == _colorMetadataMap[(int)col].King)
            _blockAllLinesOnKing(col);
        else
        {
            ProcessBlock(lastMove.OldX, lastMove.OldY);
            ProcessBlock(lastMove.MadeMove.X, lastMove.MadeMove.Y);
        }
        
        void ProcessBlock(int x, int y)
        {
            if (y == _colorMetadataMap[(int)col].King.Pos.Y)
            {
                int xDist = _colorMetadataMap[(int)col].King.Pos.X - x;

                if (xDist < 0) // right
                    _blockHorizontalRightLineOnKing(col);
                
                else // left
                    _blockHorizontalLeftLineOnKing(col);
            }
            else if (x == _colorMetadataMap[(int)col].King.Pos.X)
            {
                int yDist = _colorMetadataMap[(int)col].King.Pos.Y - y;

                if (yDist < 0) //up
                    _blockVerticalUpLineOnKing(col);
                else // down
                    _blockVerticalLowLineOnKing(col);
            }
            else
            {
                int xDist = _colorMetadataMap[(int)col].King.Pos.X - x;
                int yDist = _colorMetadataMap[(int)col].King.Pos.Y - y;

                if (Math.Abs(xDist) != Math.Abs(yDist)) return;

                if (xDist < 0)
                {
                    if (yDist < 0) // Ne
                        _blockDiagonal(col, (int)Bishop.Dir.Ne);
                    else // Se
                        _blockDiagonal(col, (int)Bishop.Dir.Se);
                }
                else
                {
                    if (yDist < 0) // Nw
                        _blockDiagonal(col, (int)Bishop.Dir.Nw);
                    else // Sw
                        _blockDiagonal(col, (int)Bishop.Dir.Sw);
                }
            }
        }
    }
    
    private void _replaceFigure(Figure replacementFig, Figure figToReplace)
        // replaces desired figure on board and figuresArray
    {
        for(int i = 0; i < _figuresArray.Length; ++i)
        {
            if (ReferenceEquals(_figuresArray[i], figToReplace))
            {
                _figuresArray[i] = replacementFig;
                _boardFigures[replacementFig.Pos.X, replacementFig.Pos.Y] = replacementFig;
                return;
            }
        }
        
#if DEBUG_
        throw new ApplicationException("Unexpected, figure to change not found");
#endif
    }

// ---------------------------------------
// Game ending conditions processing
// ---------------------------------------

    private void _processCheckMate()
        // checks if there are any legit moves and if not announces win
    {
        var range = _colorMetadataMap[(int)_movingColor].AliesRangeOnFigArr;
        for (int i = range[0]; i < range[1]; ++i)
            if (_figuresArray[i].GetMoves().movesCount != 0) return;
        
        _announceWin();
    }

    private void _processDrawConditions()
        // Processes all necessary calculations to check draw conditions, including moves counting
        // and layout presence in past
    {
        if (_colorMetadataMap[(int)_movingColor].FiguresCount == 1 
            &&_colorMetadataMap[(int)_movingColor].King.GetMoves().movesCount == 0)
            _announcePat();
        
        // sentinel guarded
        var move = _movesHistory.Last!.Value;

        if ((move.MadeMove.MoveT & MoveType.AttackMove) != 0
            || _boardFigures[move.MadeMove.X, move.MadeMove.Y] is Pawn)
            _halfMoves = 0;
        else _halfMoves++;
        
        if (_halfMoves == 50)
            _announceDraw();
        
        if (_checkForPositionRepetitions(FenTranslator.GetPosString(this)))
            _announceDraw();
    }

    private void _announceDraw()
        // turns onn game ended flag and starts up correct animation
    {
        _isGameEnded = true;
        _endGameTextureInd = (int) EndGameTexts.draw;
    }

    private void _announceWin()
        // turns onn game ended flag and starts up correct animation
    {
        _isGameEnded = true;
        _endGameTextureInd = (int) (_movingColor == Figure.ColorT.White ? EndGameTexts.bWin : EndGameTexts.wWin);
    }

    private void _announcePat()
        // turns onn game ended flag and starts up correct animations
    {
        _isGameEnded = true;
        _endGameTextureInd = (int) EndGameTexts.pat;
    }
    
    bool _checkForPositionRepetitions(string pos)
    {
        const int maxRepetitions = 3;
        
        // if (_layoutDict.ContainsKey(pos))
        // {
        //     // TODO: replaced only to test positions
        //     // if ((_layoutDict[pos] += 1) == maxRepetitions) return true;
        //
        //     return false;
        // }
        // else _layoutDict.Add(pos, 1);

        return false;
    }
    
// --------------------------------
// Move consequences removing
// --------------------------------

    private void _undoMove()
    {
        var lastMove = _movesHistory.Last!.Value;
        
        // There is some case guarding sentinel
        if (lastMove.HalfMoves == -1) return;
        _undoPositionChange(lastMove);

        // All of below ones expects to be invoked after changing previously moved figure position
        switch (lastMove.MadeMove.MoveT)
        {
            case MoveType.NormalMove:
                _boardFigures[lastMove.MadeMove.X, lastMove.MadeMove.Y] = null;
                break;
            case MoveType.AttackMove:
                _undoKill(lastMove);
                break;
            case MoveType.PromotionMove:
                _undoPromotion(lastMove);
                break;
            case MoveType.PromAndAttack:
                _undoPromotion(lastMove);
                _undoKill(lastMove);
                break;
            case MoveType.CastlingMove:
                _undoCastling(lastMove);
                break;
            case MoveType.ElPass:
                _undoElPassant(lastMove);
                break;
#if DEBUG_
            default:
                throw new ApplicationException("[DEBUG ERROR]");
#endif
        }

        _halfMoves = lastMove.HalfMoves;
        // Full moves are counted after black moved
        if (lastMove.Fig.Color == Figure.ColorT.Black)
            --_fullMoves;

        _movingColor = lastMove.Fig.Color;
        _movesHistory.RemoveLast();
        
        // Cleaning of blocked tiles and figures
        _clearCheck();
        
        _blockedTiles[(int)_movingColor] = new TileState[BoardSize,BoardSize];
        
        // IMPORTANT NOTE: _blockFigures function is not reversible in current state so only valid solution now is to 
        // just unblock and block them all
        int[] range = _colorMetadataMap[(int)_movingColor].AliesRangeOnFigArr;
        for (int i = range[0]; i < range[1]; ++i)
        {
            _figuresArray[i].IsBlocked = false;
        }
        _blockAllLinesOnKing(_movingColor);
        _blockTiles(_movingColor);
    }
    
    private void _undoPositionChange(HistoricalMove historicalMove)
        // just changes position of passed move figure from new pos to old pos, performs no sanity checks
    {
        historicalMove.Fig.IsMoved = !historicalMove.WasUnmoved;
        _boardFigures[historicalMove.OldX, historicalMove.OldY] = historicalMove.Fig;
        historicalMove.Fig.Pos.X = historicalMove.OldX;
        historicalMove.Fig.Pos.Y = historicalMove.OldY;
    }
    
    private void _undoPromotion(HistoricalMove historicalMove)
        // expects figure to be already moved to old place
    {
        _replaceFigure(historicalMove.Fig, 
            _boardFigures[historicalMove.MadeMove.X, historicalMove.MadeMove.Y]);
        _boardFigures[historicalMove.MadeMove.X, historicalMove.MadeMove.Y] = null;

    }

    private void _undoKill(HistoricalMove historicalMove)
        // assumes killed fig tile is empty just activates killed figure drawing and places it on the map
    {
        historicalMove.KilledFig.IsAlive = true;
        _colorMetadataMap[(int)historicalMove.KilledFig.Color].FiguresCount++;
        _boardFigures[historicalMove.MadeMove.X, historicalMove.MadeMove.Y] = historicalMove.KilledFig;
    }

    private void _undoCastling(HistoricalMove historicalMove)
    {
        _boardFigures[historicalMove.MadeMove.X, historicalMove.MadeMove.Y] = null;
        (int oldRookX, int newRookX) pos = historicalMove.MadeMove.X switch
        {
            King.LongCastlingX => (MinPos, King.LongCastlingRookX),
            King.ShortCastlingX => (MaxPos, King.ShortCastlingRookX)
        };
        
        _boardFigures[pos.newRookX, historicalMove.OldY].Pos = new BoardPos(pos.oldRookX, historicalMove.OldY);
        _boardFigures[pos.oldRookX, historicalMove.OldY] = _boardFigures[pos.newRookX, historicalMove.OldY];
        _boardFigures[pos.newRookX, historicalMove.OldY] = null;
    }

    private void _undoElPassant(HistoricalMove historicalMove)
    {
        _boardFigures[historicalMove.MadeMove.X, historicalMove.MadeMove.Y] = null;
        _boardFigures[historicalMove.MadeMove.X, historicalMove.OldY] = historicalMove.KilledFig;
        _colorMetadataMap[(int)historicalMove.KilledFig.Color].FiguresCount++;
        historicalMove.KilledFig.IsAlive = true;
    }
    
// ---------------------------------
// King/Tiles blocking methods
// ---------------------------------

    private void _blockAllLinesOnKing(Figure.ColorT col)
        // used to block figures covering col king from being killed on all lines (diagonal and simple ones)
    {
        for (int i = _colorMetadataMap[(int)col].AliesRangeOnFigArr[0];
             i < _colorMetadataMap[(int)col].AliesRangeOnFigArr[1];
             ++i) 
            _figuresArray[i].IsBlocked = false;
        
        _blockHorizontalLeftLineOnKing(col);
        _blockHorizontalRightLineOnKing(col);
        _blockVerticalLowLineOnKing(col);
        _blockVerticalUpLineOnKing(col);
        
        for(int i = 0; i < 4; ++i)
            _blockDiagonal(col, i);
    }
    
    private void _blockStraightLine<TMoveConds>(Figure.ColorT col)
        where TMoveConds : Rook.IStraightMove
    {
        Figure kingToCheck = _colorMetadataMap[(int)col].King;

        int foundFigCord = -1;
        int init = TMoveConds.InitIter(kingToCheck) + TMoveConds.Move;
        
        for (int iter = init; TMoveConds.RangeCheck(iter); iter+=TMoveConds.Move)
        {
            (int x, int y) = TMoveConds.GetPos(kingToCheck, iter);
            
            if (!_isEmpty(x, y))
                // figure detected
            {
                if (foundFigCord == -1)
                    // first figure on the line
                {
                    if (_boardFigures[x, y].TextureIndex == _colorMetadataMap[(int)col].EnemyRook ||
                        _boardFigures[x, y].TextureIndex == _colorMetadataMap[(int)col].EnemyQueen)
                        // if first figure is attacking king process allowed tiles and return
                    {
                        _checkKing(_boardFigures[x, y]);
                        int lastAllowedTilesCount = int.Abs(TMoveConds.InitIter(kingToCheck) - iter);
                        
                        for (int i = 0; i < lastAllowedTilesCount; ++i)
                        {
                            (int nx, int ny) = TMoveConds.GetPos(kingToCheck, iter - i*TMoveConds.Move);
                            _blockedTiles[(int)col][nx, ny] |= TileState.AllowedTile;
                        }

                        // blocking tile behind the king to prevent illegal king backup move
                        var (tempX, tempY) = TMoveConds.GetPos(kingToCheck,
                            TMoveConds.InitIter(kingToCheck) - TMoveConds.Move);
                        if (isOnBoard(tempX, tempY))
                        {
                            _blockedTiles[(int)col][tempX, tempY] |= TileState.BlockedTile;
                        }
                        
                        // return;
                    }
                    
                    foundFigCord = iter;
                }
                else
                // second figure detected 
                {
                    (int nx, int ny) = TMoveConds.GetPos(kingToCheck, foundFigCord);
                    
                    if (_boardFigures[x, y].Color != col)
                        // second fig is enemy
                    {
                        if (_boardFigures[nx, ny].Color == col)
                            // second fig is enemy and first is ally
                        {
                            if (_boardFigures[x, y].TextureIndex == _colorMetadataMap[(int)col].EnemyRook ||
                                _boardFigures[x, y].TextureIndex == _colorMetadataMap[(int)col].EnemyQueen)
                                // first fig (ally) blocked by rook or queen
                                _boardFigures[nx, ny].IsBlocked = true;
                            else
                                // enemy figure is not attacking ally free to go
                                _boardFigures[nx, ny].IsBlocked = false;
                        }
                    }
                    else
                        // second fig is ally
                    {
                        // first fig cannot attack king so second is unblocked
                        _boardFigures[x, y].IsBlocked = false;

                        // if we are sure first one is ally, he is free to go
                        if (_boardFigures[nx, ny].Color == col)
                            _boardFigures[nx, ny].IsBlocked = false;
                    }
                    
                    return;
                }
            }
        }
    }

    private void _blockHorizontalLeftLineOnKing(Figure.ColorT col)
        => _blockStraightLine<Rook.HorDecrease>(col);

    private void _blockHorizontalRightLineOnKing(Figure.ColorT col)
        => _blockStraightLine<Rook.HorIncrease>(col);

    private void _blockVerticalLowLineOnKing(Figure.ColorT col)
        => _blockStraightLine<Rook.VerDecrease>(col);

    private void _blockVerticalUpLineOnKing(Figure.ColorT col)
        => _blockStraightLine<Rook.VerIncrease>(col);

    private void _blockDiagonal(Figure.ColorT col, int dir)
        // used to block figures covering col king from being killed on diagonals
    {
        Figure kingToCheck = _colorMetadataMap[(int)col].King;
        
        int nx = kingToCheck.Pos.X;
        int ny = kingToCheck.Pos.Y;
        int mx = -1;
        int my = 0;

        for (int j = 0; j < Bishop.MoveLimMap[kingToCheck.Pos.X, kingToCheck.Pos.Y][dir]; ++j)
        {
            nx += Bishop.XMoves[dir];
            ny += Bishop.YMoves[dir];
            
            if (!_isEmpty(nx, ny))
                // figure detected
            {
                if (mx == -1)
                    // first figure on the line
                {
                    if (_boardFigures[nx, ny].TextureIndex == _colorMetadataMap[(int)col].EnemyBishop ||
                        _boardFigures[nx, ny].TextureIndex == _colorMetadataMap[(int)col].EnemyQueen)
                        // if first figure is attacking king process allowed tiles and return
                    {
                        _checkKing(_boardFigures[nx, ny]);
                        int lastAllowedTilesCount = Math.Abs(kingToCheck.Pos.Y - ny) - 1;

                        nx = kingToCheck.Pos.X + Bishop.XMoves[dir];
                        ny = kingToCheck.Pos.Y + Bishop.YMoves[dir];
                    
                        // marking fields that moves directing to this tiles are actually legal
                        for (int i = 0; i < lastAllowedTilesCount; ++i)
                        {
                            _blockedTiles[(int)col][nx, ny] |= TileState.AllowedTile;
                            nx += Bishop.XMoves[dir];
                            ny += Bishop.YMoves[dir];
                        }
                        
                        // blocking king move on same line but in opposite direction
                        
                        int tempX = kingToCheck.Pos.X - Bishop.XMoves[dir];
                        int tempY = kingToCheck.Pos.Y - Bishop.YMoves[dir];
                        if (isOnBoard(tempX, tempY))
                        {
                            _blockedTiles[(int)col][tempX, tempY] |= TileState.BlockedTile;
                        }
                    }

                    mx = nx;
                    my = ny;
                }
                else
                    // second figure detected 
                {
                    if (_boardFigures[nx, ny].Color != col)
                        // second fig is enemy
                    {
                        if (_boardFigures[mx, my].Color == col)
                            // second fig is enemy and first is ally
                        {
                            if (_boardFigures[nx, ny].TextureIndex == _colorMetadataMap[(int)col].EnemyBishop ||
                                _boardFigures[nx, ny].TextureIndex == _colorMetadataMap[(int)col].EnemyQueen)
                                // first fig (ally) blocked by rook or queen
                                _boardFigures[mx, my].IsBlocked = true;
                            else
                                // enemy figure is not attacking ally free to go
                                _boardFigures[mx, my].IsBlocked = false;
                        }
                    }
                    else
                        // second fig is ally
                    {
                        // first fig cannot attack king so second is unblocked
                        _boardFigures[nx, ny].IsBlocked = false;

                        // if we are sure first one is ally, he is free to go
                        if (_boardFigures[mx, my].Color == col)
                            _boardFigures[mx, my].IsBlocked = false;
                    }
                    
                    return;
                }
            }
        }
    }
    
// ------------------------------
// Metadata extraction
// ------------------------------

    private void _copyAndExtractMetadata()
        // performs deep copy of saved starting layout to in-game figures array
        // used mainly when initialising game or restarting to start position
        // also analyzes the layout and extract metadata about sliding figures and kings
    {
        for(int i = 0; i < _startFiguresLayout.FigArr.Length; ++i)
        {
            _figuresArray[i] = _startFiguresLayout.FigArr[i].Clone();
            Figure fig = _figuresArray[i];
            
            _boardFigures[fig.Pos.X, fig.Pos.Y] = fig;
            fig.Parent = this;

            _extractDataFromFig(fig);
        }

        if (_colorMetadataMap[(int)Figure.ColorT.White].King == null || _colorMetadataMap[(int)Figure.ColorT.Black].King == null)
            throw new ApplicationException("Starting layout has to contain white and black king!");

        if (_figuresArray.Length < 3)
            throw new ApplicationException("Not playable layout - expects to contain more figures than 2 kings");
        
        for (int i = 0; i < 2; ++i)
            _colorMetadataMap[i].FiguresCount = _colorMetadataMap[i].AliesRangeOnFigArr[1] - _colorMetadataMap[i].AliesRangeOnFigArr[0];
    }
    
    // TODO: is not used currently, should gather information from layout to boost up performance, but not used
    // TODO: correct behaviour when figure is killed etc is not implemented so it should not be used
    private void _extractDataFromFig(Figure fig)
        // used to extract some data when loading layout from starting array or fen input
    {
        switch (fig.TextureIndex)
        {
            case ChessComponents.WhiteBishop:
                CheckBishopTiles();
                break;
            case ChessComponents.WhiteRook:
                _colorMetadataMap[(int)Figure.ColorT.White].Rooks.AddLast(fig);
                break;
            case ChessComponents.WhiteQueen:
                _colorMetadataMap[(int)Figure.ColorT.White].Queens.AddLast(fig);
                break;
            case ChessComponents.WhiteKing:
                if (_colorMetadataMap[(int)Figure.ColorT.White].King == null)
                    _colorMetadataMap[(int)Figure.ColorT.White].King = fig;
                else
                    throw new ApplicationException("Only one white king is expected!");
                break;
            case ChessComponents.BlackBishop:
                CheckBishopTiles();
                break;
            case ChessComponents.BlackRook:
                _colorMetadataMap[(int)Figure.ColorT.Black].Rooks.AddLast(fig);
                break;
            case ChessComponents.BlackQueen:
                _colorMetadataMap[(int)Figure.ColorT.Black].Queens.AddLast(fig);
                break;
            case ChessComponents.BlackKing:
                if (_colorMetadataMap[(int)Figure.ColorT.Black].King == null)
                    _colorMetadataMap[(int)Figure.ColorT.Black].King = fig;
                else
                    throw new ApplicationException("Only one black king is expected!");
                break;
        }

        void CheckBishopTiles()
            // determines whether bishops walks on white or black tiles
        {
            if (Math.Abs(fig.Pos.X - fig.Pos.Y) % 2 == 0)
                _colorMetadataMap[(int)fig.Color].BlackTilesBishops.AddLast(fig);
            else
                _colorMetadataMap[(int)fig.Color].WhiteTilesBishops.AddLast(fig);
        }
    }
    
    private bool _isEmpty(int x, int y) => _boardFigures[x, y] == null;

    private void _checkKing(Figure fig)
    {
        if (!IsChecked || !ReferenceEquals(fig, _kingAttackingFigure))
        {
            _kingAttackingFigure = fig;
            ++_kingAttackingFigureCount;
        }
    }

    private void _clearCheck()
    {
        _kingAttackingFigure = null;
        _kingAttackingFigureCount = 0;
    }

    private bool IsKingNotAbleToMove => _colorMetadataMap[(int)_movingColor].King.GetMoves().movesCount == 0;
    
// -----------------------------------
// debugging and testing methods
// -----------------------------------

    public bool PerformShallowTest(int depth)
    {
        MoveGenerationTester test = new MoveGenerationTester(this);
        var (_, result) = test.PerformShallowMoveGenerationTest(depth);
        return result;
    }

    public void PerformDeepTest(int depth, int maxPaths = int.MaxValue)
    {
        MoveGenerationTester test = new MoveGenerationTester(this);
        test.PerformDeepMoveGenerationTest(depth, maxPaths);
    }
    
// ------------------------------
// public types
// ------------------------------

    // used mainly when drawing figures and board, but sometimes used to determine figure type
    public enum ChessComponents
    {
        Board,
        WhitePawn,
        WhiteKnight,
        WhiteBishop,
        WhiteRook,
        WhiteQueen,
        WhiteKing,
        BlackPawn,
        BlackKnight,
        BlackBishop,
        BlackRook,
        BlackQueen,
        BlackKing,
    }

    // used to represent texture index of corresponding tile highlighter,
    // TODO: DONT UNDERSTAND THIS YET XDDD ATTACK | PROM ???
    public enum TileHighlighters
    {
        MoveTile = 0,
        BasicAttackTile = 1,
        PromotionTile = 2,
        CastlingMove = 4,
        ElPass = 8,
        SelectedTile = 9,
        KingAttackTile = 10,
    }

    public enum EndGameTexts
    {
        wWin,
        bWin,
        draw,
        pat,
    }

    // used to explicitly hold saved layouts
    public struct Layout
    {
        public int FirstBlackFig;
        public Figure.ColorT StartingColor;
        public Figure[] FigArr;
        public BoardPos ElPassantPos; // TODO: DO SOMETHING WITH IT: Translates but do not apply
        public int HalfMoves;
        public int FullMoves;
    }
    
    // used in moves filtering-maps where
    // - UnblockedTiles - means that move TO and FROM that field is possible, and king is allowed to move here
    // - BlockedTile - means that king is NOT allowed to move here
    // - AllowedTile - means if there is a check it is allowed move for other figures than king to cover him
    [Flags]
    public enum TileState
    {
        UnblockedTile = 0,
        BlockedTile = 1,
        AllowedTile = 2,
    }
    
    // metadata class used to hold information about specific figures of single color
    public struct ColorMetadata
    {
        public ColorMetadata(ChessComponents enRook, ChessComponents enBishop, ChessComponents enQueen, Figure.ColorT enCol)
        {
            EnemyBishop = enBishop;
            EnemyQueen = enQueen;
            EnemyRook = enRook;
            EnemyColor = enCol;
        }
        
        public Figure King = null;
        public readonly LinkedList<Figure> BlackTilesBishops = new LinkedList<Figure>();
        public readonly LinkedList<Figure> WhiteTilesBishops = new LinkedList<Figure>();
        public readonly LinkedList<Figure> Rooks = new LinkedList<Figure>();
        public readonly LinkedList<Figure> Queens = new LinkedList<Figure>();

        public readonly ChessComponents EnemyRook;
        public readonly ChessComponents EnemyBishop;
        public readonly ChessComponents EnemyQueen;

        public readonly Figure.ColorT EnemyColor;

        public readonly int[] EnemyRangeOnFigArr = new int[2];
        public readonly int[] AliesRangeOnFigArr = new int[2];

        public int FiguresCount = 0;
    }
    
// ------------------------------
// public properties
// ------------------------------
    public Figure PromotionPawn => _promotionPawn;
    // used to copy pawn parameter to extern promotion class
    public LinkedList<HistoricalMove> MovesHistory => _movesHistory;
    // contains all made moves, used in some function, which needs historical data e.g. el passant moves generators
    // TODO: in future to save games in some database
    public Figure[,] BoardFigures => _boardFigures;
    // used mostly in figure's GetMoves method
    public TileState[][,] BlockedTiles => _blockedTiles;
    // used during moves generation to filter illegal moves
    public bool IsHold => _isHold;

    public Figure.ColorT MovingColor => _movingColor;

    public bool IsChecked => _kingAttackingFigure != null;
    public int KingAttackingFiguresCount => _kingAttackingFigureCount;
    public double WhiteTime => _whiteTime;
    public double BlackTime => _blackTime;
    public ColorMetadata[] ColorMetadataMap => _colorMetadataMap;

    public int HalfMoves => _halfMoves;
    public int FullMoves => _fullMoves;
    
    public const int XTilesBeg = 51;
    public const int YTilesBeg = 0;
    public const int XTilesBoardCordBeg = 51;
    public const int YTilesBoardCordBeg = 477;
    public const int Width = 600;
    public const int Height = 600;
    public const int FigureHeight = 68;
    public const int FigureWidth = 68;
    public const int BoardSize = 8;
    
// ------------------------------
// private variables
// ------------------------------

    private Figure _selectedFigure;
    // figures currently chosen to move by player
    
    private Figure _promotionPawn;
    // figures expected to be promoted by player
    
    private Figure _kingAttackingFigure;
    // figure currently attacking king, used also as check detecting flag

    private int _kingAttackingFigureCount;
    
    private ColorMetadata[] _colorMetadataMap;
    // metadata per figures color about players figures deck
    
    private Figure[,] _boardFigures;
    // actual figures on board used to map positions to figures
    
    private Layout _startFiguresLayout;
    // storing starting position used to restore start layout 
    private string _startingFiguresLayoutFen;

    private Figure[] _figuresArray;
    // used to look up throughout all figures for e.g. to process all enemy moves
    
    private Figure.ColorT _movingColor;
    // actually moving color

    private LinkedList<HistoricalMove> _movesHistory;

    private TileState[][,] _blockedTiles;
    // filtering maps used to block moves
    
    private bool _isHold;
    
    // Game ending variables
    private bool _isGameEnded;
    private int _endGameTextureInd;
    
    // Dictionary holding position present in the past to detect position repetitions
    private Dictionary<string, int> _layoutDict;
    
    // Board positioning
    private int _yTilesCordOnScreenBeg = YTilesBoardCordBeg;
    private int _xTilesCordOnScreenBeg = XTilesBoardCordBeg;
    private int _xOffset;
    private int _yOffset;
    
    private const int MouseCentX = - FigureWidth / 2;
    private const int MouseCentY = - FigureHeight / 2;

    // Using un restoring of beginning times of players
    private double _startingWhiteTime = BasicWhiteTime;
    private double _startingBlackTime = BasicBlackTIme;
    
    // actual players time
    private double _whiteTime;
    private double _blackTime;

    // in game made moves
    private int _moveCounter;
    // full moves defined by rules
    private int _fullMoves;
    // half moves to apply 50 moves rule
    private int _halfMoves;
    
// ------------------------------
// static fields
// ------------------------------

    public static readonly Texture2D[] ComponentsTextures; 
    public static readonly Texture2D[] TileHighlightersTextures;
    public static readonly Texture2D[] GameEnds;

    public const double Second = 1000;
    public const double Minute = 60 * Second;
    public const double BasicWhiteTime = 10 * Minute;
    public const double BasicBlackTIme = 10 * Minute;

    public const string BasicStartingLayoutFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
}