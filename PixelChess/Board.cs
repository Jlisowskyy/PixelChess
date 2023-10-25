using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PongGame;

public class Board
{
    
// --------------------------------
// type construction / setups
// --------------------------------
    public Board(Figure[] figuresList)
    {
        _boardFigures = new Figure[BoardSize, BoardSize];
        _startFiguresLayout = new Figure[figuresList.Length];
        figuresList.CopyTo(_startFiguresLayout, 0);
        
        foreach (var fig in _startFiguresLayout)
            fig.Parent = this;

        _yTilesCordOnScreenBeg = YTilesBoardCordBeg;
        _xTilesCordOnScreenBeg = XTilesBoardCordBeg;


        _movesList = new LinkedList<Move>();
        // Sentinel
        _movesList.AddFirst(new Move(0, 0, new BoardPos(0,0), ChessComponents.Board));
    }

    static Board()
    {
        ComponentsTextures = new Texture2D[Enum.GetNames(typeof(Board.ChessComponents)).Length];
        TileHighlightersTextures = new Texture2D[Enum.GetNames(typeof(Board.TileHighlighters)).Length];
    }

    public void Initialize(int xOffset, int yOffset)
    {
        _figuresList = new Figure[_startFiguresLayout.Length];
        _startFiguresLayout.CopyTo(_figuresList, 0);

        foreach (var fig in _figuresList)
            _boardFigures[fig.Pos.X, fig.Pos.Y] = fig;

        _xOffset = xOffset;
        _yOffset = yOffset;
        _xTilesCordOnScreenBeg = XTilesBoardCordBeg + xOffset;
        _yTilesCordOnScreenBeg = YTilesBoardCordBeg + yOffset;
    }
    
// ------------------------------
// type interaction
// ------------------------------
    public void SelectFigure(BoardPos pos)
    {
        if (!pos.isOnBoard() || _boardFigures[pos.X, pos.Y] == null)
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
            case BoardPos.MoveType.KingAttackMove:
                break;
            case BoardPos.MoveType.ElPass:
                _killFigure(MovesList.Last.Value.NewPos);
                break;
        }
        
        _moveFigure(move);
        _selectedFigure = null;
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
        MoveTile,
        BasicAttackTile,
        PromotionTile,
        KingAttackTile,
        CastlingMove,
        SelectedTile,
    }
    
// ------------------------------
// public properties
// ------------------------------
    public Figure PromotionPawn => _promotionPawn;
    public LinkedList<Move> MovesList => _movesList;
    public Figure[,] BoardFigures => _boardFigures;
    public bool IsHold => _isHold;
    
    public const int XTilesBeg = 51;
    public const int YTilesBeg = 0;
    public const int XTilesBoardCordBeg = 51;
    public const int YTilesBoardCordBeg = 477;
    public const int Width = 600;
    public const int Height = 600;
    public const int FigureHeight = 68;
    public const int FigureWidth = 68;
    
// ------------------------------
// private variables
// ------------------------------
    
    private const int BoardSize = 8;
    private readonly Figure[,] _boardFigures;
    private Figure _selectedFigure;
    private Figure _promotionPawn;

    private readonly Figure[] _startFiguresLayout;
    private Figure[] _figuresList;

    private readonly LinkedList<Move> _movesList;
    
    private bool _isHold = false;
    private int _yTilesCordOnScreenBeg;
    private int _xTilesCordOnScreenBeg;
    private int _xOffset;
    private int _yOffset;

    private const int _mouseCentX = - FigureWidth / 2;
    private const int _mouseCentY = - FigureHeight / 2;
    
    public static readonly Texture2D[] ComponentsTextures; 
    public static readonly Texture2D[] TileHighlightersTextures;
    
// ------------------------------
// static fields
// ------------------------------

    private static SpriteBatch _spriteBatch;

    public static SpriteBatch SpriteBatch
    {
        set => _spriteBatch = value;
    }

    public static readonly Figure[] BasicBeginningLayout = new Figure[]
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
    };

    public static readonly Figure[] PawnPromLayout = new Figure[]
    {
        new Pawn(6, 3, Figure.ColorT.White),
        new Pawn(5,6, Figure.ColorT.Black)
    };
}