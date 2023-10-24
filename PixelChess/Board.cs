using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PongGame;

public struct BoardPos
{
    public BoardPos(int x, int y, MoveType mov = MoveType.NormalMove)
    {
        this.X = x;
        this.Y = y;
        MoveT = mov;
    }

    public static bool operator ==(BoardPos a, BoardPos b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(BoardPos a, BoardPos b) => a.X != b.X || a.Y != b.Y;


    public static bool isOnBoard(int x, int y)  => x >= MinPos && y >= MinPos && x <= MaxPos && y <= MaxPos;

    public bool isOnBoard() => isOnBoard(X, Y);

    [Flags]
    public enum MoveType
    {
        NormalMove,
        AttackMove,
        PromotionMove,
        KingAttackMove,
        CastlingMove
    }

    public readonly MoveType MoveT;
    public const int MinPos = 0;
    public const int MaxPos = 7;
    public int X;
    public int Y;
}

public class Board
{
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

    public BoardPos.MoveType DropFigure(BoardPos pos)
    {
        if (!_isHold || !pos.isOnBoard()) return BoardPos.MoveType.NormalMove;
        
        _selectedFigure.IsAlive = true;
        _isHold = false;

        var moves = _selectedFigure.GetMoves();
        
        for (int i = 0; i < moves.movesCount; ++i)
            if (pos == moves.moves[i])
                return _processFigure(moves.moves[i]);

        return BoardPos.MoveType.NormalMove;
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
            case BoardPos.MoveType.KingAttackMove:
                break;
        }
        
        _moveFigure(move);
        _selectedFigure = null;
        return move.MoveT;
    }

    private void _moveFigure(BoardPos move)
    {
        _boardFigures[_selectedFigure.Pos.X, _selectedFigure.Pos.Y] = null;
        _boardFigures[move.X, move.Y] = _selectedFigure;
        _selectedFigure.Pos = move;
        _selectedFigure.IsMoved = true;
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
        
        
    }

    private void _killFigure(BoardPos move)
    {
        _boardFigures[move.X, move.Y].IsAlive = false;
    }

    public bool IsSelectedFigure()
    {
        return _selectedFigure != null;
    }

    public (BoardPos[] moves, int moveCont) GetSelFigMoves()
    {
        return _selectedFigure.GetMoves();
    }

    public Vector2 CenterFigurePosOnMouse(int x, int y) => new(x + _mouseCentX, y + _mouseCentY);

    public PixelChess.ChessComponents SelFigTextIndex => _selectedFigure.TextureIndex;

    public BoardPos SelFigPos => _selectedFigure.Pos;
    public Board(Figure[] figuresList)
    {
        _boardFigures = new Figure[BoardSize, BoardSize];
        _startFiguresLayout = new Figure[figuresList.Length];
        figuresList.CopyTo(_startFiguresLayout, 0);
        
        foreach (var fig in _startFiguresLayout)
            fig.Parent = this._boardFigures;
    }

    public void Initialize()
    {
        _figuresList = new Figure[_startFiguresLayout.Length];
        _startFiguresLayout.CopyTo(_figuresList, 0);

        foreach (var fig in _figuresList)
            _boardFigures[fig.Pos.X, fig.Pos.Y] = fig;
    }

    public static Vector2 Translate(BoardPos pos)
        => new Vector2(XTilesCordBeg + pos.X * FigureWidth, YTilesCordBeg - pos.Y * FigureHeight);

    public static BoardPos Translate(int x, int y)
        => new BoardPos((int)((x - XTilesCordBeg) / FigureWidth), (int)((YTilesCordBeg + 68 - y) / FigureHeight));
    
    public PixelChess.ChessComponents TextureIndex = PixelChess.ChessComponents.Board;
    private const int BoardSize = 8;
    
    private readonly Figure[,] _boardFigures;
    private Figure _selectedFigure;
    private Figure _promotionPawn;
    private readonly Figure[] _startFiguresLayout;
    private Figure[] _figuresList;
    public Figure[] FigureList => _figuresList;
    
    
    private bool _isHold = false;
    public bool IsHold => _isHold;

    public const int XTilesBeg = 51;
    public const int YTilesBeg = 0;
    public const int XTilesCordBeg = 51;
    public const int YTilesCordBeg = 477;
    public const int Width = 600;
    public const int Height = 600;
    public const int FigureHeight = 68;
    public const int FigureWidth = 68;
    private const int _mouseCentX = - FigureWidth / 2;
    private const int _mouseCentY = - FigureHeight / 2;
    
    public static readonly Figure[] BasicBeginingLayout = new Figure[]
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
        new Pawn(4, 4, Figure.ColorT.White)
    };
}