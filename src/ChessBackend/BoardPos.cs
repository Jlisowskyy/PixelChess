using System;

namespace PongGame.ChessBackend;

public struct BoardPos
{
    public bool Equals(BoardPos other)
    {
        return MoveT == other.MoveT && X == other.X && Y == other.Y;ZZ
    }

    public override bool Equals(object obj)
    {
        return obj is BoardPos other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)MoveT, X, Y);
    }

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
        CastlingMove = 4,
        ElPass = 8,
        
        PromAndAttack = AttackMove | PromotionMove,
    }

    public readonly MoveType MoveT;
    public const int MinPos = 0;
    public const int MaxPos = 7;
    public int X;
    public int Y;
}

public struct Move
{
    public Move(int ox, int oy, BoardPos madeMove, Figure fig, bool wasUnmoved = false, Figure killedFig = null)
    {
        OldX = ox;
        OldY = oy;
        WasUnmoved = wasUnmoved;
        MadeMove = madeMove;
        Fig = fig;
        KilledFig = killedFig;
    }

    public readonly int OldX;
    public readonly int OldY;
    public readonly bool WasUnmoved;
    public readonly BoardPos MadeMove;
    public readonly Figure Fig;
    public readonly Figure KilledFig;
}