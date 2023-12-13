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
        if (!IsAlive) return (null, 0);
        var mvs = IsBlocked ? _getBlockedMoves() : _getUnblockedMoves();
        return FilterAllowedTiles(mvs.Item1, mvs.Item2);
    }

    public sealed override (BoardPos[] blockedTiles, int tileCount) GetBlockedTiles()
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
        IsAlive = IsAlive,
        IsMoved = IsMoved
    };
    
    public override string ToString()
        => $"{{{(Color == ColorT.White ? 'P' : 'p')}:{Pos.ToStringPos()}}}";
    
// ------------------------------
// private methods
// ------------------------------

    private (BoardPos[], int) _getBlockedMoves()
    {
        int xDist = Parent.ColorMetadataMap[(int)Color].King.Pos.X - Pos.X;
        int yDist = Parent.ColorMetadataMap[(int)Color].King.Pos.Y - Pos.Y;
        // Pawn should be able to march forward if he is on the line of attack
        if (xDist == 0 && yDist * MvCord[(int)Color] < 0)
        {
            BoardPos[] moves = new BoardPos[MaxMarchMoves];
            return (moves, _getStraightMarchMoves(moves, 0));
        }
        // Pawn should be able to attack figure aiming on king
        int attackX = Pos.X - int.Sign(xDist);
        int attackY = Pos.Y + MvCord[(int)Color];
        if (!IsEmpty(attackX, attackY) &&
            (Parent.BoardFigures[attackX, attackY].TextureIndex == Parent.ColorMetadataMap[(int)Color].EnemyQueen ||
             Parent.BoardFigures[attackX, attackY].TextureIndex == Parent.ColorMetadataMap[(int)Color].EnemyBishop))
        {
            BoardPos[] ret = new[]
                { new BoardPos(attackX, attackY, _addPromTile(BoardPos.MoveType.AttackMove, attackY)) };
            return (ret, 1);
        }
        
        return (null, 0);
    }
    
    private (BoardPos[], int) _getUnblockedMoves()
    {
        BoardPos[] moves = new BoardPos[MaxMoves];
        int arrPos = 0;

        arrPos = _getStraightMarchMoves(moves, arrPos);
        arrPos = _getAttackMoves(moves, arrPos);

        return (moves, arrPos);
    }

    private int _getAttackMoves(BoardPos[] moves, int arrPos)
    {
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

        return arrPos;
    }

    private int _getStraightMarchMoves(BoardPos[] moves, int arrPos)
    {
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
                int.Abs(lMove.OldY - lMove.MadeMove.Y) == 2 &&  int.Abs(lMove.MadeMove.X - Pos.X) == 1 && 
                _isElPassantLegal(lMove.MadeMove.X))
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

        return arrPos;
    }
    
    private BoardPos.MoveType _addPromTile(BoardPos.MoveType move, int y)
        => y == PromTiles[(int)Color] ? move | BoardPos.MoveType.PromotionMove : move;
    
    private bool _isLastMovedFigFreshPawn()
    {
        if (Parent.MovesHistory.Last!.Value.Fig.TextureIndex == EnemyPawn[(int)Color] &&
            Parent.MovesHistory.Last!.Value.WasUnmoved) return true;
        
        return false;
    }

    // should be used only, when all el passant requirements are true and we are not sure if the move will uncover king
    private bool _isElPassantLegal(int elPassantPawnX)
    {
        if (Parent.ColorMetadataMap[(int)Color].King.Pos.Y == Pos.Y)
        {
            int xDiff = Parent.ColorMetadataMap[(int)Color].King.Pos.X - Pos.X;
            
            return xDiff > 0 ? _elPassantCheckLeft(elPassantPawnX) : _elPassantCheckRight(elPassantPawnX);
        }

        return true;
    }

    private bool _elPassantCheckLeft(int elPassantPawnX)
        => _elPassantCheck<Rook.HorDecrease>(elPassantPawnX);

    private bool _elPassantCheckRight(int elPassantPawnX)
        => _elPassantCheck<Rook.HorIncrease>(elPassantPawnX);
    private bool _elPassantCheck<TMoveConds>(int elPassantPawnX) where TMoveConds : Rook.IStraightMove
        // TMoveConds should only be left or right ones
    {
        int init = TMoveConds.InitIter(this) + TMoveConds.Move;
        if (init == elPassantPawnX) init += TMoveConds.Move;

        for (int x = init; TMoveConds.RangeCheck(x); x += TMoveConds.Move)
        {
            if (!IsEmpty(x, Pos.Y) && Parent.BoardFigures[x, Pos.Y].TextureIndex == EnemyRook[(int)Color])
                return false;
        }

        return true;
    }
    
// ------------------------------
// variables and properties
// ------------------------------

    private const int MaxMoves = 4;
    private const int MaxBlockedTiles = 2;
    private const int MaxMarchMoves = 2;

    private static readonly int[] XAttackCords = { -1, 1 };
    private static readonly int[] MvCord = { 1, -1 };
    private static readonly int[] PromTiles = { 7, 0 };
    private static readonly int[] ElPassantTiles = { 4, 3 };
    private static readonly Board.ChessComponents[] EnemyPawn =
        { Board.ChessComponents.BlackPawn, Board.ChessComponents.WhitePawn };
    private static readonly Board.ChessComponents[] TextId =
        { Board.ChessComponents.WhitePawn, Board.ChessComponents.BlackPawn };
    private static readonly Board.ChessComponents[] EnemyRook =
        { Board.ChessComponents.BlackRook, Board.ChessComponents.WhiteRook };
}