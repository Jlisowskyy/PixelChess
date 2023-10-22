using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PongGame;

public abstract class Figure
{
    public BoardPos Pos;
    public abstract Game1.chessComponents TextureIndex
    {
        get;
    }

    public abstract (BoardPos[] moves, int movesCount) GetMoves();
}

public abstract class Pawn : Figure
{
    protected bool _isMoved = false;
}

public abstract class Knight : Figure
{
    private const int MaxPossibleTiles = 8;
    private static readonly int[] XPosTable = new[] { -2, -1, 1, 2 };
    private static readonly int[] YPosTable = new[] { 1, 2, 2, 1 };
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
}

public abstract class Bishop : Figure
{
    internal const int MaxPossibleTiles = 13;

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

public class WhitePawn: Pawn
{
    WhitePawn(int x, int y)
    {
        Pos.X = x;
        Pos.Y = y;
    }
    public override Game1.chessComponents TextureIndex => Game1.chessComponents.WhitePawn;

    public override (BoardPos[] moves, int movesCount) GetMoves()
    {
        if (!_isMoved)
        {
            return (new[] { new BoardPos(Pos.X, Pos.Y + 1), new BoardPos(Pos.X, Pos.Y + 2) }, 2);
        }
        else
        {
            return (new[] { new BoardPos(Pos.X, Pos.Y + 1) }, 1);
        }
    }
}

public class BlackPawn: Pawn
{
    BlackPawn(int x, int y)
    {
        Pos.X = x;
        Pos.Y = y;
    }
    public override Game1.chessComponents TextureIndex => Game1.chessComponents.BlackPawn;

    public override (BoardPos[] moves, int movesCount) GetMoves()
    {
        if (!_isMoved)
        {
            return (new[] { new BoardPos(Pos.X, Pos.Y - 1), new BoardPos(Pos.X, Pos.Y - 2) }, 2);
        }
        else
        {
            return (new[] { new BoardPos(Pos.X, Pos.Y - 1) }, 1);
        }
    }
}

public class WhiteKnight : Knight
{
    public override Game1.chessComponents TextureIndex => Game1.chessComponents.WhiteKnight;

    WhiteKnight(int x, int y)
    {
        Pos.X = x;
        Pos.Y = y;
    }
    
}

public class BlackKnight : Knight
{
    public override Game1.chessComponents TextureIndex => Game1.chessComponents.BlackKnight;

    BlackKnight(int x, int y)
    {
        Pos.X = x;
        Pos.Y = y;
    }
}

public class WhiteBishop : Bishop
{
    public override Game1.chessComponents TextureIndex => Game1.chessComponents.WhiteBishop;

    WhiteBishop(int x, int y)
    {
        Pos.X = x;
        Pos.Y = y;
    }
}

public class BlackBishop : Bishop
{
    public override Game1.chessComponents TextureIndex => Game1.chessComponents.BlackBishop;

    BlackBishop(int x, int y)
    {
        Pos.X = x;
        Pos.Y = y;
    }
}

public class WhiteRook : Rook
{
    public override Game1.chessComponents TextureIndex => Game1.chessComponents.WhiteRook;

    WhiteRook(int x, int y)
    {
        Pos.X = x;
        Pos.Y = y;
    }
}

public class BlackRook : Rook
{
    public override Game1.chessComponents TextureIndex => Game1.chessComponents.BlackRook;

    BlackRook(int x, int y)
    {
        Pos.X = x;
        Pos.Y = y;
    }
}

public class WhiteQueen : Queen
{
    public override Game1.chessComponents TextureIndex => Game1.chessComponents.WhiteQueen;

    WhiteQueen(int x, int y)
    {
        Pos.X = x;
        Pos.Y = y;
    }
}

public class BlackQueen : Queen
{
    public override Game1.chessComponents TextureIndex => Game1.chessComponents.BlackQueen;

    BlackQueen(int x, int y)
    {
        Pos.X = x;
        Pos.Y = y;
    }
}

public class BlackKing : King
{
    public override Game1.chessComponents TextureIndex => Game1.chessComponents.BlackKing;

    BlackKing(int x, int y)
    {
        Pos.X = x;
        Pos.Y = y;
    }
}

public class WhiteKing : King
{
    public override Game1.chessComponents TextureIndex => Game1.chessComponents.WhiteKing;

    WhiteKing(int x, int y)
    {
        Pos.X = x;
        Pos.Y = y;
    }
}