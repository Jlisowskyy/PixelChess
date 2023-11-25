using PixelChess.ChessBackend;
namespace PixelChess.Figures;

public class King : Figure
{
// --------------------------------
// type construction / setups
// --------------------------------
    public King(int x, int y, ColorT color):
        base(x, y, color, color == ColorT.White? Board.ChessComponents.WhiteKing : Board.ChessComponents.BlackKing) {}

// --------------------------------
// abstract method overwrite
// --------------------------------
    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        BoardPos[] ret = new BoardPos[KingMaxTiles];
        int arrPos = 0;

        for (int i = -1; i < 2; ++i)
        {
            for (int j = -1; j < 2; ++j)
            {
                if (i == 0 && j == 0) continue;

                int tempX = Pos.X + i;
                int tempY = Pos.Y + j;
                if (BoardPos.isOnBoard(tempX, tempY) && Parent.BlockedTiles[(int)Color][tempX, tempY] == Board.TileState.UnblockedTile)
                {
                    if (IsEmpty(tempX, tempY))
                        ret[arrPos++] = new BoardPos(tempX, tempY);
                    else if (IsEnemy(tempX, tempY))
                        ret[arrPos++] = new BoardPos(tempX, tempY, BoardPos.MoveType.AttackMove);
                }
            }
        }
        
        if (!IsMoved)
            arrPos = GetCastlingMoves(ret, arrPos);

        return (ret, arrPos);
    }
    
    public override Figure Clone() => new King(Pos.X, Pos.Y, Color)
    {
        IsAlive = IsAlive,
        IsMoved = IsMoved
    };
    
// ------------------------------
// private help method
// ------------------------------
    
    private int GetCastlingMoves(BoardPos[] moves, int arrPos)
    {
        Board.ChessComponents rookType =
            Color == ColorT.White ? Board.ChessComponents.WhiteRook : Board.ChessComponents.BlackRook; 
        int i;

        if (!IsEmpty(BoardPos.MinPos, Pos.Y) 
            && Parent.BoardFigures[BoardPos.MinPos, Pos.Y].TextureIndex == rookType 
            && Parent.BoardFigures[BoardPos.MinPos, Pos.Y].IsMoved == false)
        {
            for (i = Pos.X + 1; i < BoardPos.MaxPos; ++i)
            {
                if (!IsEmpty(i, Pos.Y) || Parent.BlockedTiles[(int)Color][i, Pos.Y] == Board.TileState.BlockedTile)
                    break;

                // TODO: check for attacks
            }

            if (i == BoardPos.MaxPos)
                moves[arrPos++] = new BoardPos(ShortCastlingX, Pos.Y, BoardPos.MoveType.CastlingMove);
        }

        if (!IsEmpty(BoardPos.MaxPos, Pos.Y) 
            && Parent.BoardFigures[BoardPos.MaxPos, Pos.Y].TextureIndex == rookType 
            && Parent.BoardFigures[BoardPos.MaxPos, Pos.Y].IsMoved == false)
        {
            for (i = Pos.X - 1; i > BoardPos.MinPos; --i)
            {
                if (!IsEmpty(i, Pos.Y) || Parent.BlockedTiles[(int)Color][i, Pos.Y] == Board.TileState.BlockedTile)
                    break;
            }

            if (i == BoardPos.MinPos)
                moves[arrPos++] = new BoardPos(LongCastlingX, Pos.Y, BoardPos.MoveType.CastlingMove);
        }

        return arrPos;
    }
    
// ------------------------------
// variables and properties
// ------------------------------

    public const int ShortCastlingX= 6;
    public const int LongCastlingX = 2;
    public const int ShortCastlingRookX = 5;
    public const int LongCastlingRookX= 3;
    private const int KingMaxTiles = 8 + 2;
}