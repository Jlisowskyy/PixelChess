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
        return IsBlocked ? _getMovesWhenBlocked() : _getNormalSituationMoves();
    }
    
    public override Figure Clone() => new Bishop(Pos.X, Pos.Y, Color)
    {
        IsAlive = this.IsAlive,
        IsMoved = this.IsMoved
    };

    private (BoardPos[] moves, int movesCount) _getMovesWhenBlocked()
    {
        int xDist = Parent.ColorMetadataMap[(int)Color].King.Pos.X - Pos.X;
        int yDist = Parent.ColorMetadataMap[(int)Color].King.Pos.Y - Pos.Y;

        if (Math.Abs(xDist) != Math.Abs(yDist)) return (null, 0);
        
        BoardPos[] ret = new BoardPos[MaxPossibleTilesWhenBlocked];
        int arrPos = 0;

        if (xDist < 0)
            ProcessDirections(yDist < 0 ? new[] { Dir.Ne, Dir.Sw } : new[] { Dir.Nw, Dir.Se }); 
        else
            ProcessDirections(yDist < 0 ? new[] { Dir.Nw, Dir.Se } : new[] { Dir.Ne, Dir.Sw });

        return (ret, arrPos);

        
        
        // Expression simplifier helper
        
        void ProcessDirections(Dir[] dirs)
        {
            arrPos = DirChosenMoves(this, ret, arrPos, dirs[0]);
            arrPos = DirChosenMoves(this, ret, arrPos, dirs[1]);
        }
    }

    private (BoardPos[] moves, int movesCount) _getNormalSituationMoves()
    {
        BoardPos[] ret = new BoardPos[MaxPossibleTiles];
        int arrPos = 0;

        // loops through all directions from [ sw nw ne se ]
        for (int i = 0; i < 4; ++i)
            arrPos = DirChosenMoves(this, ret, arrPos, (Dir)i);
        
        return FilterAllowedTiles(ret, arrPos);
    } 
    

// ------------------------------------------------------------------------
// public static function to calculate bishop moves on desired figure
// ------------------------------------------------------------------------

    /*                      IMPORTANT
     *  All functions below returns updated arrPos as a result
     */ 

    public static int DirChosenMoves(Figure fig, BoardPos[] mArr, int arrPos, Dir dir)
    {
        int nx = fig.Pos.X;
        int ny = fig.Pos.Y;
        for (int j = 0; j < MoveLimMap[fig.Pos.X, fig.Pos.Y][(int)dir]; ++j)
        {
            nx += XMoves[(int)dir];
            ny += YMoves[(int)dir];

            if (!IsEmpty(fig, nx, ny))
            {
                if (IsEnemy(fig, nx, ny))
                    mArr[arrPos++] = new BoardPos(nx, ny, BoardPos.MoveType.AttackMove);
                break;
            }

            mArr[arrPos++] = new BoardPos(nx, ny);
        }

        return arrPos;
    }

    
// ------------------------------
// variables and properties
// ------------------------------

    internal const int MaxPossibleTiles = 13;
    private const int MaxPossibleTilesWhenBlocked = 6;
    
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