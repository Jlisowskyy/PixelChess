using System;

namespace PongGame.Figures;

public class Pawn : Figure
{
// --------------------------------
// type construction / setups
// --------------------------------
    public Pawn(int x, int y, ColorT color) :
        base(x, y, color, color == ColorT.White ? Board.ChessComponents.WhitePawn : Board.ChessComponents.BlackPawn)
    {
        // consider creating array of those values? TODO
        if (color == ColorT.White)
        {
            _mvCord = 1;
            _promTile = BoardPos.MaxPos;
            _enemyPawnId = Board.ChessComponents.BlackPawn;
        }
        else
        {
            _mvCord = -1;
            _promTile = BoardPos.MinPos;
            _enemyPawnId = Board.ChessComponents.WhitePawn;
        }

        _elPassantX = _promTile - 3 * _mvCord;
    }
    
// --------------------------------
// abstract method overwrite
// --------------------------------

    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        BoardPos[] moves = new BoardPos[MaxMoves];
        int arrPos = 0;

        if (IsBlocked) return (null, 0);
        
        if (IsMoved)
            // does not check whether pawn goes out of board, assumes will promoted by board before next call
        {
            if (IsEmpty(Pos.X, Pos.Y + _mvCord))
            {
                BoardPos.MoveType mt = _addPromTile(BoardPos.MoveType.NormalMove, Pos.Y + _mvCord);
                moves[arrPos++] = new BoardPos(Pos.X, Pos.Y + _mvCord, mt);
            }

            if (Pos.Y == _elPassantX)
            {
                for (int i = 0; i < 2; ++i)
                {
                    int nx = Pos.X + XAttackCords[i];
                    if (nx >= BoardPos.MinPos && !IsEmpty(nx, Pos.Y) && _isElPassPossible(nx, Pos.Y))
                    {
                        moves[arrPos++] = new BoardPos(nx, Pos.Y + _mvCord, BoardPos.MoveType.ElPass);
                    }
                }
            }
        }
        else
        {
            for (int dist = 1; dist < 3; ++dist)
            {
                if (IsEmpty(Pos.X, Pos.Y + dist * _mvCord))
                    moves[arrPos++] = new BoardPos(Pos.X, Pos.Y + dist * _mvCord);
                else break;
            }
        }

        int ny = Pos.Y + _mvCord;
        for (int i = 0; i < 2; ++i)
        {
            int nx = Pos.X + XAttackCords[i];
            
            // TODO: checks speed changes after this removal
            if (nx > BoardPos.MaxPos || nx < BoardPos.MinPos) continue;
            
            if (!IsEmpty(nx, ny) && IsEnemy(nx, ny))
            {
                moves[arrPos++] = new BoardPos(nx, ny, _addPromTile(BoardPos.MoveType.AttackMove, ny));
            }
        }
        
        return FilterAllowedTiles(moves, arrPos);
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
        => y == _promTile ? move | BoardPos.MoveType.PromotionMove : move;
    
    private bool _isElPassPossible(int nx, int ny)
    {
        if (Parent.BoardFigures[nx, ny].TextureIndex == _enemyPawnId && Parent.MovesHistory.Last.Value.FigT == _enemyPawnId
            && Math.Abs(Parent.MovesHistory.Last.Value.OldY - Parent.MovesHistory.Last.Value.NewPos.Y) == 2) return true;
        else return false;
    }
    
// ------------------------------
// variables and properties
// ------------------------------

    private readonly int _promTile;
    private readonly int _mvCord;
    private readonly int _elPassantX;
    private readonly Board.ChessComponents _enemyPawnId;
    private const int MaxMoves = 4;

    private static readonly int[] XAttackCords = new int[2] { -1, 1 };
}