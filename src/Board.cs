#define DEBUG_

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PongGame.Figures;
using static PongGame.BoardPos;

namespace PongGame;

/*          GENERAL TODOS
 *   - repair drawing offsets XD
 *   - change structure of game elements
 *   - add interactive menus
 *   - change texture holding
 * 
 *   - make tests for moves
 *   - implement check mate
 *   - add sounds
 *   - open source models
 *   - update readme and docs
 *
 *   - blocking tiles function should process all possible moves
 *   - checking errors
 * 
 */

// TODO: 

public class Board
{
// --------------------------------
// type construction / setups
// --------------------------------
    public Board(Layout layout)
        // expects layout array to be grouped to colors groups fitted together, where firs group is white,
        // there also should be exactly one king per color and more than one figure different than king
    {
        _startFiguresLayout = layout;
        ResetBoard();
    }

    public Board(string fenNotationInput)
    {
        try
        {
            _startFiguresLayout = FenTranslator.Translate(fenNotationInput);
        }
        catch (ApplicationException exc)
        {
            Console.Error.WriteLine($"Error occured during fen translation: {exc.Message}");
            Console.Error.WriteLine("Loading default layout...");

            _startFiguresLayout = BasicBeginningLayout;
        }
        ResetBoard();
    }

    static Board()
    {
        ComponentsTextures = new Texture2D[Enum.GetNames(typeof(ChessComponents)).Length];
        TileHighlightersTextures = new Texture2D[(int)Enum.GetValues(typeof(TileHighlighters)).Cast<TileHighlighters>().Max() + 1];
        GameEnds = new Texture2D[Enum.GetNames(typeof(EndGameTexts)).Length];
    }

    public void InitializeUiApp(int xOffset, int yOffset, SpriteBatch batch)
    {
        _xOffset = xOffset;
        _yOffset = yOffset;
        _xTilesCordOnScreenBeg = XTilesBoardCordBeg + xOffset;
        _yTilesCordOnScreenBeg = YTilesBoardCordBeg + yOffset;
        _spriteBatch = batch;
    }

    public void ChangeGameLayout(Layout layout)
    {
        _startFiguresLayout = layout;
        ResetBoard();
    }

    public void ChangeGameLayout(string fenNotationInput)
    {
        _startFiguresLayout = FenTranslator.Translate(fenNotationInput);
        ResetBoard();
    }

    public void ResetBoard()
    {
        _boardFigures = new Figure[BoardSize, BoardSize];
        _figuresArray = new Figure[_startFiguresLayout.FigArr.Length];
        
        _movesHistory = new LinkedList<Move>();
        _movesHistory.AddFirst(new Move(0, 0, new BoardPos(0, 0), null)); // Sentinel
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
        
        // Needs to be reseted, cuz its only counts in game moves
        _moveCounter = 0;

        // should be reseted before initial check mate tests
        _isGameEnded = false;
        _movingColor = _startFiguresLayout.StartingColor;
        
        try
        {
            _copyAndExtractMetadata();
            _blockAllLinesOnKing(Figure.ColorT.White);
            _blockAllLinesOnKing(Figure.ColorT.Black);
            _blockTilesInit(_movingColor);
            
            // TODO: test for not moving color check????
            
            if (_kingAttackingFigure != null && _colorMetadataMap[(int)_movingColor].King.GetMoves().movesCount == 0)
                _processCheckMate(_movingColor);
            else if (_colorMetadataMap[(int)_movingColor].FiguresCount == 1 
                     &&_colorMetadataMap[(int)_movingColor].King.GetMoves().movesCount == 0)
                _announcePat();
        }
        catch (ApplicationException exc)
        {
            Console.Error.WriteLine($"Incorrect layout passed: {exc.Message}");
            Console.Error.WriteLine("Loading default layout...");

            _startFiguresLayout = BasicBeginningLayout;
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
            
            // TODO:
            if (_whiteTime < 0){}
        }
        else
        {
            _blackTime -= spentTime;
            
            // TODO:
            if (_blackTime < 0){}
        }
        
    }
    
// ------------------------------
// type interaction
// ------------------------------

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
        // accepts desired figure to replace promoting pawn
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
    }

    private bool IsSelectedFigure() => _selectedFigure != null;

    private Vector2 CenterFigurePosOnMouse(int x, int y) => new(x + MouseCentX, y + MouseCentY);

    private Vector2 Translate(BoardPos pos)
        => new Vector2(_xTilesCordOnScreenBeg + pos.X * FigureWidth, _yTilesCordOnScreenBeg - pos.Y * FigureHeight);

    public BoardPos Translate(int x, int y)
        => new BoardPos((x - _xTilesCordOnScreenBeg) / FigureWidth, (_yTilesCordOnScreenBeg + 68 - y) / FigureHeight);
    

