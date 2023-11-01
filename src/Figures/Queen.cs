using System;

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
        return IsBlocked ? _getMovesWhenBlocked() : _getNormalMoves();
    }

    private (BoardPos[] moves, int movesCount) _getNormalMoves()
    {
        BoardPos[] ret = new BoardPos[MaxCorrectTiles];
        int arrPos = 0;
        
        arrPos = Rook.HorLeftMoves(this, ret, arrPos);
        arrPos = Rook.HorRightMoves(this, ret, arrPos);
        arrPos = Rook.VertDownMoves(this, ret, arrPos);
        arrPos = Rook.VertUpMoves(this, ret, arrPos);
        
        for (int i = 0; i < 4; ++i)
            arrPos = Bishop.DirChosenMoves(this, ret, arrPos, (Bishop.Dir)i);

        return FilterAllowedTiles(ret, arrPos);
    }

    private (BoardPos[] moves, int movesCount) _getMovesWhenBlocked()
    {
        // Horizontal line
        if (Pos.Y == Parent.ColorMetadataMap[(int)Color].King.Pos.Y)
        {
            BoardPos[] ret = new BoardPos[BlockedMaxTiles];
            int arrPos = 0;
            
            arrPos = Rook.HorLeftMoves(this, ret, arrPos);
            arrPos = Rook.HorRightMoves(this, ret, arrPos);

            return (ret, arrPos);
        }
        
        // Vertical line
        if (Pos.X == Parent.ColorMetadataMap[(int)Color].King.Pos.X)
        {
            BoardPos[] ret = new BoardPos[BlockedMaxTiles];
            int arrPos = 0;
            
            arrPos = Rook.VertUpMoves(this, ret, arrPos);
            arrPos = Rook.VertDownMoves(this, ret, arrPos);
            
            return (ret, arrPos);
        }
        
        // Diagonals check
        
        int xDist = Parent.ColorMetadataMap[(int)Color].King.Pos.X - Pos.X;
        int yDist = Parent.ColorMetadataMap[(int)Color].King.Pos.Y - Pos.Y;

        if (Math.Abs(xDist) != Math.Abs(yDist)) return (null, 0);
        
        BoardPos[] retVal = new BoardPos[BlockedMaxTiles];
        int arrayPos = 0;

        if (xDist < 0)
            ProcessDirections(yDist < 0 ? new[] { Bishop.Dir.Ne, Bishop.Dir.Sw } : new[] { Bishop.Dir.Nw, Bishop.Dir.Se }); 
        else
            ProcessDirections(yDist < 0 ? new[] { Bishop.Dir.Nw, Bishop.Dir.Se } : new[] { Bishop.Dir.Ne, Bishop.Dir.Sw });

        return (retVal, arrayPos);

        void ProcessDirections(Bishop.Dir[] dirs)
        {
            arrayPos = Bishop.DirChosenMoves(this, retVal, arrayPos, dirs[0]);
            arrayPos = Bishop.DirChosenMoves(this, retVal, arrayPos, dirs[1]);
        }
    }
    
    public override Figure Clone() => new Queen(Pos.X, Pos.Y, Color)
    {
        IsAlive = this.IsAlive,
        IsMoved = this.IsMoved
    };
    
    private const int MaxCorrectTiles = Rook.MaxCorrectTiles + Bishop.MaxPossibleTiles;
    private const int BlockedMaxTiles = 6;
}