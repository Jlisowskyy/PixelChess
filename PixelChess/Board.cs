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
    private Figure[,] _boardFigures;
    private Texture2D _boardTexture;
    public const int Width = 600;
    public const int Height = 600;

    public void Initialize()
    {
        
    }
}