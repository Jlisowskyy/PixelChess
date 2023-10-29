using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PongGame;

public class Board
{
// --------------------------------
// type construction / setups
// --------------------------------
    public Board(Layout layout)
    {
        _startFiguresLayout = new Figure[layout.startLayout.Length];
        _importantFigMap = new ImportantFigures[]{ 
            new ImportantFigures(ChessComponents.BlackRook, ChessComponents.BlackBishop, ChessComponents.BlackQueen),
            new ImportantFigures(ChessComponents.WhiteRook, ChessComponents.BlackBishop, ChessComponents.BlackQueen) 
        };
        _blockedTiles = new TileState[][,] {
            new TileState[BoardSize, BoardSize],
            new TileState[BoardSize, BoardSize]
        };
        
        
        layout.startLayout.CopyTo(_startFiguresLayout, 0);

        _blackFirstIndex = layout.FirstBlackFig;
        _yTilesCordOnScreenBeg = YTilesBoardCordBeg;
        _xTilesCordOnScreenBeg = XTilesBoardCordBeg;
    }

    static Board()
    {
        ComponentsTextures = new Texture2D[Enum.GetNames(typeof(Board.ChessComponents)).Length];
        TileHighlightersTextures = new Texture2D[(int)Enum.GetValues(typeof(TileHighlighters)).Cast<TileHighlighters>().Max() + 1];
    }

    public void Initialize(int xOffset, int yOffset)
    {
        _xOffset = xOffset;
        _yOffset = yOffset;
        _xTilesCordOnScreenBeg = XTilesBoardCordBeg + xOffset;
        _yTilesCordOnScreenBeg = YTilesBoardCordBeg + yOffset;

        ResetBoard();
    }

    public void ResetBoard()
    {
        _boardFigures = new Figure[BoardSize, BoardSize];
        _figuresArray = new Figure[_startFiguresLayout.Length];
        
        _movesHistory = new LinkedList<Move>();
        _movesHistory.AddFirst(new Move(0, 0, new BoardPos(0, 0), ChessComponents.Board)); // Sentinel

        _moveCounter = 0;
        _movingColor = Figure.ColorT.White;
        _copyAndExtractMetadata();
    }

