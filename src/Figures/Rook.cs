using PixelChess.ChessBackend;
namespace PixelChess.Figures;

public class Rook : Figure
{
// --------------------------------
// type construction / setups
// --------------------------------
    public Rook(int x, int y, ColorT color) :
        base(x, y, color, TextInd[(int)color])
    {}

// --------------------------------
// abstract method overwrite
// --------------------------------
    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        if (!IsAlive) return (null, 0);
        
        return IsBlocked ? _getMovesWhenBlocked() : _getNormalSituationMoves();
    }

    public sealed override (BoardPos[] blockedTiles, int tileCount) GetBlocked()
    {
        BoardPos[] tiles = new BoardPos[MaxCorrectTiles];
        int tilesPos = 0;

        tilesPos = VertUpBlocks(this, tiles, tilesPos);
        tilesPos = VertDownBlocks(this, tiles, tilesPos);
        tilesPos = HorRightBlocks(this, tiles, tilesPos);
        tilesPos = HorLeftBlocks(this, tiles, tilesPos);

        return (tiles, tilesPos);
    }

    public override Figure Clone() => new Rook(Pos.X, Pos.Y, Color)
    {
        IsAlive = IsAlive,
        IsMoved = IsMoved
    };
    
    private (BoardPos[] moves, int movesCount) _getMovesWhenBlocked()
    {
        // Horizontal line
        if (Pos.Y == Parent.ColorMetadataMap[(int)Color].King.Pos.Y)
        {
            BoardPos[] ret = new BoardPos[BlockedMaxTiles];
            int arrPos = 0;

            arrPos = HorLeftMoves(this, ret, arrPos);
            arrPos = HorRightMoves(this, ret, arrPos);

            return (ret, arrPos);
        }
        
        // Vertical line
        if (Pos.X == Parent.ColorMetadataMap[(int)Color].King.Pos.X)
        {
            BoardPos[] ret = new BoardPos[BlockedMaxTiles];
            int arrPos = 0;

            arrPos = VertUpMoves(this, ret, arrPos);
            arrPos = VertDownMoves(this, ret, arrPos);
            
            return (ret, arrPos);
        }

        return (null, 0);
    }

    private (BoardPos[] moves, int movesCount) _getNormalSituationMoves()
    {
        BoardPos[] ret = new BoardPos[MaxCorrectTiles];
        int arrPos = 0;

        arrPos = HorLeftMoves(this, ret, arrPos);
        arrPos = HorRightMoves(this, ret, arrPos);
        arrPos = VertDownMoves(this, ret, arrPos);
        arrPos = VertUpMoves(this, ret, arrPos);

        return FilterAllowedTiles(ret, arrPos);
    }
    
// ----------------------------------------------------------------------
// public static function to calculate rook moves on desired figure
// ----------------------------------------------------------------------

    /*                      IMPORTANT
     *  All functions below returns updated arrPos as a result
     */
    
    public interface IStraightMove
    {
        public static virtual int Move { get; }
        public static virtual bool RangeCheck(int i) => true;
        public static virtual int InitIter(Figure fig) => 0;

        public static virtual (int, int) GetPos(Figure fig, int iter) => (0, 0);
    }

    public abstract class VerIncrease: IStraightMove
    {
        private const int _move = 1;
        public static int Move => _move;
        public static bool RangeCheck(int i) => i <= BoardPos.MaxPos;
        public static int InitIter(Figure fig) => fig.Pos.Y;
        public static (int, int) GetPos(Figure fig, int iter) => (fig.Pos.X, iter);
    }

    public abstract class VerDecrease : IStraightMove
    {
        private const int _move = -1;
        public static int Move => _move;
        public static bool RangeCheck(int i) => i >= BoardPos.MinPos;
        public static int InitIter(Figure fig) => fig.Pos.Y;
        public static (int, int) GetPos(Figure fig, int iter) => (fig.Pos.X, iter);
    }

    public abstract class HorDecrease : IStraightMove
    {
        private const int _move = -1;
        public static int Move => _move;
        public static bool RangeCheck(int i) => i >= BoardPos.MinPos;
        public static int InitIter(Figure fig) => fig.Pos.X;
        public static (int, int) GetPos(Figure fig, int iter) => (iter, fig.Pos.Y);
    }
    
    public abstract class HorIncrease : IStraightMove
    {
        private const int _move = 1;

        public static int Move => _move;
        public static bool RangeCheck(int i) => i <= BoardPos.MaxPos;
        public static int InitIter(Figure fig) => fig.Pos.X;
        public static (int, int) GetPos(Figure fig, int iter) => (iter, fig.Pos.Y);
    }

    public static int GenStraightLineActions<TMoveConds>(Figure fig, BoardPos[] mArr, int arrPos, bool isBlockChecking = false)
        where TMoveConds : IStraightMove
        // isBlockChecking flag - indicates whether method is generating moves or blocked tiles positions
    {
        int init = TMoveConds.InitIter(fig) + TMoveConds.Move;

        for (int i = init; TMoveConds.RangeCheck(i); i += TMoveConds.Move)
        {
            (int x, int y) = TMoveConds.GetPos(fig, i);
            
            if (IsEmpty(fig, x, y))
                mArr[arrPos++] = new BoardPos(x, y);
            else
            {
                if (isBlockChecking) mArr[arrPos++] = new BoardPos(x, y);
                else if (IsEnemy(fig, x, y)) mArr[arrPos++] = new BoardPos(x, y, BoardPos.MoveType.AttackMove);
                break;
            }
        }

        return arrPos;
    }

    public static int VertUpMoves(Figure fig, BoardPos[] mArr, int arrPos)
        => GenStraightLineActions<VerIncrease>(fig, mArr, arrPos);

    public static int VertDownMoves(Figure fig, BoardPos[] mArr, int arrPos)
        => GenStraightLineActions<VerDecrease>(fig, mArr, arrPos);

    public static int HorRightMoves(Figure fig, BoardPos[] mArr, int arrPos)
        => GenStraightLineActions<HorIncrease>(fig, mArr, arrPos);

    public static int HorLeftMoves(Figure fig, BoardPos[] mArr, int arrPos)
        => GenStraightLineActions<HorDecrease>(fig, mArr, arrPos);
    
    public static int VertUpBlocks(Figure fig, BoardPos[] mArr, int arrPos)
        => GenStraightLineActions<VerIncrease>(fig, mArr, arrPos, true);

    public static int VertDownBlocks(Figure fig, BoardPos[] mArr, int arrPos)
        => GenStraightLineActions<VerDecrease>(fig, mArr, arrPos, true);

    public static int HorRightBlocks(Figure fig, BoardPos[] mArr, int arrPos)
        => GenStraightLineActions<HorIncrease>(fig, mArr, arrPos, true);

    public static int HorLeftBlocks(Figure fig, BoardPos[] mArr, int arrPos)
        => GenStraightLineActions<HorDecrease>(fig, mArr, arrPos, true);
    
// ------------------------------
// variables and properties
// ------------------------------

    internal const int MaxCorrectTiles = 14;
    private const int BlockedMaxTiles = 6;

    private static readonly Board.ChessComponents[] TextInd =
        { Board.ChessComponents.WhiteRook, Board.ChessComponents.BlackRook };
}