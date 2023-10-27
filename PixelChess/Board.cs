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
        foreach (var fig in _startFiguresLayout)
            fig.Parent = this;

        _figuresList = new Figure[_startFiguresLayout.Length];
        _startFiguresLayout.CopyTo(_figuresList, 0);

        foreach (var fig in _figuresList)
        {
            _boardFigures[fig.Pos.X, fig.Pos.Y] = fig;

            if (fig.TextureIndex == ChessComponents.BlackKing) _blackKing = fig;
            else if (fig.TextureIndex == ChessComponents.WhiteKing) _whiteKing = fig;
        }

        if (_blackKing == null || _whiteKing == null)
            throw new ApplicationException("Starting layout has to contain white and black king!");

        _movesList = new LinkedList<Move>();
        // Sentinel
        _movesList.AddFirst(new Move(0, 0, new BoardPos(0, 0), ChessComponents.Board));

        _moveCounter = 0;
        _movingColor = Figure.ColorT.White;
        
        _blockTiles(_blackFirstIndex, _figuresList.Length);
        _blockFigures();
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
    {
        if (!pos.isOnBoard() || _boardFigures[pos.X, pos.Y] == null || _boardFigures[pos.X, pos.Y].Color != _movingColor
            || _blockedTiles[pos.X, pos.Y] == TileState.BlockedFigure)
        {
            _selectedFigure = null;
            return;
        }

        if (_checkedKing != null && _boardFigures[pos.X, pos.Y].TextureIndex != ChessComponents.WhiteKing &&
            _boardFigures[pos.X, pos.Y].TextureIndex != ChessComponents.BlackKing)
        {
            _selectedFigure = null;
            return;
        }
        
        _selectedFigure = _boardFigures[pos.X, pos.Y];
        _isHold = true;
        _selectedFigure.IsAlive = false;
    }

    public (bool WasHit, BoardPos.MoveType mType) DropFigure(BoardPos pos)
    {
        if (_selectedFigure == null || !pos.isOnBoard()) return (false, BoardPos.MoveType.NormalMove);
        
        _selectedFigure.IsAlive = true;
        _isHold = false;

        var moves = _selectedFigure.GetMoves();
        
        for (int i = 0; i < moves.movesCount; ++i)
            if (pos == moves.moves[i])
                return (true, _processFigure(moves.moves[i]));

        return (false, BoardPos.MoveType.NormalMove);
    }
    
    public void Promote(Figure promFig)
    {
        if (promFig == null)
            return;
        
#if DEBUG
        if (_promotionPawn == null)
        {
            Console.Error.WriteLine("[ERROR] Promotion pawn is null");
            return;
        }
#endif

        for(int i = 0; i < _figuresList.Length; ++i)
        {
            if (Object.ReferenceEquals(_figuresList[i], _promotionPawn))
            {
                _figuresList[i] = promFig;
                _boardFigures[promFig.Pos.X, promFig.Pos.Y] = promFig;
                _promotionPawn = null;
            }
        }
    }
    
    public bool IsSelectedFigure()
    {
        return _selectedFigure != null;
    }

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

        if (_checkedKing != null)
        {
            _spriteBatch.Draw(TileHighlightersTextures[(int)TileHighlighters.KingAttackTile], Translate(_checkedKing.Pos), Color.White);
        }
    }

    private void _drawStaticFigures()
    {
        foreach (var actFig in _figuresList)
        {
            if (actFig.IsAlive)
            {
                _spriteBatch.Draw(ComponentsTextures[(int)actFig.TextureIndex], Translate(actFig.Pos), Color.White);
            }
        }
    }

    private void _drawHoveringFigure()
    {
        if (_isHold)
        {
            var mState = Mouse.GetState();
                
            _spriteBatch.Draw(ComponentsTextures[(int)_selectedFigure.TextureIndex],
                CenterFigurePosOnMouse(mState.X, mState.Y), Color.White);
        }
    }
    
