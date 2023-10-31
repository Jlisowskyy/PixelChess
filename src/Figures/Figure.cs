using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PongGame.Figures;

namespace PongGame;
    
    /*  GENERAL TODO:
     * - make fen translator
     * - add top level class to interact better with monogame
     */

public abstract class Figure
    // All figures expects to have board attached, otherwise undefined
{
// --------------------------------
// type construction / setups
// --------------------------------

    protected Figure(int x, int y, ColorT color, Board.ChessComponents textureIndex)
    {
        Pos.X = x;
        Pos.Y = y;
        Color = color;
        TextureIndex = textureIndex;
    }
    
    public abstract (BoardPos[] moves, int movesCount) GetMoves();
    public abstract Figure Clone();
    
    
// ------------------------------
// helping protected fields
// ------------------------------

    protected bool IsEmpty(int x, int y) => Parent.BoardFigures[x, y] == null;

    protected bool IsEnemy(int x, int y) => Parent.BoardFigures[x, y].Color != this.Color;
    
// ------------------------------
// public types
// ------------------------------

    public enum ColorT
    {
        White,
        Black,
    }
    
// ------------------------------
// variables and properties
// ------------------------------

    public Board Parent;
    // used to access move filtering maps, moves history etc
    
    public bool IsAlive = true;
    // works also as flag whether the figure should be drew
    
    public bool IsMoved = false;
    // important only for pawns and castling

    public bool IsBlocked = false;
    
    public readonly Board.ChessComponents TextureIndex;
    // also used to identify figures color or type
    
    public BoardPos Pos;
    
    public ColorT Color
    {
        get;
    }
}