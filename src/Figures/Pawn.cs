using System;
using System.Linq;
using PixelChess.ChessBackend;
namespace PixelChess.Figures;

public class Pawn : Figure
{
// --------------------------------
// type construction / setups
// --------------------------------

    public Pawn(int x, int y, ColorT color) :
        base(x, y, color, TextId[(int)color])
    {}
    
// --------------------------------
// abstract method overwrite
// --------------------------------

    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        BoardPos[] moves = new BoardPos[MaxMoves];
        int arrPos = 0;

        if (IsBlocked || !IsAlive) return (null, 0);
        
        if (IsMoved)
            // does not check whether pawn goes out of board, assumes will promoted by board before next call
        {
            if (IsEmpty(Pos.X, Pos.Y + MvCord[(int)Color]))
            {
                BoardPos.MoveType mt = _addPromTile(BoardPos.MoveType.NormalMove, Pos.Y + MvCord[(int)Color]);
                moves[arrPos++] = new BoardPos(Pos.X, Pos.Y + MvCord[(int)Color], mt);
            }

            var lMove = Parent.MovesHistory.Last!.Value;
            if (Pos.Y == ElPassantTiles[(int)Color] && _isLastMovedFigFreshPawn() &&
                int.Abs(lMove.OldY - lMove.MadeMove.Y) == 2 &&  int.Abs(lMove.MadeMove.X - Pos.X) == 1)
            {
                moves[arrPos++] = new BoardPos(lMove.MadeMove.X, Pos.Y + MvCord[(int)Color], BoardPos.MoveType.ElPass);
            }
        }
        else
        {
            for (int dist = 1; dist < 3; ++dist)
            {
                if (IsEmpty(Pos.X, Pos.Y + dist * MvCord[(int)Color]))
                    moves[arrPos++] = new BoardPos(Pos.X, Pos.Y + dist * MvCord[(int)Color]);
                else break;
            }
        }

        int ny = Pos.Y + MvCord[(int)Color];
        for (int i = 0; i < 2; ++i)
        {
            int nx = Pos.X + XAttackCords[i];
            
            if (nx > BoardPos.MaxPos || nx < BoardPos.MinPos) continue;
            
            if (!IsEmpty(nx, ny) && IsEnemy(nx, ny))
            {
                moves[arrPos++] = new BoardPos(nx, ny, _addPromTile(BoardPos.MoveType.AttackMove, ny));
            }
        }
        
        return FilterAllowedTiles(moves, arrPos);
    }

    public sealed override (BoardPos[] blockedTiles, int tileCount) GetBlocked()
    {
        BoardPos[] tiles = new BoardPos[MaxBlockedTiles];
        int tilesPos = 0;
        
        foreach (var xOffset in XAttackCords)
        {
            int attackX = Pos.X + xOffset;
            if (attackX >= BoardPos.MinPos && attackX <= BoardPos.MaxPos)
            {
                tiles[tilesPos++] = new BoardPos(attackX, Pos.Y + MvCord[(int)Color]);
            }
        }
        
        return (tiles, tilesPos);
    }

    public override Figure Clone() => new Pawn(Pos.X, Pos.Y, Color)
    {
        IsAlive = this.IsAlive,
        IsMoved = this.IsMoved
    };
    
// ------------------------------
// helping methods
// ------------------------------

    private BoardPos.MoveType _addPromTile(BoardPos.MoveType move, int y)
        => y == PromTiles[(int)Color] ? move | BoardPos.MoveType.PromotionMove : move;
    
    private bool _isLastMovedFigFreshPawn()
    {
        if (Parent.MovesHistory.Last!.Value.Fig.TextureIndex == EnemyPawn[(int)Color] &&
            Parent.MovesHistory.Last!.Value.WasUnmoved) return true;
        
        return false;
    }
    
// ------------------------------
// variables and properties
// ------------------------------

    private const int MaxMoves = 4;
    private const int MaxBlockedTiles = 2;

    private static readonly int[] XAttackCords = { -1, 1 };
    private static readonly int[] MvCord = { 1, -1 };
    private static readonly int[] PromTiles = { 7, 0 };
    private static readonly int[] ElPassantTiles = { 4, 3 };
    private static readonly Board.ChessComponents[] EnemyPawn =
        { Board.ChessComponents.BlackPawn, Board.ChessComponents.WhitePawn };
    private static readonly Board.ChessComponents[] TextId =
        { Board.ChessComponents.WhitePawn, Board.ChessComponents.BlackPawn };
}