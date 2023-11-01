namespace PongGame.Figures;

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
        return IsBlocked ? _getMovesWhenBlocked() : _getNormalSituationMoves();
    }
    
    public override Figure Clone() => new Rook(Pos.X, Pos.Y, Color)
    {
        IsAlive = this.IsAlive,
        IsMoved = this.IsMoved
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

    public static int VertUpMoves(Figure fig, BoardPos[] mArr, int arrPos)
    {
        for (int i = fig.Pos.Y + 1; i <= BoardPos.MaxPos; ++i)
        {
            if (IsEmpty(fig, fig.Pos.X, i))
                mArr[arrPos++] = new BoardPos(fig.Pos.X, i);
            else
            {
                if (IsEnemy(fig, fig.Pos.X, i)) mArr[arrPos++] = new BoardPos(fig.Pos.X, i, BoardPos.MoveType.AttackMove);
                break;
            }
        }

        return arrPos;
    }

    public static int VertDownMoves(Figure fig, BoardPos[] mArr, int arrPos)
    {
        for (int i = fig.Pos.Y - 1; i >= BoardPos.MinPos; --i)
        {
            if (IsEmpty(fig, fig.Pos.X, i))
                mArr[arrPos++] = new BoardPos(fig.Pos.X, i);
            else
            {
                if (IsEnemy(fig, fig.Pos.X, i)) mArr[arrPos++] = new BoardPos(fig.Pos.X, i, BoardPos.MoveType.AttackMove);
                break;
            }
        }

        return arrPos;
    }

    public static int HorRightMoves(Figure fig, BoardPos[] mArr, int arrPos)
    {
        for (int i = fig.Pos.X + 1; i <= BoardPos.MaxPos; ++i)
        {
            if (IsEmpty(fig,i, fig.Pos.Y))
                mArr[arrPos++] = new BoardPos(i, fig.Pos.Y);
            else
            {
                if (IsEnemy(fig, i, fig.Pos.Y)) mArr[arrPos++] = new BoardPos(i, fig.Pos.Y, BoardPos.MoveType.AttackMove);
                break;
            }
        }

        return arrPos;
    }

    public static int HorLeftMoves(Figure fig, BoardPos[] mArr, int arrPos)
    {
        for (int i = fig.Pos.X - 1; i >= BoardPos.MinPos; --i)
        {
            if (IsEmpty(fig, i, fig.Pos.Y))
                mArr[arrPos++] = new BoardPos(i, fig.Pos.Y);
            else
            {
                if (IsEnemy(fig, i, fig.Pos.Y)) mArr[arrPos++] = new BoardPos(i, fig.Pos.Y, BoardPos.MoveType.AttackMove);
                break;
            }
        }

        return arrPos;
    }
    
// ------------------------------
// variables and properties
// ------------------------------

    internal const int MaxCorrectTiles = 14;
    private const int BlockedMaxTiles = 6;
}