// ------------------------------
// drawing method
// ------------------------------
    public void Draw()
    {
        _drawBoard();
        _drawHighlightedTiles();
        _drawStaticFigures();
        _drawHoveringFigure();
        _drawEndGameSign();
    }
    
    private void _drawBoard() => _spriteBatch.Draw(ComponentsTextures[(int)ChessComponents.Board], new Vector2(_xOffset, _yOffset), Color.White);

    private void _drawHighlightedTiles()
        // draws applies highlighting layers on the board to inform player about allowed move and ongoing actions
    {
        if (IsSelectedFigure())
        {
            var moves = _selectedFigure.GetMoves();
            
            _spriteBatch.Draw(TileHighlightersTextures[(int)TileHighlighters.SelectedTile], Translate(_selectedFigure.Pos), Color.White);
            
            for (int i = 0; i < moves.movesCount; ++i)
                _spriteBatch.Draw(TileHighlightersTextures[(int)moves.moves[i].MoveT], Translate(moves.moves[i]), Color.White);
        }

        if (_kingAttackingFigure != null)
            _spriteBatch.Draw(TileHighlightersTextures[(int)TileHighlighters.KingAttackTile], Translate(_colorMetadataMap[(int)_movingColor].King.Pos), Color.White);
    }

    private void _drawStaticFigures()
        // draws not moving figures on the board
    {
        foreach (var actFig in _figuresArray)
        {
            if (actFig.IsAlive)
                _spriteBatch.Draw(ComponentsTextures[(int)actFig.TextureIndex], Translate(actFig.Pos), Color.White);
        }
    }

    private void _drawHoveringFigure()
        // draws figure on cursor, when figure is hold
    {
        if (_isHold)
        {
            var mState = Mouse.GetState();
                
            _spriteBatch.Draw(ComponentsTextures[(int)_selectedFigure.TextureIndex],
                CenterFigurePosOnMouse(mState.X, mState.Y), Color.White);
        }
    }

    private void _drawEndGameSign()
    {
        if (!_isGameEnded) return;
        int horOffset = (Width - GameEnds[_endGameTextureInd].Width) / 2;
        int verOffset = (Height - GameEnds[_endGameTextureInd].Height) / 2;

        
        _spriteBatch.Draw(GameEnds[_endGameTextureInd], new Vector2(_xOffset + horOffset, _yOffset + verOffset), Color.White);
    }
    
