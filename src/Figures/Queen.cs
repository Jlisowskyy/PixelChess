namespace PongGame.Figures;

public class Queen : Figure
{
// --------------------------------
// type construction / setups
// --------------------------------
    public Queen(int x, int y, ColorT color) :
        base(x, y, color, color == ColorT.White ? Board.ChessComponents.WhiteQueen : Board.ChessComponents.BlackQueen){}
    
// --------------------------------
// abstract method overwrite
// --------------------------------
    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        BoardPos[] ret = new BoardPos[QueenMaxTiles];
        
        if (IsBlocked) return (null, 0);
        
        // TODO: speedup???
        Figure rook = new Rook(Pos.X, Pos.Y, Color);
        Figure bishop = new Bishop(Pos.X, Pos.Y, Color);
        rook.Parent = Parent;
        bishop.Parent = Parent;
        
        var rookRet = rook.GetMoves();
        var bishopRet = bishop.GetMoves();
        
        rookRet.moves.CopyTo(ret, 0);
        bishopRet.moves.CopyTo(ret, rookRet.movesCount);

        return (ret, rookRet.movesCount + bishopRet.movesCount);
    }
    
    public override Figure Clone() => new Queen(Pos.X, Pos.Y, Color)
    {
        IsAlive = this.IsAlive,
        IsMoved = this.IsMoved
    };
    
    private const int QueenMaxTiles = Rook.RookCorrectTiles + Bishop.MaxPossibleTiles;
}