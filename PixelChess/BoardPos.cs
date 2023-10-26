using System;

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
        NormalMove = 0,
        AttackMove = 1,
        PromotionMove = 2,
        KingAttackMove = 4,
        CastlingMove = 8,
        ElPass = 16,
    }

    public readonly MoveType MoveT;
    public const int MinPos = 0;
    public const int MaxPos = 7;
    public int X;
    public int Y;
}


public struct Move
{
    public Move(int ox, int oy, BoardPos newPos, Board.ChessComponents figT)
    {
        OldX = ox;
        OldY = oy;
        NewPos = newPos;
        FigT = figT;
    }

    public readonly int OldX;
    public readonly int OldY;
    public readonly BoardPos NewPos;
    public readonly Board.ChessComponents FigT;
}