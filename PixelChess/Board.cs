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
        _figuresList = new Figure[figuresList.Length];
        figuresList.CopyTo(_figuresList, 0);
    }

    public void Initialize()
    {
        
    }
    
    public static Game1.chessComponents TextureIndex => Game1.chessComponents.Board;
    private static int BoardSize = 8;
    
    private Figure[,] _boardFigures = new Figure[BoardSize, BoardSize];
    private Figure[] _figuresList;
    
    public const int Width = 600;
    public const int Height = 600;
    
    public static readonly Figure[] basicBeginingLayout = new Figure[]
    {
        new WhitePawn(1,0),
        new WhitePawn(1,1),
        new WhitePawn(1,2),
        new WhitePawn(1,3),
        new WhitePawn(1,4),
        new WhitePawn(1,5),
        new WhitePawn(1,6),
        new WhitePawn(1,7),
        new WhiteRook(0, 0),
        new WhiteKnight(0, 1),
        new WhiteBishop(0,2),
        new WhiteQueen(0, 3),
        new WhiteKing(0,4),
        new WhiteBishop(0,5),
        new WhiteKnight(0, 6),
        new WhiteRook(0, 7),
        new BlackPawn(6,0),
        new BlackPawn(6,1),
        new BlackPawn(6,2),
        new BlackPawn(6,3),
        new BlackPawn(6,4),
        new BlackPawn(6,5),
        new BlackPawn(6,6),
        new BlackPawn(6,7),
        new BlackRook(7, 0),
        new BlackKnight(7, 1),
        new BlackBishop(7,2),
        new BlackKing(7,3),
        new BlackQueen(7, 4),
        new BlackBishop(7,5),
        new BlackKnight(7, 6),
        new BlackRook(7, 7),
    };
}