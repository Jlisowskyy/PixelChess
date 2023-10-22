using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PongGame;

public abstract class Figure
    // All figures expects to have array attached board attached, otherwise undefined
{
    protected Figure(int x, int y, ColorT color)
    {
        Pos.X = x;
        Pos.Y = y;
        Color = color;
    }
    public Figure[,] Parent
    {
        set => _parent = value;
    }
    
    public abstract Game1.chessComponents TextureIndex
    {
        get;
    }

    protected bool isEmpty(BoardPos pos)
    {
        return _parent[pos.X, pos.Y] == null;
    }

    protected bool isEnemy(BoardPos pos)
    {
        return _parent[pos.X, pos.Y].Color != this.Color;
    }
    
    protected bool isEmpty(int x, int y)
    {
        return _parent[x, y] == null;
    }

    protected bool isEnemy(int x, int y)
    {
        return _parent[x, y].Color != this.Color;
    }

    public abstract (BoardPos[] moves, int movesCount) GetMoves();

    public enum ColorT
    {
        White,
        Black,
    }
    
    protected Figure[,] _parent;
    public bool IsAlive = true;
    public BoardPos Pos;

    public ColorT Color
    {
        get;
    }
}

public abstract class Pawn : Figure
{
    private bool _isMoved = false;

    Pawn(int x, int y, ColorT color) :
        base(x, y, color) {}