// ------------------------------
// Private methods zone
// ------------------------------

    private void _blockFigures()
    // should be invoked after blocking tiles
    {
        Figure kingToCheck;
        ChessComponents enemyRook;
        ChessComponents enemyBishop;
        ChessComponents enemyQueen;

        if (_movingColor == Figure.ColorT.White)
        {
            kingToCheck = _whiteKing;
            enemyRook = ChessComponents.BlackRook;
            enemyBishop = ChessComponents.BlackBishop;
            enemyQueen = ChessComponents.BlackQueen;
        }
        else
        {
            kingToCheck = _blackKing;
            enemyRook = ChessComponents.WhiteRook;
            enemyBishop = ChessComponents.WhiteBishop;
            enemyQueen = ChessComponents.WhiteQueen;
        }

        // ------------------------------
        // rook lines checks
        // ------------------------------
        
        // TODO: PACK IT!!!

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
                    if (_boardFigures[x, kingToCheck.Pos.Y].TextureIndex == enemyRook ||
                        _boardFigures[x, kingToCheck.Pos.Y].TextureIndex == enemyQueen)
                    {
                        _blockedTiles[mx, kingToCheck.Pos.Y] = TileState.BlockedFigure;
                    }
        
                    break;
                }
            }
        }

        mx = -1;
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
                    if (_boardFigures[x, kingToCheck.Pos.Y].TextureIndex == enemyRook ||
                        _boardFigures[x, kingToCheck.Pos.Y].TextureIndex == enemyQueen)
                    {
                        _blockedTiles[mx, kingToCheck.Pos.Y] = TileState.BlockedFigure;
                    }
        
                    break;
                }
            }
        }

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
                    if (_boardFigures[kingToCheck.Pos.X, y].TextureIndex == enemyRook ||
                        _boardFigures[kingToCheck.Pos.X, y].TextureIndex == enemyQueen)
                    {
                        _blockedTiles[kingToCheck.Pos.X, my] = TileState.BlockedFigure;
                    }
        
                    break;
                }
            }
        }

        my = -1;
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
                    if (_boardFigures[kingToCheck.Pos.X, y].TextureIndex == enemyRook ||
                        _boardFigures[kingToCheck.Pos.X, y].TextureIndex == enemyQueen)
                    {
                        _blockedTiles[kingToCheck.Pos.X, my] = TileState.BlockedFigure;
                    }
        
                    break;
                }
            }
        }

        for (int i = 0; i < 4; ++i)
        {
            int nx = kingToCheck.Pos.X;
            int ny = kingToCheck.Pos.Y;
            mx = -1;
            
            for (int j = 0; j < Bishop.MoveLimMap[kingToCheck.Pos.X, kingToCheck.Pos.Y][i]; ++j)
            {
                nx += Bishop.XMoves[i];
                ny += Bishop.YMoves[i];

                if (!_isEmpty(nx, ny))
                {
                    if (mx == -1)
                    {
                        mx = nx;
                        my = ny;
                    }
                    else
                    {
                        if (_boardFigures[nx, ny].TextureIndex == enemyBishop ||
                            _boardFigures[nx, ny].TextureIndex == enemyQueen)
                        {
                            _blockedTiles[mx, my] = TileState.BlockedFigure;
                        }
        
                        break;
                    }
                }
            }
        }
    }
    
    private void _blockTiles(int beg, int end)
    {
        _blockedTiles = new TileState[BoardSize, BoardSize];

        for (int i = beg; i < end; ++i)
        {
            var moves = _figuresList[i].GetMoves();

            for (int j = 0; j < moves.movesCount; ++j)
            {
                _blockedTiles[moves.moves[j].X, moves.moves[j].Y] = Board.TileState.BlockedTile;
            }
        }
    }

    private BoardPos.MoveType _processFigure(BoardPos move)
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
                _killFigure(MovesList.Last.Value.NewPos);
                break;
        }
        
        _moveFigure(move);
        _selectedFigure = null;
        _checkedKing = null; // can move only when available slot 
        _changePlayingColor();
        ++_moveCounter;
        return move.MoveT;
    }

    private void _moveFigure(BoardPos move)
    {
        _movesList.AddLast(new Move(_selectedFigure.Pos.X, _selectedFigure.Pos.Y, move, _selectedFigure.TextureIndex));
        
        _boardFigures[_selectedFigure.Pos.X, _selectedFigure.Pos.Y] = null;
        _boardFigures[move.X, move.Y] = _selectedFigure;
        _selectedFigure.Pos = move;
        _selectedFigure.IsMoved = true;
    }

    private void _killFigure(BoardPos move)
    {
        _boardFigures[move.X, move.Y].IsAlive = false;
        _boardFigures[move.X, move.Y] = null;
    }


    private void _changePlayingColor()
    {
        if (_movingColor == Figure.ColorT.White)
        {
            _movingColor = Figure.ColorT.Black;
            _blockTiles(0, _blackFirstIndex);
            if (_blockedTiles[_blackKing.Pos.X, _blackKing.Pos.Y] == TileState.BlockedTile) _checkedKing = _blackKing;
        }
        else
        {
            _movingColor = Figure.ColorT.White;
            _blockTiles(_blackFirstIndex, _figuresList.Length);
            if (_blockedTiles[_whiteKing.Pos.X, _whiteKing.Pos.Y] == TileState.BlockedTile) _checkedKing = _whiteKing;
        }

        _blockFigures();
    }

    private void _castleKing(BoardPos move)
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

    private bool _isEmpty(int x, int y) => _boardFigures[x, y] == null;

// ------------------------------
// public types
// ------------------------------

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

    public struct Layout
    {
        public int FirstBlackFig;
        public Figure[] startLayout;
    }

    public enum TileState
    {
        UnblockedTile,
        BlockedTile,
        BlockedFigure,
    }
    
// ------------------------------
// public properties
// ------------------------------
    public Figure PromotionPawn => _promotionPawn;
    public LinkedList<Move> MovesList => _movesList;
    public Figure[,] BoardFigures => _boardFigures;
    public TileState[,] BlockedTiles => _blockedTiles;
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

    private Figure _blackKing;
    private Figure _whiteKing;
    private Figure _checkedKing;
    private Figure _selectedFigure;
    private Figure _promotionPawn;
    
    private Figure[,] _boardFigures;
    private Figure[] _startFiguresLayout;
    private Figure[] _figuresList;
    
    private Figure.ColorT _movingColor;

    private LinkedList<Move> _movesList;
    private TileState[,] _blockedTiles;
    
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