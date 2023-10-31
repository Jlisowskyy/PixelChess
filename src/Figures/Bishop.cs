using System;

namespace PongGame.Figures;

public class Bishop : Figure
{
// --------------------------------
// type construction / setups
// --------------------------------
    public Bishop(int x, int y, ColorT color) :
        base(x, y, color, color == ColorT.White ? Board.ChessComponents.WhiteBishop : Board.ChessComponents.BlackBishop) {}

    static Bishop()
    {
        MoveLimMap = new int[8, 8][];
        for (int x = 0; x < 8; ++x)
        {
            for (int y = 0; y < 8; ++y)
            {
                // in order [ sw nw ne se ]
                MoveLimMap[x, y] = new int[4];

                MoveLimMap[x, y][0] = Math.Min(x, y);
                MoveLimMap[x, y][1] = Math.Min(x, Math.Abs(BoardPos.MaxPos - y));
                MoveLimMap[x, y][2] = Math.Min(Math.Abs(BoardPos.MaxPos - x), Math.Abs(BoardPos.MaxPos - y));
                MoveLimMap[x, y][3] = Math.Min(Math.Abs(BoardPos.MaxPos - x), y);
            }
        }
    }
    
// --------------------------------
// abstract method overwrite
// --------------------------------
    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    
    {
        BoardPos[] ret = new BoardPos[MaxPossibleTiles];
        int arrPos = 0;
        
        if (IsBlocked) return (null, 0);

        // loops through all directions from [ sw nw ne se ]
        for (int i = 0; i < 4; ++i)
        {
            int nx = Pos.X;
            int ny = Pos.Y;
            for (int j = 0; j < MoveLimMap[Pos.X, Pos.Y][i]; ++j)
            {
                nx += XMoves[i];
                ny += YMoves[i];

                if (!IsEmpty(nx, ny))
                {
                    if (IsEnemy(nx, ny))
                        ret[arrPos++] = new BoardPos(nx, ny, BoardPos.MoveType.AttackMove);
                    break;
                }

                ret[arrPos++] = new BoardPos(nx, ny);
            }
        }
        
        return FilterAllowedTiles(ret, arrPos);
    }
    
    public override Figure Clone() => new Bishop(Pos.X, Pos.Y, Color)
    {
        IsAlive = this.IsAlive,
        IsMoved = this.IsMoved
    };
    
// ------------------------------
// variables and properties
// ------------------------------

    internal const int MaxPossibleTiles = 13;
    
    public enum Dir
    {
        Sw,
        Nw,
        Ne,
        Se
    }
    
    // Contains limits for each diagonal moves on all specific fields
    // in order [ sw nw ne se ]
    public static readonly int[,][] MoveLimMap;

    // in order [ sw nw ne se ]
    public static readonly int[] XMoves = { -1, -1, 1, 1 };
    public static readonly int[] YMoves = { -1, 1, 1, -1 };
}