    public void ProcTimers(double spentTime)
    {
        if (_moveCounter == 0) return;

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

    public void StartGame(double whiteTime, double blackTime)
    {
        _whiteTime = whiteTime;
        _blackTime = blackTime;
    }

    // TODO: consider delegate here when check?
    public void SelectFigure(BoardPos pos)
        // if pos is correct and points to currently moving color's figure selects it as moving figure
    {
        if (!pos.isOnBoard() || _boardFigures[pos.X, pos.Y] == null || _boardFigures[pos.X, pos.Y].Color != _movingColor
            || _blockedTiles[(int)_movingColor][pos.X, pos.Y] == TileState.BlockedFigure)
        {
            _selectedFigure = null;
            return;
        }
        
        // TODO: add checks in figure?
        
        _selectedFigure = _boardFigures[pos.X, pos.Y];
        _isHold = true;
        _selectedFigure.IsAlive = false;
    }

    public (bool WasHit, BoardPos.MoveType mType) DropFigure(BoardPos pos)
        // method used to 'drop' figure on the board, if there is selected ony
    {
        if (_selectedFigure == null || !pos.isOnBoard()) return (false, BoardPos.MoveType.NormalMove);
        
        // flag used to indicate whether figure should be drew or not
        _selectedFigure.IsAlive = true;
        _isHold = false;

        // performs sanity check whether the move was legal
        var moves = _selectedFigure.GetMoves();
        for (int i = 0; i < moves.movesCount; ++i)
            if (pos == moves.moves[i])
                return (true, _processMove(moves.moves[i]));

        return (false, BoardPos.MoveType.NormalMove);
    }
    
    public void Promote(Figure promFig)
        // method used to communicate with PromotionMenu class and BoardClass
        // accepts desired figure to replace promoting pawn
    {
        if (promFig == null)
            return;

        // replaces pawn also on the figures Array
        for(int i = 0; i < _figuresArray.Length; ++i)
        {
            if (Object.ReferenceEquals(_figuresArray[i], _promotionPawn))
            {
                _figuresArray[i] = promFig;
                _boardFigures[promFig.Pos.X, promFig.Pos.Y] = promFig;
                _promotionPawn = null;
                
                // adds promoted pawn to important figures metadata
                _extractDataFromFig(promFig);
                return;
            }
        }
        
        throw new ApplicationException("Unexpected, promotion figure not found");
    }
    
    public bool IsSelectedFigure() => _selectedFigure != null;
    
    public Vector2 CenterFigurePosOnMouse(int x, int y) => new(x + _mouseCentX, y + _mouseCentY);
    
    public Vector2 Translate(BoardPos pos)
        => new Vector2(_xTilesCordOnScreenBeg + pos.X * FigureWidth, _yTilesCordOnScreenBeg - pos.Y * FigureHeight);

    public BoardPos Translate(int x, int y)
        => new BoardPos((int)((x - _xTilesCordOnScreenBeg) / FigureWidth), (int)((_yTilesCordOnScreenBeg + 68 - y) / FigureHeight));
    

// ------------------------------
// drawing method
// ------------------------------
    public void Draw()
    {
        _drawBoard();
        _drawHighlightedTiles();
        _drawStaticFigures();
        _drawHoveringFigure();
    }
    
    private void _drawBoard()
    {
        _spriteBatch.Draw(ComponentsTextures[(int)ChessComponents.Board], new Vector2(_xOffset, _yOffset), Color.White);
    }

    private void _drawHighlightedTiles()
        // draws applies highlighting layers on the board to inform player about allowed move and ongoing actions
    {
        if (IsSelectedFigure())
        {
            var movs = _selectedFigure.GetMoves();
            
            _spriteBatch.Draw(TileHighlightersTextures[(int)Board.TileHighlighters.SelectedTile], Translate(_selectedFigure.Pos), Color.White);
            
            for (int i = 0; i < movs.movesCount; ++i)
            {
                _spriteBatch.Draw(TileHighlightersTextures[(int)movs.moves[i].MoveT], Translate(movs.moves[i]), Color.White);
            }
        }

        if (_kingAttackingFigure != null)
        {
            _spriteBatch.Draw(TileHighlightersTextures[(int)TileHighlighters.KingAttackTile], Translate(_importantFigMap[(int)_movingColor].King.Pos), Color.White);
        }
    }

    private void _drawStaticFigures()
        // draws not moving figures on the board
    {
        foreach (var actFig in _figuresArray)
        {
            if (actFig.IsAlive)
            {
                _spriteBatch.Draw(ComponentsTextures[(int)actFig.TextureIndex], Translate(actFig.Pos), Color.White);
            }
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

    private void _drawDesiredBishopColoredTiles(Figure.ColorT col)
        // used in tests - debug only
    {
        int posMod = col == Figure.ColorT.White ? 1 : 0;
        
        for (int x = BoardPos.MinPos; x <= BoardPos.MaxPos; ++x)
        {
            for (int y = BoardPos.MinPos; y <= BoardPos.MaxPos; ++y)
            {
                if (Math.Abs(x - y) % 2 == posMod)
                {
                    _spriteBatch.Draw(TileHighlightersTextures[(int)TileHighlighters.SelectedTile], Translate(new BoardPos(x, y)), Color.White);
                }
            }
        }
    }
    
// ------------------------------
// Private methods zone
// ------------------------------

    private BoardPos.MoveType _processMove(BoardPos move)
        // calls special function to handle passed type of move, performs move consequences on board,
        // and finally hands on action to _processToNextRound(), which performs further actions
    {
        switch (move.MoveT)
        {
            case BoardPos.MoveType.NormalMove:
                break;
            case BoardPos.MoveType.AttackMove:
                _killFigure(move);
                break;
            case BoardPos.MoveType.PromotionMove:
                _promotionPawn = _selectedFigure;
                break;
            case BoardPos.MoveType.PromAndAttack:
                _promotionPawn = _selectedFigure;
                _killFigure(move);
                break;
            case BoardPos.MoveType.CastlingMove:
                _castleKing(move);
                break;
            case BoardPos.MoveType.ElPass:
                _killFigure(MovesHistory.Last.Value.NewPos);
                break;
        }
        
        _moveFigure(move); 
        _processToNextRound();
        return move.MoveT;
    }

    private void _processToNextRound()
    {
        _kingAttackingFigure = null;
        _changePlayingColor();

    }
    
    private void _changePlayingColor()
    {
        // if (_movingColor == Figure.ColorT.White)
        // {
        //     _movingColor = Figure.ColorT.Black;
        //     _blockTiles(0, _blackFirstIndex);
        // }
        // else
        // {
        //     _movingColor = Figure.ColorT.White;
        //     _blockTiles(_blackFirstIndex, _figuresList.Length); 
        // }

        // (_moveKing, _moveEnemyKing) = (_moveEnemyKing, _moveKing);
        // _blockFigures();
    }
    
    // private void _blockTiles(int beg, int end)
    // {
    //     _blockedTiles = new TileState[BoardSize, BoardSize];
    //
    //     for (int i = beg; i < end; ++i)
    //     {
    //         var moves = _figuresList[i].GetMoves();
    //
    //         for (int j = 0; j < moves.movesCount; ++j)
    //         {
    //             _blockedTiles[moves.moves[j].X, moves.moves[j].Y] = Board.TileState.BlockedTile;
    //
    //             if (_boardFigures[moves.moves[j].X, moves.moves[j].Y].TextureIndex == _moveKing.TextureIndex)
    //             {
    //                 _kingAttackingFigure = _figuresList[i];
    //             }
    //         }
    //     }
    // }

    private void _checkAllLinesOnKing(Figure.ColorT col)
        // used to block figures covering col king from being killed on all lines (diagonal and simple ones)
    {
        _checkHorizontalLeftLineOnKing(col);
        _checkHorizontalRightLineOnKing(col);
        _checkVerticalLowLineOnKing(col);
        _checkVerticalUpLineOnKing(col);
        
        for(int i = 0; i < 4; ++i)
            _checkDiagonal(col, i);
    }

    private void _checkHorizontalLeftLineOnKing(Figure.ColorT col)
        // used to block figures covering col king from being killed on horizontal left line
    {
        Figure kingToCheck = _importantFigMap[(int)col].King;

        int mx = -1;
        for (int x = kingToCheck.Pos.X - 1; x >= BoardPos.MinPos; --x)
        {
            if (!_isEmpty(x, kingToCheck.Pos.Y))
            {
                if (mx == -1)
                {
                    mx = x;
                }
                else
                {
                    if (_boardFigures[x, kingToCheck.Pos.Y].TextureIndex == _importantFigMap[(int)col].EnemyRook ||
                        _boardFigures[x, kingToCheck.Pos.Y].TextureIndex == _importantFigMap[(int)col].EnemyQueen)
                    {
                        _blockedTiles[(int)col][mx, kingToCheck.Pos.Y] = TileState.BlockedFigure;
                    }

                    break;
                }
            }
        }
    }
    
    private void _checkHorizontalRightLineOnKing(Figure.ColorT col)
        // used to block figures covering col king from being killed on horizontal right line
    {
        Figure kingToCheck = _importantFigMap[(int)col].King;

        int mx = -1;
        for (int x = kingToCheck.Pos.X + 1; x <= BoardPos.MaxPos; ++x)
        {
            if (!_isEmpty(x, kingToCheck.Pos.Y))
            {
                if (mx == -1)
                {
                    mx = x;
                }
                else
                {
                    if (_boardFigures[x, kingToCheck.Pos.Y].TextureIndex == _importantFigMap[(int)col].EnemyRook ||
                        _boardFigures[x, kingToCheck.Pos.Y].TextureIndex == _importantFigMap[(int)col].EnemyQueen)
                    {
                        _blockedTiles[(int)col][mx, kingToCheck.Pos.Y] = TileState.BlockedFigure;
                    }

                    break;
                }
            }
        }
    }
    
    private void _checkVerticalLowLineOnKing(Figure.ColorT col)
        // used to block figures covering col king from being killed on vertical lower line
    {
        Figure kingToCheck = _importantFigMap[(int)col].King;

        int my = -1;
        for (int y = kingToCheck.Pos.Y - 1; y >= BoardPos.MinPos; --y)
        {
            if (!_isEmpty(kingToCheck.Pos.X, y))
            {
                if (my == -1)
                {
                    my = y;
                }
                else
                {
                    if (_boardFigures[kingToCheck.Pos.X, y].TextureIndex == _importantFigMap[(int)col].EnemyRook ||
                        _boardFigures[kingToCheck.Pos.X, y].TextureIndex == _importantFigMap[(int)col].EnemyQueen)
                    {
                        _blockedTiles[(int)col][kingToCheck.Pos.X, my] = TileState.BlockedFigure;
                    }
        
                    break;
                }
            }
        }
    }

    private void _checkVerticalUpLineOnKing(Figure.ColorT col)
        // used to block figures covering col king from being killed on vertical upper line
    {
        Figure kingToCheck = _importantFigMap[(int)col].King;

        int my = -1;
        for (int y = kingToCheck.Pos.Y + 1; y <= BoardPos.MaxPos; ++y)
        {
            if (!_isEmpty(kingToCheck.Pos.X, y))
            {
                if (my == -1)
                {
                    my = y;
                }
                else
                {
                    if (_boardFigures[kingToCheck.Pos.X, y].TextureIndex == _importantFigMap[(int)col].EnemyRook ||
                        _boardFigures[kingToCheck.Pos.X, y].TextureIndex == _importantFigMap[(int)col].EnemyQueen)
                    {
                        _blockedTiles[(int)col][kingToCheck.Pos.X, my] = TileState.BlockedFigure;
                    }

                    break;
                }
            }
        }
    }

    private void _checkDiagonal(Figure.ColorT col, int dir)
        // used to block figures covering col king from being killed on diagonals
    {
        Figure kingToCheck = _importantFigMap[(int)col].King;
        
        int nx = kingToCheck.Pos.X;
        int ny = kingToCheck.Pos.Y;
        int mx = -1;
        int my = 0;
        
        for (int j = 0; j < Bishop.MoveLimMap[kingToCheck.Pos.X, kingToCheck.Pos.Y][dir]; ++j)
        {
            nx += Bishop.XMoves[dir];
            ny += Bishop.YMoves[dir];
        
            if (!_isEmpty(nx, ny))
            {
                if (mx == -1)
                {
                    mx = nx;
                    my = ny;
                }
                else
                {
                    if (_boardFigures[nx, ny].TextureIndex == _importantFigMap[(int)col].EnemyBishop ||
                        _boardFigures[nx, ny].TextureIndex == _importantFigMap[(int)col].EnemyQueen)
                    {
                        _blockedTiles[(int)col][mx, my] = TileState.BlockedFigure;
                    }
        
                    break;
                }
            }
        }
    }


    private void _moveFigure(BoardPos move)
        // adds moves to history and applies move consequences to all Board structures
    {
        _movesHistory.AddLast(new Move(_selectedFigure.Pos.X, _selectedFigure.Pos.Y, move, _selectedFigure.TextureIndex));
        
        _boardFigures[_selectedFigure.Pos.X, _selectedFigure.Pos.Y] = null;
        _boardFigures[move.X, move.Y] = _selectedFigure;
        _selectedFigure.Pos = move;
        _selectedFigure.IsMoved = true;
        ++_moveCounter;
        _selectedFigure = null;
    }

    private void _killFigure(BoardPos move)
        // removes figures from board->figures map
    {
        _boardFigures[move.X, move.Y].IsAlive = false;
        _boardFigures[move.X, move.Y] = null;
    }

    private void _castleKing(BoardPos move)
        // assumes passed move is correct castling and selected figures is moving king
        // only applies consequences of desired castling 
    {
        if (_selectedFigure.Pos.X - move.X < 0)
            // short castling
        {
            _boardFigures[BoardPos.MaxPos, move.Y].Pos = new BoardPos(King.ShortCastlingRookX, move.Y);
            _boardFigures[King.ShortCastlingRookX, move.Y] = _boardFigures[BoardPos.MaxPos, move.Y];
            _boardFigures[BoardPos.MaxPos, move.Y] = null;
        }
        else
            // long castling
        {
            _boardFigures[BoardPos.MinPos, move.Y].Pos = new BoardPos(King.LongCastlingRookX, move.Y);
            _boardFigures[King.LongCastlingRookX, move.Y] = _boardFigures[BoardPos.MinPos, move.Y];
            _boardFigures[BoardPos.MinPos, move.Y] = null;
        }
    }

    private void _translateFenToArray(string fenInput)
        // translates fen input to array representation of figures
    {
        // TODO
        throw new NotImplementedException();
    }

    private void _copyAndExtractMetadata()
        // performs deep copy of saved starting layout to in-game figures array
        // used mainly when initialising game or restarting to start position
        // also analyzes the layout and extract metadata about sliding figures and kings
    {
        for(int i = 0; i < _startFiguresLayout.Length; ++i)
        {
            _figuresArray[i] = _startFiguresLayout[i].Clone();
            Figure fig = _figuresArray[i];
            
            _boardFigures[fig.Pos.X, fig.Pos.Y] = fig;
            fig.Parent = this;

            _extractDataFromFig(fig);
        }

        if (_importantFigMap[(int)Figure.ColorT.White] == null || _importantFigMap[(int)Figure.ColorT.Black] == null)
            throw new ApplicationException("Starting layout has to contain white and black king!");
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
                _importantFigMap[(int)Figure.ColorT.White].Rooks.AddLast(fig);
                break;
            case ChessComponents.WhiteQueen:
                _importantFigMap[(int)Figure.ColorT.White].Queens.AddLast(fig);
                break;
            case ChessComponents.WhiteKing:
                if (_importantFigMap[(int)Figure.ColorT.White].King == null)
                    _importantFigMap[(int)Figure.ColorT.White].King = fig;
                else
                    throw new ApplicationException("Only one white king is expected!");
                break;
            case ChessComponents.BlackBishop:
                CheckBishopTiles();
                break;
            case ChessComponents.BlackRook:
                _importantFigMap[(int)Figure.ColorT.Black].Rooks.AddLast(fig);
                break;
            case ChessComponents.BlackQueen:
                _importantFigMap[(int)Figure.ColorT.Black].Queens.AddLast(fig);
                break;
            case ChessComponents.BlackKing:
                if (_importantFigMap[(int)Figure.ColorT.Black].King == null)
                    _importantFigMap[(int)Figure.ColorT.Black].King = fig;
                else
                    throw new ApplicationException("Only one black king is expected!");
                break;
        }

        void CheckBishopTiles()
            // determines whether bishops walks on white or black tiles
        {
            if (Math.Abs(fig.Pos.X - fig.Pos.Y) % 2 == 0)
                _importantFigMap[(int)fig.Color].BlackTilesBishops.AddLast(fig);
            else
                _importantFigMap[(int)fig.Color].WhiteTilesBishops.AddLast(fig);
        }
    }

    private bool _isEmpty(int x, int y) => _boardFigures[x, y] == null;

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
    // TODO: DONT UNDERSTAND THIS YET XDDD
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

    // used to explicitly hold saved layouts
    public struct Layout
    {
        public int FirstBlackFig;
        public Figure[] startLayout;
    }


    // used in moves filtering-maps where
    // - UnblockedTiles - means that move TO and FROM that field is possible, and king is allowed to move here
    // - BlockedTile - means that king is NOT allowed to move here
    // - BlockedFigure - means that figure on that field is NOT allowed to move, because would uncover the king
    public enum TileState
    {
        UnblockedTile,
        BlockedTile,
        BlockedFigure,
    }
    
    // metadata class used to hold information about specific figures of single color
    class ImportantFigures
    {
        public ImportantFigures(ChessComponents enRook, ChessComponents enBishop, ChessComponents enQueen)
        {
            EnemyBishop = enBishop;
            EnemyQueen = enQueen;
            EnemyRook = enRook;
        }
        
        public Figure King;
        public readonly LinkedList<Figure> BlackTilesBishops = new LinkedList<Figure>();
        public readonly LinkedList<Figure> WhiteTilesBishops = new LinkedList<Figure>();
        public readonly LinkedList<Figure> Rooks = new LinkedList<Figure>();
        public readonly LinkedList<Figure> Queens = new LinkedList<Figure>();

        public readonly ChessComponents EnemyRook;
        public readonly ChessComponents EnemyBishop;
        public readonly ChessComponents EnemyQueen;
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

    public double WhiteTime => _whiteTime;
    public double BlackTime => _blackTime;
    
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
    
    private readonly ImportantFigures[] _importantFigMap;
    // metadata per figures color about players figures deck
    
    private Figure[,] _boardFigures;
    // actual figures on board used to map positions to figures
    
    private Figure[] _startFiguresLayout;
    // storing starting position used to restore start layout 


    private Figure[] _figuresArray;
    // used to look up throughout all figures for e.g. to process all enemy moves
    
    private Figure.ColorT _movingColor;
    // actually moving color

    private LinkedList<Move> _movesHistory;

    private TileState[][,] _blockedTiles;
    // filtering maps used to block moves
    
    private bool _isHold = false;
    
    private int _yTilesCordOnScreenBeg;
    private int _xTilesCordOnScreenBeg;
    private int _xOffset;
    private int _yOffset;
    
    private double _whiteTime;
    private double _blackTime;

    private int _moveCounter;
    private int _blackFirstIndex;

    private const int _mouseCentX = - FigureWidth / 2;
    private const int _mouseCentY = - FigureHeight / 2;
    
// ------------------------------
// static fields
// ------------------------------

    private static SpriteBatch _spriteBatch;
    
    public static readonly Texture2D[] ComponentsTextures; 
    public static readonly Texture2D[] TileHighlightersTextures;

    public const double Second = 1000;
    public const double Minute = 60 * Second;
    public const double BasicWhiteTime = 10 * Minute;
    public const double BasicBlackTIme = 10 * Minute;
    
    public static SpriteBatch SpriteBatch
    {
        set => _spriteBatch = value;
    }
    
    public static readonly Layout BasicBeginningLayout = new Layout
    {
        startLayout = new Figure[]
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
    };
    
    public static readonly Layout PawnPromLayout = new Layout
    {
        startLayout = new Figure[]
        {
            new Pawn(6, 3, Figure.ColorT.White),
            new King(0,0 , Figure.ColorT.White),
            new Pawn(5,6, Figure.ColorT.Black),
            new King(0, 7, Figure.ColorT.Black)
        },
        FirstBlackFig = 2,
    };
    
    public static readonly Layout CastlingLayout = new Layout
    {
        startLayout = new Figure[]
        {
            new King(4, 0, Figure.ColorT.White),
            new Rook(0, 0, Figure.ColorT.White),
            new Rook(7, 0, Figure.ColorT.White),
            new King(0,7, Figure.ColorT.Black),
        },
        FirstBlackFig = 3,
    };

    public static readonly Layout DiagTest = new Layout
    {
        startLayout = new Figure[]
        {
            new Bishop(4, 4, Figure.ColorT.White),
            new Queen(3, 3, Figure.ColorT.White),
            new King(0, 7, Figure.ColorT.White),
            new King(7,0, Figure.ColorT.Black),
        },
        FirstBlackFig = 2,
    };
    
    public static readonly Layout BlockTest = new Layout
    {
        startLayout = new Figure[]
        {
            new King(3, 0, Figure.ColorT.White),
            new Rook(1,1, Figure.ColorT.Black),
            new King(0, 7, Figure.ColorT.Black),
        },
        FirstBlackFig = 1,
    };
}