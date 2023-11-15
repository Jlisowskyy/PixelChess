using PongGame.ChessBackend;

namespace PongGame.Figures;

public class Knight : Figure
{
// --------------------------------
// type construction / setups
// --------------------------------
    public Knight(int x, int y, ColorT color):
        base(x, y, color, color == ColorT.White ? Board.ChessComponents.WhiteKnight : Board.ChessComponents.BlackKnight) {}

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

                movesTable[x, y] = ret[..arrPos];
            }
        }
    }
    
    public override Figure Clone() => new Knight(Pos.X, Pos.Y, Color)
    {
        IsAlive = this.IsAlive,
        IsMoved = this.IsMoved
    };

// --------------------------------
// abstract method overwrite
// --------------------------------

    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        BoardPos[] ret = new BoardPos[MaxPossibleTiles];
        int arrPos = 0;
        
        if (IsBlocked) return (null, 0);

        for (int i = 0; i < movesTable[Pos.X, Pos.Y].Length; ++i)
        {
            if (!IsEmpty(movesTable[Pos.X, Pos.Y][i].X, movesTable[Pos.X, Pos.Y][i].Y))
            {
                if (IsEnemy(movesTable[Pos.X, Pos.Y][i].X, movesTable[Pos.X, Pos.Y][i].Y))
                {
                    var dt = movesTable[Pos.X, Pos.Y][i]; 
                    ret[arrPos++] = new BoardPos(dt.X, dt.Y, BoardPos.MoveType.AttackMove);
                }
            }
            else ret[arrPos++] = movesTable[Pos.X, Pos.Y][i];
        }
        
        return FilterAllowedTiles(ret,arrPos);
    }
    
// ------------------------------
// variables and properties
// ------------------------------
    
    private const int MaxPossibleTiles = 8;
    private static readonly int[] XPosTable = new[] { -2, -1, 1, 2 };
    private static readonly int[] YPosTable = new[] { 1, 2, 2, 1 };
    private static BoardPos[,][] movesTable = new BoardPos[8,8][];
}