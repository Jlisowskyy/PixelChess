using System;
using PixelChess.ChessBackend;

namespace PixelChess.Figures;

public class Queen : Figure
{
// --------------------------------
// type construction / setups
// --------------------------------
    public Queen(int x, int y, ColorT color) :
        base(x, y, color, TextInd[(int)color]){}
    
// --------------------------------
// abstract method overwrite
// --------------------------------
    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        if (!IsAlive) return (null, 0);
        
        return IsBlocked ? _getMovesWhenBlocked() : _getNormalMoves();
    }

    public sealed override (BoardPos[] blockedTiles, int tileCount) GetBlocked()
    {
        BoardPos[] tiles = new BoardPos[MaxCorrectTiles];
        int tilesPos = 0;
        
        tilesPos = Rook.HorLeftBlocks(this, tiles, tilesPos);
        tilesPos = Rook.HorRightBlocks(this, tiles, tilesPos);
        tilesPos = Rook.VertDownBlocks(this, tiles, tilesPos);
        tilesPos = Rook.VertUpBlocks(this, tiles, tilesPos);
        
        for (int i = 0; i < 4; ++i)
            tilesPos = Bishop.DirChosenMoves(this, tiles, tilesPos, (Bishop.Dir)i, true);

        return (tiles, tilesPos);
    }
    
    public override Figure Clone() => new Queen(Pos.X, Pos.Y, Color)
    {
        IsAlive = IsAlive,
        IsMoved = IsMoved
    };
    
// ------------------------------
// private methods
// ------------------------------

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
    
// ------------------------------
// variables and properties
// ------------------------------
    
    private const int MaxCorrectTiles = Rook.MaxCorrectTiles + Bishop.MaxPossibleTiles;
    private const int BlockedMaxTiles = 6;

    private static readonly Board.ChessComponents[] TextInd =
        { Board.ChessComponents.WhiteQueen, Board.ChessComponents.BlackQueen };
}