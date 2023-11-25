using PixelChess.ChessBackend;
namespace PixelChess.Figures;

public class Rook : Figure
{
// --------------------------------
// type construction / setups
// --------------------------------
    public Rook(int x, int y, ColorT color) :
        base(x, y, color, color == ColorT.White ? Board.ChessComponents.WhiteRook : Board.ChessComponents.BlackRook)
    {}

// --------------------------------
// abstract method overwrite
// --------------------------------
    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        if (!IsAlive) return (null, 0);
        
        return IsBlocked ? _getMovesWhenBlocked() : _getNormalSituationMoves();
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

    public static int GenStraightLineMoves<TMoveConds>(Figure fig, BoardPos[] mArr, int arrPos)
        where TMoveConds : IStraightMove
    {
        int init = TMoveConds.InitIter(fig) + TMoveConds.Move;

        for (int i = init; TMoveConds.RangeCheck(i); i += TMoveConds.Move)
        {
            (int x, int y) = TMoveConds.GetPos(fig, i);
            
            if (IsEmpty(fig, x, y))
                mArr[arrPos++] = new BoardPos(x, y);
            else
            {
                if (IsEnemy(fig, x, y)) mArr[arrPos++] = new BoardPos(x, y, BoardPos.MoveType.AttackMove);
                break;
            }
        }

        return arrPos;
    }

    public static int VertUpMoves(Figure fig, BoardPos[] mArr, int arrPos)
        => GenStraightLineMoves<VerIncrease>(fig, mArr, arrPos);

    public static int VertDownMoves(Figure fig, BoardPos[] mArr, int arrPos)
        => GenStraightLineMoves<VerDecrease>(fig, mArr, arrPos);

    public static int HorRightMoves(Figure fig, BoardPos[] mArr, int arrPos)
        => GenStraightLineMoves<HorIncrease>(fig, mArr, arrPos);

    public static int HorLeftMoves(Figure fig, BoardPos[] mArr, int arrPos)
        => GenStraightLineMoves<HorDecrease>(fig, mArr, arrPos);
    
// ------------------------------
// variables and properties
// ------------------------------

    internal const int MaxCorrectTiles = 14;
    private const int BlockedMaxTiles = 6;
}