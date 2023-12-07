using PixelChess.ChessBackend;
namespace PixelChess.Figures;

public class Knight : Figure
{
// --------------------------------
// type construction / setups
// --------------------------------
    public Knight(int x, int y, ColorT color):
        base(x, y, color, TextInd[(int)color]) {}

    static Knight()
        // precalculates moves for all fields
    {
        for (int x = 0; x < Board.BoardSize; ++x)
        {
            for (int y = 0; y < Board.BoardSize; ++y) 
            {
                BoardPos[] ret = new BoardPos[MaxPossibleTiles];
                int arrPos = 0;
                
                for (int k = 0; k < 4; ++k)
                {
                    int tempX = x + XPosTable[k];
                    int tempY = y + YPosTable[k];

                    if (BoardPos.isOnBoard(tempX, tempY))
                        ret[arrPos++] = new BoardPos(tempX, tempY);

                    tempX = x + XPosTable[k];
                    tempY = y - YPosTable[k];
            
                    if (BoardPos.isOnBoard(tempX, tempY))
                        ret[arrPos++] = new BoardPos(tempX, tempY);
                }

                MovesTable[x, y] = ret[..arrPos];
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
        
        if (IsBlocked || !IsAlive) return (null, 0);

        for (int i = 0; i < MovesTable[Pos.X, Pos.Y].Length; ++i)
        {
            if (!IsEmpty(MovesTable[Pos.X, Pos.Y][i].X, MovesTable[Pos.X, Pos.Y][i].Y))
            {
                if (IsEnemy(MovesTable[Pos.X, Pos.Y][i].X, MovesTable[Pos.X, Pos.Y][i].Y))
                {
                    var dt = MovesTable[Pos.X, Pos.Y][i]; 
                    ret[arrPos++] = new BoardPos(dt.X, dt.Y, BoardPos.MoveType.AttackMove);
                }
            }
            else ret[arrPos++] = MovesTable[Pos.X, Pos.Y][i];
        }
        
        return FilterAllowedTiles(ret, arrPos);
    }

    public sealed override (BoardPos[] blockedTiles, int tileCount) GetBlocked()
        => (MovesTable[Pos.X, Pos.Y], MovesTable[Pos.X, Pos.Y].Length);

    public override Figure Clone() => new Knight(Pos.X, Pos.Y, Color)
    {
        IsAlive = IsAlive,
        IsMoved = IsMoved
    };
    
// ------------------------------
// variables and properties
// ------------------------------
    
    private const int MaxPossibleTiles = 8;
    private static readonly int[] XPosTable = { -2, -1, 1, 2 };
    private static readonly int[] YPosTable = { 1, 2, 2, 1 };
    private static readonly BoardPos[,][] MovesTable = new BoardPos[8,8][];
    private static readonly Board.ChessComponents[] TextInd =
        { Board.ChessComponents.WhiteKnight, Board.ChessComponents.BlackKnight };
}