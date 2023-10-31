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
        BoardPos[] ret = new BoardPos[RookCorrectTiles];
        int arrPos = 0;
        
        if (IsBlocked) return (null, 0);

        for (int i = Pos.X - 1; i >= BoardPos.MinPos; --i)
        {
            if (IsEmpty(i, Pos.Y))
                ret[arrPos++] = new BoardPos(i, Pos.Y);
            else
            {
                if (IsEnemy(i, Pos.Y)) ret[arrPos++] = new BoardPos(i, Pos.Y, BoardPos.MoveType.AttackMove);
                break;
            }
        }

        for (int i = Pos.X + 1; i <= BoardPos.MaxPos; ++i)
        {
            if (IsEmpty(i, Pos.Y))
                ret[arrPos++] = new BoardPos(i, Pos.Y);
            else
            {
                if (IsEnemy(i, Pos.Y)) ret[arrPos++] = new BoardPos(i, Pos.Y, BoardPos.MoveType.AttackMove);
                break;
            }
        }

        for (int i = Pos.Y - 1; i >= BoardPos.MinPos; --i)
        {
            if (IsEmpty(Pos.X, i))
                ret[arrPos++] = new BoardPos(Pos.X, i);
            else
            {
                if (IsEnemy(Pos.X, i)) ret[arrPos++] = new BoardPos(Pos.X, i, BoardPos.MoveType.AttackMove);
                break;
            }
        }

        for (int i = Pos.Y + 1; i <= BoardPos.MaxPos; ++i)
        {
            if (IsEmpty(Pos.X, i))
                ret[arrPos++] = new BoardPos(Pos.X, i);
            else
            {
                if (IsEnemy(Pos.X, i)) ret[arrPos++] = new BoardPos(Pos.X, i, BoardPos.MoveType.AttackMove);
                break;
            }
        }

        return FilterAllowedTiles(ret, arrPos);
    }
    
    public override Figure Clone() => new Rook(Pos.X, Pos.Y, Color)
    {
        IsAlive = this.IsAlive,
        IsMoved = this.IsMoved
    };

// ------------------------------
// variables and properties
// ------------------------------

    internal const int RookCorrectTiles = 14;
}