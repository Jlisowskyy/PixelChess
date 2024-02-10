using PixelChess.ChessBackend;

namespace PixelChess.Figures;

public abstract class Figure
    // All figures expects to have board attached, otherwise undefined
{
// --------------------------------
// type construction / setups
// --------------------------------

    protected Figure(int x, int y, ColorT color, Board.ChessComponents textureIndex)
    {
        Pos.X = x;
        Pos.Y = y;
        Color = color;
        TextureIndex = textureIndex;
    }
    
    public abstract (BoardPos[] moves, int movesCount) GetMoves();
    public abstract (BoardPos[] blockedTiles, int tileCount) GetBlockedTiles();
    public abstract Figure Clone();
    
    
// ------------------------------
// helping protected fields
// ------------------------------

    protected bool IsEmpty(int x, int y) => Parent.BoardFigures[x, y] == null;

    protected bool IsEnemy(int x, int y) => Parent.BoardFigures[x, y].Color != this.Color;
    
    protected static bool IsEmpty(Figure fig, int x, int y) => fig.Parent.BoardFigures[x, y] == null;

    protected static bool IsEnemy(Figure fig, int x, int y) => fig.Parent.BoardFigures[x, y].Color != fig.Color;

    protected (BoardPos[] arr, int arrLimit) FilterAllowedTiles(BoardPos[] arr, int arrLimit)
    {
        if (!Parent.IsChecked) return (arr, arrLimit);

        // in this situation the only legal move belongs to king
        if (Parent.KingAttackingFiguresCount > 1) return (null, 0);

        BoardPos[] arrRet = new BoardPos[arrLimit];
        int arrRetPos = 0;
        
        for (int i = 0; i < arrLimit; ++i)
        {
            if ((Parent.BlockedTiles[(int)Color][arr[i].X, arr[i].Y] & Board.TileState.AllowedTile) != 0)
                arrRet[arrRetPos++] = arr[i];
        }

        return (arrRet, arrRetPos);
    }
    
// ------------------------------
// public types
// ------------------------------

    public enum ColorT
    {
        White,
        Black,
    }
    
// ------------------------------
// variables and properties
// ------------------------------

    public Board Parent;
    // used to access move filtering maps, moves history etc
    
    public bool IsAlive = true;
    // works also as flag whether the figure should be drew
    
    public bool IsMoved = false;
    // important only for pawns and castling

    public bool IsBlocked = false;
    
    public readonly Board.ChessComponents TextureIndex;
    // also used to identify figures color or type
    
    public BoardPos Pos;
    
    public ColorT Color
    {
        get;
    }
}