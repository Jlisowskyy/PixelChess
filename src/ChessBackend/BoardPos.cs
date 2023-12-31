using System;
using PixelChess.Figures;

namespace PixelChess.ChessBackend;

public struct BoardPos
    // structure used to pass information about position on board, made moves and actions taken
{
    public override bool Equals(object obj)
        => obj is BoardPos other && other == this;

    public override int GetHashCode()
        => HashCode.Combine((int)MoveT, X, Y);

    public static string NumToStringPos(int x, int y)
        => $"{(char)('A'+x)}{1+y}";

    public string ToStringPos() => NumToStringPos(X, Y);
    public override string ToString()
        => $"{{{MoveT} : [{NumToStringPos(X, Y)}]}}";

    public BoardPos(int x, int y, MoveType mov = MoveType.NormalMove)
    {
        X = x;
        Y = y;
        MoveT = mov;
    }

    public static bool operator ==(BoardPos a, BoardPos b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(BoardPos a, BoardPos b) => !(a == b);
    
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

public struct HistoricalMove
    // Class only used to represent state change of the state of the board during game, in other words,
    // contains information about made moves.
{
    public HistoricalMove(int ox, int oy, BoardPos madeMove, Figure fig, int halfMoves, 
        bool wasUnmoved = false, Figure killedFig = null)
    {
        OldX = ox;
        OldY = oy;
        WasUnmoved = wasUnmoved;
        MadeMove = madeMove;
        Fig = fig;
        HalfMoves = halfMoves;
        KilledFig = killedFig;
    }

    public readonly int OldX;
    public readonly int OldY;
    public char figureUpdateChar;
    public readonly bool WasUnmoved;
    public readonly BoardPos MadeMove;
    public readonly Figure Fig;
    public readonly Figure KilledFig;
    public readonly int HalfMoves;
}