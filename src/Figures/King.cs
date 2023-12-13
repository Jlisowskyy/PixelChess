using PixelChess.ChessBackend;
namespace PixelChess.Figures;

public class King : Figure
{
// --------------------------------
// type construction / setups
// --------------------------------
    public King(int x, int y, ColorT color):
        base(x, y, color, TextInd[(int)color]) {}

    static King()
    {
        for (int x = BoardPos.MinPos; x <= BoardPos.MaxPos; ++x)
            for (int y = BoardPos.MinPos; y <= BoardPos.MaxPos; ++y)
            {
                var arr = new BoardPos[MaxKingMoves];
                int arrPos = 0;
                
                for (int xOff = -1; xOff < 2; ++xOff)
                    for (int yOff = -1; yOff < 2; ++yOff)
                    {
                        if (xOff == 0 && yOff == 0) continue;
                        
                        int tempX = x + xOff;
                        int tempY = y + yOff;

                        if (BoardPos.isOnBoard(tempX, tempY))
                        {
                            arr[arrPos++] = new BoardPos(tempX, tempY);
                        }
                    }

                KingMoves[x, y] = arr[..arrPos];
            }
        
    }

// --------------------------------
// abstract method overwrite
// --------------------------------
    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        BoardPos[] ret = new BoardPos[MaxKingMoves];
        int arrPos = 0;
        
        foreach (var move  in KingMoves[Pos.X, Pos.Y])
        {
            if ((Parent.BlockedTiles[(int)Color][move.X, move.Y] & Board.TileState.BlockedTile) == 0)
            {
                if (IsEmpty(move.X, move.Y))
                    ret[arrPos++] = new BoardPos(move.X, move.Y);
                else if (IsEnemy(move.X, move.Y))
                    ret[arrPos++] = new BoardPos(move.X, move.Y, BoardPos.MoveType.AttackMove);
            }
        }
        
        if (!IsMoved)
            arrPos = GetCastlingMoves(ret, arrPos);

        return (ret, arrPos);
    }

    public sealed override (BoardPos[] blockedTiles, int tileCount) GetBlockedTiles()
        => (KingMoves[Pos.X, Pos.Y], KingMoves[Pos.X, Pos.Y].Length);

    public override Figure Clone() => new King(Pos.X, Pos.Y, Color)
    {
        IsAlive = IsAlive,
        IsMoved = IsMoved
    };
    
    public override string ToString()
        => $"{{{(Color == ColorT.White ? 'K' : 'k')}:{Pos.ToStringPos()}}}";
    
// ------------------------------
// private help method
// ------------------------------
    
    private int GetCastlingMoves(BoardPos[] moves, int arrPos)
    {
        if (Parent.IsChecked) return arrPos;

        Board.ChessComponents rookType = FriendlyRooks[(int)Color];
        int i;
        
        if (!IsEmpty(BoardPos.MinPos, Pos.Y) 
            && Parent.BoardFigures[BoardPos.MinPos, Pos.Y].TextureIndex == rookType 
            && Parent.BoardFigures[BoardPos.MinPos, Pos.Y].IsMoved == false)
        {
            for (i = Pos.X - 1; i > 1; --i)
            {
                if (!IsEmpty(i, Pos.Y) || Parent.BlockedTiles[(int)Color][i, Pos.Y] == Board.TileState.BlockedTile)
                    break;
            }

            if (i == 1 && IsEmpty(1, Pos.Y))
                moves[arrPos++] = new BoardPos(LongCastlingX, Pos.Y, BoardPos.MoveType.CastlingMove);
        }

        if (!IsEmpty(BoardPos.MaxPos, Pos.Y) 
            && Parent.BoardFigures[BoardPos.MaxPos, Pos.Y].TextureIndex == rookType 
            && Parent.BoardFigures[BoardPos.MaxPos, Pos.Y].IsMoved == false)
        {
            for (i = Pos.X + 1; i < BoardPos.MaxPos; ++i)
            {
                if (!IsEmpty(i, Pos.Y) || Parent.BlockedTiles[(int)Color][i, Pos.Y] == Board.TileState.BlockedTile)
                    break;
            }

            if (i == BoardPos.MaxPos)
                moves[arrPos++] = new BoardPos(ShortCastlingX, Pos.Y, BoardPos.MoveType.CastlingMove);
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
    public const int StartingXPos = 4;
    private const int MaxKingMoves = 8;
    private static readonly Board.ChessComponents[] TextInd =
        { Board.ChessComponents.WhiteKing, Board.ChessComponents.BlackKing };
    private static readonly Board.ChessComponents[] FriendlyRooks =
        { Board.ChessComponents.WhiteRook, Board.ChessComponents.BlackRook };
    private static readonly BoardPos[,][] KingMoves = new BoardPos[8, 8][];
}