// ------------------------------
// Private methods zone
// ------------------------------

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
        
        // TODO: when promotion was done _selectedFigure is not valid then
        _movesHistory.AddLast(new Move(_selectedFigure.Pos.X, _selectedFigure.Pos.Y, move, _selectedFigure, _selectedFigure.IsMoved, killedFig));
        _moveFigure(move); 
        _processToNextRound();
        return move.MoveT;
    }

    private void _replaceFigure(Figure replacementFig, Figure figToReplace)
    {
        // replaces desired figure also on the figures Array
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

    private void _undoPositionChange(Move move)
        // just changes position of passed move figure from new pos to old pos, performs no sanity checks
    {
        move.Fig.IsMoved = move.WasUnmoved;
        _boardFigures[move.MadeMove.X, move.MadeMove.Y] = null;
        _boardFigures[move.OldX, move.OldY] = move.Fig;
        move.Fig.Pos.X = move.OldX;
        move.Fig.Pos.Y = move.OldY;
    }
    
    private void _undoPromotion(Move move)
        // expects figure to be already move to old place
    {
        Figure oldPawn = new Pawn(move.OldX, move.OldY, _boardFigures[move.OldX, move.OldY].Color) { Parent = this };
        _replaceFigure(oldPawn, _boardFigures[move.OldX, move.OldY]);
    }

    private void _undoKill(Move move)
        // assumes killed fig tile is empty just activates killed figure drawing and places it on the map
    {
        move.KilledFig.IsAlive = true;
        _boardFigures[move.MadeMove.X, move.MadeMove.Y] = move.KilledFig;
    }

    private void _undoCastling(Move move)
    {
        (int oldRookX, int newRookX) pos = move.OldX switch
        {
            King.LongCastlingX => (MinPos, King.LongCastlingRookX),
            King.ShortCastlingX => (MaxPos, King.ShortCastlingRookX)
        };
        
        _boardFigures[pos.newRookX, move.OldY].Pos = new BoardPos(pos.oldRookX, move.OldY);
        _boardFigures[pos.oldRookX, move.OldY] = _boardFigures[pos.newRookX, move.OldY];
        _boardFigures[pos.newRookX, move.OldY] = null;
    }

    private void _undoElPassant(Move move)
    {
        _boardFigures[move.MadeMove.X, move.OldY] = move.KilledFig;
        move.KilledFig.IsAlive = true;
    }

    private void _undoMove()
    {
        var lastMove = _movesHistory.Last!.Value;
        if (lastMove.Fig == null) return;
        _undoPositionChange(lastMove);
        
        switch (lastMove.MadeMove.MoveT)
        {
            case MoveType.NormalMove:
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
        
        _movesHistory.RemoveLast();
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
    private void _processToNextRound()
    {
        // performs cleaning after previous round, like blocking figures, blocking tiles for king
        // and performing checks for game endings
        
        // TODO: temporary all blocking strategy
        
        // if (_lastAllowedTilesCount != 0){
        //     for (int i = 0; i < _lastAllowedTilesCount; ++i)
        //         _blockedTiles[(int)_movingColor][_lastAllowedTilesArr[i].X, _lastAllowedTilesArr[i].Y] ^=
        //             TileState.AllowedTile;
        //
        //     _lastAllowedTilesCount = 0;
        // }
            
        _kingAttackingFigure = null;
        _movingColor = _movingColor == Figure.ColorT.White ? Figure.ColorT.Black : Figure.ColorT.White;
        
        _blockedTiles[0] = new TileState[BoardSize,BoardSize];
        _blockedTiles[1] = new TileState[BoardSize,BoardSize];

        _blockFigures(_movingColor);
        _blockFigures(_colorMetadataMap[(int)_movingColor].EnemyColor);
        _blockTiles(_movingColor);
        
        // only moving color can lose a game
        if (_kingAttackingFigure != null && _colorMetadataMap[(int)_movingColor].King.GetMoves().movesCount == 0)
            _processCheckMate(_movingColor);
        else
            _processDrawConditions();
    }

    private void _blockTiles(Figure.ColorT col)
        // blocks tiles for king to prevent illegal moves, actually just checks all possibilities
    {
        for (int i = _colorMetadataMap[(int)col].EnemyRangeOnFigArr[0]; i < _colorMetadataMap[(int)col].EnemyRangeOnFigArr[1]; ++i)
        {
            var mv = _figuresArray[i].GetMoves();
    
            for (int j = 0; j < mv.movesCount; ++j)
            {
                _blockedTiles[(int)col][mv.moves[j].X, mv.moves[j].Y] |= TileState.BlockedTile;
                
                // checks detection from pawns and knights, sliding figures should be detected before on fig blocking phase
                // due to sliding properties
                if ((mv.moves[j].MoveT & MoveType.AttackMove) != 0 && _boardFigures[mv.moves[j].X, mv.moves[j].Y] == _colorMetadataMap[(int)col].King)
                    _kingAttackingFigure = _figuresArray[i];
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
    
    private void _blockTilesInit(Figure.ColorT col)
        // blocks tiles for king and also checks whether there is check or not, if kings has no moves ends the game
    {
        for (int i = _colorMetadataMap[(int)col].EnemyRangeOnFigArr[0]; i < _colorMetadataMap[(int)col].EnemyRangeOnFigArr[1]; ++i)
        {
            var moves = _figuresArray[i].GetMoves();
    
            for (int j = 0; j < moves.movesCount; ++j)
            {
                _blockedTiles[(int)col][moves.moves[j].X, moves.moves[j].Y] |= TileState.BlockedTile;
                if (_boardFigures[moves.moves[j].X, moves.moves[j].Y] == null)continue;
                
                var type = _boardFigures[moves.moves[j].X, moves.moves[j].Y].TextureIndex;
                // checks detection from pawns and knights, sliding figures should be detected before blocking phase
                if (type == _colorMetadataMap[(int)col].King.TextureIndex &&
                    type != _colorMetadataMap[(int)col].EnemyQueen && 
                    type != _colorMetadataMap[(int)col].EnemyRook &&
                    type != _colorMetadataMap[(int)col].EnemyBishop)
                {
                    if (_kingAttackingFigure != null)
                        throw new ApplicationException("Double check detected - invalid layout");
                    
                    _kingAttackingFigure = _figuresArray[i];
                }
            }
        }
    }

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

    // TODO: ALL MOTHERFUCKERS BELOW SHOULD BE TEMPLATED AF
    
    private void _blockHorizontalLeftLineOnKing(Figure.ColorT col)
        // used to block figures covering col king from being killed on horizontal left line
    {
        Figure kingToCheck = _colorMetadataMap[(int)col].King;

        int mx = -1;
        for (int x = kingToCheck.Pos.X - 1; x >= MinPos; --x)
        {
            if (!_isEmpty(x, kingToCheck.Pos.Y))
                // figure detected
            {
                if (mx == -1)
                    // first figure on the line
                {
                    if (_boardFigures[x, kingToCheck.Pos.Y].TextureIndex == _colorMetadataMap[(int)col].EnemyRook ||
                        _boardFigures[x, kingToCheck.Pos.Y].TextureIndex == _colorMetadataMap[(int)col].EnemyQueen)
                        // if first figure is attacking king process allowed tiles and return
                    {
                        if (_kingAttackingFigure != null)
                            continue;
                            // throw new ApplicationException("Double check detected - invalid move");
                        
                        _kingAttackingFigure = _boardFigures[x, kingToCheck.Pos.Y];
                        _lastAllowedTilesCount = kingToCheck.Pos.X - x - 1;
                        _lastAllowedTilesArr = new BoardPos[_lastAllowedTilesCount];

                        for (int i = 0; i < _lastAllowedTilesCount; ++i)
                        {
                            _lastAllowedTilesArr[i].Y = kingToCheck.Pos.Y;
                            _lastAllowedTilesArr[i].X = x + 1 + i;
                            _blockedTiles[(int)col][x + 1 + i, kingToCheck.Pos.Y] |= TileState.AllowedTile;
                        }
                        
                        return;
                    }
                    
                    mx = x;
                }
                else
                // second figure detected 
                {
                    if (_boardFigures[x, kingToCheck.Pos.Y].Color != col)
                        // second fig is enemy
                    {
                        if (_boardFigures[mx, kingToCheck.Pos.Y].Color == col)
                            // second fig is enemy and first is ally
                        {
                            if (_boardFigures[x, kingToCheck.Pos.Y].TextureIndex == _colorMetadataMap[(int)col].EnemyRook ||
                                _boardFigures[x, kingToCheck.Pos.Y].TextureIndex == _colorMetadataMap[(int)col].EnemyQueen)
                                // first fig (ally) blocked by rook or queen
                                _boardFigures[mx, kingToCheck.Pos.Y].IsBlocked = true;
                            else
                                // enemy figure is not attacking ally free to go
                                _boardFigures[mx, kingToCheck.Pos.Y].IsBlocked = false;
                        }
                    }
                    else
                        // second fig is ally
                    {
                        // first fig cannot attack king so second is unblocked
                        _boardFigures[x, kingToCheck.Pos.Y].IsBlocked = false;

                        // if we are sure first one is ally, he is free to go
                        if (_boardFigures[mx, kingToCheck.Pos.Y].Color == col)
                            _boardFigures[mx, kingToCheck.Pos.Y].IsBlocked = false;
                    }
                    
                    return;
                }
            }
        }
    }
    
    private void _blockHorizontalRightLineOnKing(Figure.ColorT col)
        // used to block figures covering col king from being killed on horizontal right line
    {
        Figure kingToCheck = _colorMetadataMap[(int)col].King;

        int mx = -1;
        for (int x = kingToCheck.Pos.X + 1; x <= MaxPos; ++x)
        {
            if (!_isEmpty(x, kingToCheck.Pos.Y))
                // figure detected
            {
                if (mx == -1)
                    // first figure on the line
                {
                    if (_boardFigures[x, kingToCheck.Pos.Y].TextureIndex == _colorMetadataMap[(int)col].EnemyRook ||
                        _boardFigures[x, kingToCheck.Pos.Y].TextureIndex == _colorMetadataMap[(int)col].EnemyQueen)
                        // if first figure is attacking king process allowed tiles and return
                    {
                        if (_kingAttackingFigure != null)
                            continue;
                            // throw new ApplicationException("Double check detected - invalid move");
                        
                        _kingAttackingFigure = _boardFigures[x, kingToCheck.Pos.Y];
                        _lastAllowedTilesCount = x - kingToCheck.Pos.X - 1;
                        _lastAllowedTilesArr = new BoardPos[_lastAllowedTilesCount];

                        for (int i = 0; i < _lastAllowedTilesCount; ++i)
                        {
                            _lastAllowedTilesArr[i].Y = kingToCheck.Pos.Y;
                            _lastAllowedTilesArr[i].X = kingToCheck.Pos.X + 1 + i;
                            _blockedTiles[(int)col][kingToCheck.Pos.X + 1 + i, kingToCheck.Pos.Y] |= TileState.AllowedTile;
                        }
                    }
                    
                    mx = x;
                }
                else
                    // second figure detected 
                {
                    if (_boardFigures[x, kingToCheck.Pos.Y].Color != col)
                        // second fig is enemy
                    {
                        if (_boardFigures[mx, kingToCheck.Pos.Y].Color == col)
                            // second fig is enemy and first is ally
                        {
                            if (_boardFigures[x, kingToCheck.Pos.Y].TextureIndex == _colorMetadataMap[(int)col].EnemyRook ||
                                _boardFigures[x, kingToCheck.Pos.Y].TextureIndex == _colorMetadataMap[(int)col].EnemyQueen)
                                // first fig (ally) blocked by rook or queen
                                _boardFigures[mx, kingToCheck.Pos.Y].IsBlocked = true;
                            else
                                // enemy figure is not attacking ally free to go
                                _boardFigures[mx, kingToCheck.Pos.Y].IsBlocked = false;
                        }
                    }
                    else
                        // second fig is ally
                    {
                        // first fig cannot attack king so second is unblocked
                        _boardFigures[x, kingToCheck.Pos.Y].IsBlocked = false;

                        // if we are sure first one is ally, he is free to go
                        if (_boardFigures[mx, kingToCheck.Pos.Y].Color == col)
                            _boardFigures[mx, kingToCheck.Pos.Y].IsBlocked = false;
                    }
                    
                    return;
                }
            }
        }
    }
    
    private void _blockVerticalLowLineOnKing(Figure.ColorT col)
        // used to block figures covering col king from being killed on vertical lower line
    {
        Figure kingToCheck = _colorMetadataMap[(int)col].King;

        int my = -1;
        for (int y = kingToCheck.Pos.Y - 1; y >= MinPos; --y)
        {
            if (!_isEmpty(kingToCheck.Pos.X, y))
                // figure detected
            {
                if (my == -1)
                    // first figure on the line
                {
                    if (_boardFigures[kingToCheck.Pos.X, y].TextureIndex == _colorMetadataMap[(int)col].EnemyRook ||
                        _boardFigures[kingToCheck.Pos.X, y].TextureIndex == _colorMetadataMap[(int)col].EnemyQueen)
                        // if first figure is attacking king process allowed tiles and return
                    {
                        if (_kingAttackingFigure != null)
                            continue;
                            // throw new ApplicationException("Double check detected - invalid move");
                        
                        _kingAttackingFigure = _boardFigures[kingToCheck.Pos.X, y];
                        _lastAllowedTilesCount = kingToCheck.Pos.Y - y - 1;
                        _lastAllowedTilesArr = new BoardPos[_lastAllowedTilesCount];

                        for (int i = 0; i < _lastAllowedTilesCount; ++i)
                        {
                            _lastAllowedTilesArr[i].Y = y + 1 + i;
                            _lastAllowedTilesArr[i].X = kingToCheck.Pos.X;
                            _blockedTiles[(int)col][kingToCheck.Pos.X, y + 1 + i] |= TileState.AllowedTile;
                        }
                    }

                    my = y;
                }
                else
                    // second figure detected 
                {
                    if (_boardFigures[kingToCheck.Pos.X, y].Color != col)
                        // second fig is enemy
                    {
                        if (_boardFigures[kingToCheck.Pos.X, my].Color == col)
                            // second fig is enemy and first is ally
                        {
                            if (_boardFigures[kingToCheck.Pos.X, y].TextureIndex == _colorMetadataMap[(int)col].EnemyRook ||
                                _boardFigures[kingToCheck.Pos.X, y].TextureIndex == _colorMetadataMap[(int)col].EnemyQueen)
                                // first fig (ally) blocked by rook or queen
                                _boardFigures[kingToCheck.Pos.X, my].IsBlocked = true;
                            else
                                // enemy figure is not attacking ally free to go
                                _boardFigures[kingToCheck.Pos.X, my].IsBlocked = false;
                        }
                    }
                    else
                        // second fig is ally
                    {
                        // first fig cannot attack king so second is unblocked
                        _boardFigures[kingToCheck.Pos.X, y].IsBlocked = false;

                        // if we are sure first one is ally, he is free to go
                        if (_boardFigures[kingToCheck.Pos.X, my].Color == col)
                            _boardFigures[kingToCheck.Pos.X, my].IsBlocked = false;
                    }
                    
                    return;
                }
            }
        }
    }

    private void _blockVerticalUpLineOnKing(Figure.ColorT col)
        // used to block figures covering col king from being killed on vertical upper line
    {
        Figure kingToCheck = _colorMetadataMap[(int)col].King;

        int my = -1;
        for (int y = kingToCheck.Pos.Y + 1; y <= MaxPos; ++y)
        {
            if (!_isEmpty(kingToCheck.Pos.X, y))
                // figure detected
            {
                if (my == -1)
                    // first figure on the line
                {
                    if (_boardFigures[kingToCheck.Pos.X, y].TextureIndex == _colorMetadataMap[(int)col].EnemyRook ||
                        _boardFigures[kingToCheck.Pos.X, y].TextureIndex == _colorMetadataMap[(int)col].EnemyQueen)
                        // if first figure is attacking king process allowed tiles and return
                    {
                        if (_kingAttackingFigure != null)
                            continue;
                            // throw new ApplicationException("Double check detected - invalid move");
                        
                        _kingAttackingFigure = _boardFigures[kingToCheck.Pos.X, y];
                        _lastAllowedTilesCount = y - kingToCheck.Pos.Y - 1;
                        _lastAllowedTilesArr = new BoardPos[_lastAllowedTilesCount];

                        for (int i = 0; i < _lastAllowedTilesCount; ++i)
                        {
                            _lastAllowedTilesArr[i].Y = kingToCheck.Pos.Y + 1 + i;
                            _lastAllowedTilesArr[i].X = kingToCheck.Pos.X;
                            _blockedTiles[(int)col][kingToCheck.Pos.X, kingToCheck.Pos.Y + 1 + i] |= TileState.AllowedTile;
                        }
                    }

                    my = y;
                }
                else
                    // second figure detected 
                {
                    if (_boardFigures[kingToCheck.Pos.X, y].Color != col)
                        // second fig is enemy
                    {
                        if (_boardFigures[kingToCheck.Pos.X, my].Color == col)
                            // second fig is enemy and first is ally
                        {
                            if (_boardFigures[kingToCheck.Pos.X, y].TextureIndex == _colorMetadataMap[(int)col].EnemyRook ||
                                _boardFigures[kingToCheck.Pos.X, y].TextureIndex == _colorMetadataMap[(int)col].EnemyQueen)
                                // first fig (ally) blocked by rook or queen
                                _boardFigures[kingToCheck.Pos.X, my].IsBlocked = true;
                            else
                                // enemy figure is not attacking ally free to go
                                _boardFigures[kingToCheck.Pos.X, my].IsBlocked = false;
                        }
                    }
                    else
                        // second fig is ally
                    {
                        // first fig cannot attack king so second is unblocked
                        _boardFigures[kingToCheck.Pos.X, y].IsBlocked = false;

                        // if we are sure first one is ally, he is free to go
                        if (_boardFigures[kingToCheck.Pos.X, my].Color == col)
                            _boardFigures[kingToCheck.Pos.X, my].IsBlocked = false;
                    }
                    
                    return;
                }
            }
        }
    }

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
                        if (_kingAttackingFigure != null)
                            continue;
                            // throw new ApplicationException("Double check detected - invalid move");
                        
                        
                        _kingAttackingFigure = _boardFigures[nx, ny];
                        _lastAllowedTilesCount = Math.Abs(kingToCheck.Pos.Y - ny) - 1;
                        _lastAllowedTilesArr = new BoardPos[_lastAllowedTilesCount];

                        nx = kingToCheck.Pos.X + Bishop.XMoves[dir];
                        ny = kingToCheck.Pos.Y + Bishop.YMoves[dir];
                    
                        for (int i = 0; i < _lastAllowedTilesCount; ++i)
                        {
                            _lastAllowedTilesArr[i].X = nx;
                            _lastAllowedTilesArr[i].Y = ny;
                            _blockedTiles[(int)col][nx, ny] |= TileState.AllowedTile;
                            nx += Bishop.XMoves[dir];
                            ny += Bishop.YMoves[dir];
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
                            if (_boardFigures[nx, ny].TextureIndex == _colorMetadataMap[(int)col].EnemyRook ||
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

        
        // TODO: remove if works
        // if (_selectedFigure.Pos.X - move.X < 0)
        //     // short castling
        // {
        //     _boardFigures[MaxPos, move.Y].Pos = new BoardPos(King.ShortCastlingRookX, move.Y);
        //     _boardFigures[King.ShortCastlingRookX, move.Y] = _boardFigures[MaxPos, move.Y];
        //     _boardFigures[MaxPos, move.Y] = null;
        // }
        // else
        //     // long castling
        // {
        //     _boardFigures[MinPos, move.Y].Pos = new BoardPos(King.LongCastlingRookX, move.Y);
        //     _boardFigures[King.LongCastlingRookX, move.Y] = _boardFigures[MinPos, move.Y];
        //     _boardFigures[MinPos, move.Y] = null;
        // }
    }
    
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

    bool _checkForPositionRepetitions(string pos)
    {
        const int MaxRepetitions = 3;
        
        if (_layoutDict.ContainsKey(pos))
            if ((_layoutDict[pos] += 1) == MaxRepetitions) return true;
        else _layoutDict.Add(pos, 1);

        return false;
    }

    private void _processCheckMate(Figure.ColorT lostColor)
        // checks if there are any legit moves and if not anonunces win
    {
        var range = _colorMetadataMap[(int)_movingColor].AliesRangeOnFigArr;
        for (int i = range[0]; i < range[1]; ++i)
            if (_figuresArray[i].GetMoves().movesCount != 0) return;
        
        _announceWin();
    }

    private bool _isEmpty(int x, int y) => _boardFigures[x, y] == null;
    
// -----------------------------------
// debugging and testing methods
// -----------------------------------

    private void _drawDesiredBishopColoredTiles(Figure.ColorT col)
        // used in tests - debug only
    {
        int posMod = col == Figure.ColorT.White ? 1 : 0;
        
        for (int x = MinPos; x <= MaxPos; ++x)
        for (int y = MinPos; y <= MaxPos; ++y)
            if (Math.Abs(x - y) % 2 == posMod)
                _spriteBatch.Draw(TileHighlightersTextures[(int)TileHighlighters.SelectedTile], Translate(new BoardPos(x, y)), Color.White);
    }

    private ulong[] _testMoveGeneration(int depth)
    {
        ulong[] ret = new ulong[depth];

        var range = _colorMetadataMap[(int)_movingColor].AliesRangeOnFigArr;
        for (int i = range[0]; i < range[1]; ++i)
            if (_figuresArray[i].IsAlive)
            {
                var mv = _figuresArray[i].GetMoves();
                ret[0] += (ulong)mv.movesCount;
                
                // no need to process all moves when deeper depth is not expected
                if (depth == 1) continue;
                
                for (int j = 0; j < mv.movesCount; ++j)
                {
                    _processMove(mv.moves[j]);
                    var recResult = _testMoveGeneration(depth - 1);
                    Array.Copy(recResult, 0, ret, 1, recResult.Length);
                    // TODO: undo move
                    throw new NotImplementedException("implement undo move");
                }
            }

        return ret;
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
    public LinkedList<Move> MovesHistory => _movesHistory;
    // contains all made moves, used in some function, which needs historical data e.g. el passant moves generators
    // TODO: in future to save games in some database
    public Figure[,] BoardFigures => _boardFigures;
    // used mostly in figure's GetMoves method
    public TileState[][,] BlockedTiles => _blockedTiles;
    // used during moves generation to filter illegal moves
    public bool IsHold => _isHold;

    public Figure.ColorT MovingColor => _movingColor;

    public bool IsChecked => _kingAttackingFigure != null;
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
    
    private ColorMetadata[] _colorMetadataMap;
    // metadata per figures color about players figures deck

    private BoardPos[] _lastAllowedTilesArr; // TODO: temporary
    private int _lastAllowedTilesCount;
    // used to remove old allowed tiles
    
    private Figure[,] _boardFigures;
    // actual figures on board used to map positions to figures
    
    private Layout _startFiguresLayout;
    // storing starting position used to restore start layout 

    private Figure[] _figuresArray;
    // used to look up throughout all figures for e.g. to process all enemy moves
    
    private Figure.ColorT _movingColor;
    // actually moving color

    private LinkedList<Move> _movesHistory;

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
    
    // to allow drawing from inside of the class
    private SpriteBatch _spriteBatch;
    
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

    
    public static readonly Layout BasicBeginningLayout = new Layout
    {
        FigArr = new Figure[]
        {
            new Pawn(0,1, Figure.ColorT.White),
            new Pawn(1,1, Figure.ColorT.White),
            new Pawn(2,1, Figure.ColorT.White),
            new Pawn(3,1, Figure.ColorT.White),
            new Pawn(4,1, Figure.ColorT.White),
            new Pawn(5,1, Figure.ColorT.White),
            new Pawn(6,1, Figure.ColorT.White),
            new Pawn(7,1, Figure.ColorT.White),
            new Rook(0, 0, Figure.ColorT.White),
            new Knight(1, 0, Figure.ColorT.White),
            new Bishop(2,0, Figure.ColorT.White),
            new Queen(3, 0, Figure.ColorT.White),
            new King(4,0, Figure.ColorT.White),
            new Bishop(5,0, Figure.ColorT.White),
            new Knight(6, 0, Figure.ColorT.White),
            new Rook(7, 0, Figure.ColorT.White),
            new Pawn(0,6, Figure.ColorT.Black),
            new Pawn(1,6, Figure.ColorT.Black),
            new Pawn(2,6, Figure.ColorT.Black),
            new Pawn(3,6, Figure.ColorT.Black),
            new Pawn(4,6, Figure.ColorT.Black),
            new Pawn(5,6, Figure.ColorT.Black),
            new Pawn(6,6, Figure.ColorT.Black),
            new Pawn(7,6, Figure.ColorT.Black),
            new Rook(0, 7, Figure.ColorT.Black),
            new Knight(1, 7, Figure.ColorT.Black),
            new Bishop(2,7, Figure.ColorT.Black),
            new Queen(3, 7, Figure.ColorT.Black),
            new King(4,7, Figure.ColorT.Black),
            new Bishop(5,7, Figure.ColorT.Black),
            new Knight(6, 7, Figure.ColorT.Black),
            new Rook(7, 7, Figure.ColorT.Black),
        },
        FirstBlackFig = 16,
        StartingColor = Figure.ColorT.White
    };
    
    public static readonly Layout BasicBeginningWoutPawnsLayout = new Layout
    {
        FigArr = new Figure[]
        {
            new Rook(0, 0, Figure.ColorT.White),
            new Knight(1, 0, Figure.ColorT.White),
            new Bishop(2,0, Figure.ColorT.White),
            new Queen(3, 0, Figure.ColorT.White),
            new King(4,0, Figure.ColorT.White),
            new Bishop(5,0, Figure.ColorT.White),
            new Knight(6, 0, Figure.ColorT.White),
            new Rook(7, 0, Figure.ColorT.White),
            new Rook(0, 7, Figure.ColorT.Black),
            new Knight(1, 7, Figure.ColorT.Black),
            new Bishop(2,7, Figure.ColorT.Black),
            new Queen(3, 7, Figure.ColorT.Black),
            new King(4,7, Figure.ColorT.Black),
            new Bishop(5,7, Figure.ColorT.Black),
            new Knight(6, 7, Figure.ColorT.Black),
            new Rook(7, 7, Figure.ColorT.Black),
        },
        FirstBlackFig = 8,
        StartingColor = Figure.ColorT.White

    };
    
    public static readonly Layout PawnPromLayout = new Layout
    {
        FigArr = new Figure[]
        {
            new Pawn(6, 3, Figure.ColorT.White),
            new King(0,0 , Figure.ColorT.White),
            new Pawn(5,6, Figure.ColorT.Black),
            new King(0, 7, Figure.ColorT.Black)
        },
        FirstBlackFig = 2,
        StartingColor = Figure.ColorT.White

    };
    
    public static readonly Layout CastlingLayout = new Layout
    {
        FigArr = new Figure[]
        {
            new King(4, 0, Figure.ColorT.White),
            new Rook(0, 0, Figure.ColorT.White),
            new Rook(7, 0, Figure.ColorT.White),
            new King(0,7, Figure.ColorT.Black),
        },
        FirstBlackFig = 3,
        StartingColor = Figure.ColorT.White

    };

    public static readonly Layout DiagTest = new Layout
    {
        FigArr = new Figure[]
        {
            new Bishop(4, 4, Figure.ColorT.White),
            new Queen(3, 3, Figure.ColorT.White),
            new King(0, 7, Figure.ColorT.White),
            new King(7,0, Figure.ColorT.Black),
        },
        FirstBlackFig = 2,
        StartingColor = Figure.ColorT.White

    };
    
    public static readonly Layout BlockTest = new Layout
    {
        FigArr = new Figure[]
        {
            new King(3, 0, Figure.ColorT.White),
            new Rook(1,1, Figure.ColorT.Black),
            new King(0, 7, Figure.ColorT.Black),
        },
        FirstBlackFig = 1,
        StartingColor = Figure.ColorT.White
    };
}