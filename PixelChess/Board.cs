using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PongGame;

public struct BoardPos
{
    public BoardPos(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }
    
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
        new WhitePawn(0,1),
        new WhitePawn(1,1),
        new WhitePawn(2,1),
        new WhitePawn(3,1),
        new WhitePawn(4,1),
        new WhitePawn(5,1),
        new WhitePawn(6,1),
        new WhitePawn(7,1),
        new WhiteRook(0, 0),
        new WhiteKnight(1, 0),
        new WhiteBishop(2,0),
        new WhiteQueen(3, 0),
        new WhiteKing(4,0),
        new WhiteBishop(5,0),
        new WhiteKnight(6, 0),
        new WhiteRook(7, 0),
        new BlackPawn(0,6),
        new BlackPawn(1,6),
        new BlackPawn(2,6),
        new BlackPawn(3,6),
        new BlackPawn(4,6),
        new BlackPawn(5,6),
        new BlackPawn(6,6),
        new BlackPawn(7,6),
        new BlackRook(0, 7),
        new BlackKnight(1, 7),
        new BlackBishop(2,7),
        new BlackQueen(3, 7),
        new BlackKing(4,7),
        new BlackBishop(5,7),
        new BlackKnight(6, 7),
        new BlackRook(7, 7),
    };
}