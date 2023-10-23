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

    [Flags]
    public enum MoveType
    {
        NormalMove,
        AttackMove,
        PromotionMove,
    }

    public readonly MoveType MoveT;
    public const int MinPos = 0;
    public const int MaxPos = 7;
    public int X;
    public int Y;
}

public class Board
{ 
    public Board(Figure[] figuresList)
    {
        _startFiguresLayout = new Figure[figuresList.Length];
        figuresList.CopyTo(_startFiguresLayout, 0);
        
        foreach (var fig in _startFiguresLayout)
        {
            fig.Parent = this._boardFigures;
        }
    }

    public void Initialize()
    {
        _figuresList = new Figure[_startFiguresLayout.Length];
        _startFiguresLayout.CopyTo(_figuresList, 0);
    }

    public static Vector2 Translate(BoardPos pos)
    {
        return new Vector2(XTilesCordBeg + pos.X * FigureWidth, YTilesCordBeg - pos.Y * FigureHeight);
    }

    public static BoardPos Translate(Vector2 pos)
    {
        return new BoardPos((int)(pos.X - XTilesCordBeg) / FigureWidth, (int)-(pos.Y - YTilesCordBeg) / FigureHeight);
    }
    
    public Game1.chessComponents TextureIndex = Game1.chessComponents.Board;
    private const int BoardSize = 8;
    
    private Figure[,] _boardFigures = new Figure[BoardSize, BoardSize];
    private readonly Figure[] _startFiguresLayout;
    private Figure[] _figuresList;
    public Figure[] FigureList => _figuresList;

    public const int XTilesBeg = 51;
    public const int YTilesBeg = 0;
    public const int XTilesCordBeg = 51;
    public const int YTilesCordBeg = 477;
    public const int Width = 600;
    public const int Height = 600;
    public const int FigureHeight = 68;
    public const int FigureWidth = 68;
    
    public static readonly Figure[] basicBeginingLayout = new Figure[]
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
}