    public sealed override Game1.chessComponents TextureIndex
    {
        get
        {
            switch (Color)
            {
                case ColorT.White:
                    return Game1.chessComponents.WhitePawn;
                case ColorT.Black:
                    return Game1.chessComponents.BlackPawn;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        switch (Color)
        {
            case ColorT.White:
                if (_isMoved || isEnemy(Pos.X, Pos.Y + 1))
                {
                    return (new BoardPos[] { new BoardPos(Pos.X, Pos.Y + 1) }, 1);
                }
                else
                {
                    return (new BoardPos[]
                    {
                        new BoardPos(Pos.X, Pos.Y + 1),
                        new BoardPos(Pos.X, Pos.Y + 2),
                    }, 2);
                }
            case ColorT.Black:
                if (_isMoved || isEnemy(Pos.X, Pos.Y - 1) || Pos.Y - 1 == BoardPos.MinPos)
                {
                    return (new BoardPos[] { new BoardPos(Pos.X, Pos.Y - 1) }, 1);
                }
                else
                {
                    return (new BoardPos[]
                    {
                        new BoardPos(Pos.X, Pos.Y - 1),
                        new BoardPos(Pos.X, Pos.Y - 2),
                    }, 2);
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

public abstract class Knight : Figure
{
    public Knight(int x, int y, ColorT color):
        base(x, y, color) {}
    
    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        BoardPos[] ret = new BoardPos[MaxPossibleTiles];
        int arrPos = 0;
        
        for (int i = 0; i < 4; ++i)
        {
            if (Pos.X + XPosTable[i] >= 0 && Pos.X + XPosTable[i] <= BoardPos.MaxPos && Pos.Y + YPosTable[i] >= 0 &&
                Pos.Y + YPosTable[i] <= BoardPos.MaxPos)
            {
                ret[arrPos++] = new BoardPos(Pos.X + XPosTable[i], Pos.Y + YPosTable[i]);
            }
            
            if (Pos.X + XPosTable[i] >= 0 && Pos.X + XPosTable[i] <= BoardPos.MaxPos && Pos.Y - YPosTable[i] >= 0 &&
                Pos.Y - YPosTable[i] <= BoardPos.MaxPos)
            {
                ret[arrPos++] = new BoardPos(Pos.X + XPosTable[i], Pos.Y - YPosTable[i]);
            }
        }

        return (ret, arrPos);
    }

    public sealed override Game1.chessComponents TextureIndex
    {
        get
        {
            switch (Color)
            {
                case ColorT.White:
                    return Game1.chessComponents.WhiteKnight;
                case ColorT.Black:
                    return Game1.chessComponents.BlackKnight;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private const int MaxPossibleTiles = 8;
    private static readonly int[] XPosTable = new[] { -2, -1, 1, 2 };
    private static readonly int[] YPosTable = new[] { 1, 2, 2, 1 };
}

public abstract class Bishop : Figure
{
    public Bishop(int x, int y, ColorT color) :
        base(x, y, color) {}

    public sealed override Game1.chessComponents TextureIndex
    {
        get
        {
            switch (Color)
            {
                case ColorT.White:
                    return Game1.chessComponents.WhiteBishop;
                case ColorT.Black:
                    return Game1.chessComponents.BlackBishop;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public static (BoardPos[] moves, int movesCount) GetBishopMoves(BoardPos pos)
    {
        BoardPos[] ret = new BoardPos[MaxPossibleTiles];
        int arrPos = 0;
        
        int offset = Math.Min(pos.X, pos.Y);

        int xTemp = pos.X - offset;
        int yTemp = pos.Y - offset;

        while (xTemp <= BoardPos.MaxPos && yTemp <= BoardPos.MaxPos)
        {
            if (xTemp != pos.X && yTemp != pos.Y)
            {
                ret[arrPos++] = new BoardPos(xTemp, yTemp);
            }

            ++xTemp;
            ++yTemp;
        }

        offset = Math.Min(pos.X, BoardPos.MaxPos - pos.Y);
        xTemp = pos.X - offset;
        yTemp = pos.Y - offset;
        
        while (xTemp <= BoardPos.MaxPos && yTemp >= BoardPos.MinPos)
        {
            if (xTemp != pos.X && yTemp != pos.Y)
            {
                ret[arrPos++] = new BoardPos(xTemp, yTemp);
            }

            ++xTemp;
            --yTemp;
        }

        return (ret, arrPos);
    }

    internal const int MaxPossibleTiles = 13;
    public sealed override (BoardPos[] moves, int movesCount) GetMoves() => GetBishopMoves(Pos);
}

public abstract class Rook : Figure
{
    internal const int RookCorrectTiles = 14;

    public static (BoardPos[] moves, int movesCount) GetRookMoves(BoardPos pos)
    {
        BoardPos[] ret = new BoardPos[RookCorrectTiles];
        int arrPos = 0;
        
        for (int i = BoardPos.MinPos; i <= BoardPos.MaxPos; ++i)
        {
            if (i != pos.X)
                ret[arrPos++] = new BoardPos(i, pos.Y);

            if (i != pos.Y)
                ret[arrPos++] = new BoardPos(pos.X, i);
        }
        return (ret, RookCorrectTiles);
    }

    public sealed override (BoardPos[] moves, int movesCount) GetMoves() => GetRookMoves(Pos);
}

public abstract class Queen : Figure
{
    private const int QueenMaxTiles = Rook.RookCorrectTiles + Bishop.MaxPossibleTiles;

    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        BoardPos[] ret = new BoardPos[QueenMaxTiles];
        
        var rookMoves = Rook.GetRookMoves(Pos);
        var bishopMoves = Bishop.GetBishopMoves(Pos);
        
        rookMoves.moves.CopyTo(ret, 0);
        bishopMoves.moves.CopyTo(ret, Rook.RookCorrectTiles);

        return (ret, Rook.RookCorrectTiles + bishopMoves.movesCount);
    }
}

public abstract class King : Figure
{
    private const int KingMaxTiles = 8;

    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        BoardPos[] ret = new BoardPos[KingMaxTiles];
        int arrPos = 0;

        for (int i = -1; i < 2; ++i)
        {
            for (int j = -1; j < 2; ++j)
            {
                if (i == 0 && j == 0) continue;

                if (Pos.X + i >= 0 && Pos.X + i <= BoardPos.MaxPos && Pos.Y + j >= 0 && Pos.Y + j <= BoardPos.MaxPos)
                {
                    ret[arrPos++] = new BoardPos(Pos.X + i, Pos.Y + j);
                }
            }
        }

        return (ret, arrPos);
    }
}