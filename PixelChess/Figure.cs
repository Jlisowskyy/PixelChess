using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PongGame;
    
    /*  GENERAL TODO:
     * - MAKE THIS SHIT TEMPLATED
     * - consider faster solutions
     * - make fen translator
     * 
     */

public abstract class Figure
    // All figures expects to have array attached board attached, otherwise undefined
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
    
    
// ------------------------------
// helping protected fields
// ------------------------------

    protected bool IsEmpty(int x, int y)
    {
        return Parent.BoardFigures[x, y] == null;
    }

    protected bool IsEnemy(int x, int y)
    {
        return Parent.BoardFigures[x, y].Color != this.Color;
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
    public bool IsAlive = true;
    public bool IsMoved = false;
    public readonly Board.ChessComponents TextureIndex;
    public BoardPos Pos;

    public ColorT Color
    {
        get;
    }
}

public class Pawn : Figure
{
// --------------------------------
// type construction / setups
// --------------------------------
    public Pawn(int x, int y, ColorT color) :
        base(x, y, color, color == ColorT.White ? Board.ChessComponents.WhitePawn : Board.ChessComponents.BlackPawn)
    {
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
        
        if (IsMoved)
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
                if (IsEmpty(Pos.X, Pos.Y + _mvCord))
                {
                    moves[arrPos++] = new BoardPos(Pos.X, Pos.Y + dist * _mvCord);
                }
                else break;
            }
        }

        int ny = Pos.Y + _mvCord;
        for (int i = 0; i < 2; ++i)
        {
            int nx = Pos.X + XAttackCords[i];
            if (nx >= BoardPos.MinPos && !IsEmpty(nx, ny) && IsEnemy(nx, ny))
            {
                moves[arrPos++] = new BoardPos(nx, ny, _addPromTile(BoardPos.MoveType.AttackMove, ny));
            }
        }
        
        return (moves, arrPos);
    }
    
// ------------------------------
// helping methods
// ------------------------------

    private BoardPos.MoveType _addPromTile(BoardPos.MoveType move, int y)
        => y == _promTile ? move | BoardPos.MoveType.PromotionMove : move;

    private bool _isElPassPossible(int nx, int ny)
    {
        if (Parent.BoardFigures[nx, ny].TextureIndex == _enemyPawnId && Parent.MovesList.Last.Value.FigT == _enemyPawnId
            && Math.Abs(Parent.MovesList.Last.Value.OldY - Parent.MovesList.Last.Value.NewPos.Y) == 2) return true;
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
        for (int i = 0; i < Board.BoardSize; ++i)
        {
            for (int j = 0; j < Board.BoardSize; ++j)
            {
                
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

        for (int i = 0; i < 4; ++i)
        {
            int tempX = Pos.X + XPosTable[i];
            int tempY = Pos.Y + YPosTable[i];

            if (BoardPos.isOnBoard(tempX, tempY))
            {
                if (IsEmpty(tempX, tempY))
                    ret[arrPos++] = new BoardPos(tempX, tempY);
                else if (IsEnemy(tempX, tempY))
                    ret[arrPos++] = new BoardPos(tempX, tempY, BoardPos.MoveType.AttackMove);
            }

            tempX = Pos.X + XPosTable[i];
            tempY = Pos.Y - YPosTable[i];
            
            if (BoardPos.isOnBoard(tempX, tempY))
            {
                if (IsEmpty(tempX, tempY))
                    ret[arrPos++] = new BoardPos(tempX, tempY);
                else if (IsEnemy(tempX, tempY))
                    ret[arrPos++] = new BoardPos(tempX, tempY, BoardPos.MoveType.AttackMove);
            }
        }

        return (ret, arrPos);
    }
    
    private const int MaxPossibleTiles = 8;
    private static readonly int[] XPosTable = new[] { -2, -1, 1, 2 };
    private static readonly int[] YPosTable = new[] { 1, 2, 2, 1 };
    private static BoardPos[,][] movesTable = new BoardPos[8,8][];
}

public class Bishop : Figure
{
    public Bishop(int x, int y, ColorT color) :
        base(x, y, color, color == ColorT.White ? Board.ChessComponents.WhiteBishop : Board.ChessComponents.BlackBishop) {}

    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    
    {
        BoardPos[] ret = new BoardPos[MaxPossibleTiles];
        int arrPos = 0;
        
        int xTemp = Pos.X + 1;
        int yTemp = Pos.Y + 1;

        while (xTemp <= BoardPos.MaxPos && yTemp <= BoardPos.MaxPos)
        {
            if (IsEmpty(xTemp, yTemp))
            {
                ret[arrPos++] = new BoardPos(xTemp, yTemp);
            }
            else
            {
                if (IsEnemy(xTemp, yTemp))
                {
                    ret[arrPos++] = new BoardPos(xTemp, yTemp, BoardPos.MoveType.AttackMove);
                }

                break;
            }

            ++xTemp;
            ++yTemp;
        }
        
        xTemp = Pos.X + 1;
        yTemp = Pos.Y - 1;

        while (xTemp <= BoardPos.MaxPos && yTemp >= BoardPos.MinPos)
        {
            if (IsEmpty(xTemp, yTemp))
            {
                ret[arrPos++] = new BoardPos(xTemp, yTemp);
            }
            else
            {
                if (IsEnemy(xTemp, yTemp))
                {
                    ret[arrPos++] = new BoardPos(xTemp, yTemp, BoardPos.MoveType.AttackMove);
                }

                break;
            }

            ++xTemp;
            --yTemp;
        }

        xTemp = Pos.X - 1;
        yTemp = Pos.Y + 1;

        while (xTemp >= BoardPos.MinPos && yTemp <= BoardPos.MaxPos)
        {
            if (IsEmpty(xTemp, yTemp))
            {
                ret[arrPos++] = new BoardPos(xTemp, yTemp);
            }
            else
            {
                if (IsEnemy(xTemp, yTemp))
                {
                    ret[arrPos++] = new BoardPos(xTemp, yTemp, BoardPos.MoveType.AttackMove);
                }

                break;
            }

            --xTemp;
            ++yTemp;
        }

        xTemp = Pos.X - 1;
        yTemp = Pos.Y - 1;

        while (xTemp >= BoardPos.MaxPos && yTemp >= BoardPos.MaxPos)
        {
            if (IsEmpty(xTemp, yTemp))
            {
                ret[arrPos++] = new BoardPos(xTemp, yTemp);
            }
            else
            {
                if (IsEnemy(xTemp, yTemp))
                {
                    ret[arrPos++] = new BoardPos(xTemp, yTemp, BoardPos.MoveType.AttackMove);
                }

                break;
            }

            --xTemp;
            --yTemp;
        }
        
        return (ret, arrPos);
    }
    
    internal const int MaxPossibleTiles = 13;
}

public class Rook : Figure
{
    public Rook(int x, int y, ColorT color) :
        base(x, y, color, color == ColorT.White ? Board.ChessComponents.WhiteRook : Board.ChessComponents.BlackRook) {}

    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        BoardPos[] ret = new BoardPos[RookCorrectTiles];
        int arrPos = 0;

        for (int i = Pos.X - 1; i >= BoardPos.MinPos; --i)
        {
            if (IsEmpty(i, Pos.Y))
                ret[arrPos++] = new BoardPos(i, Pos.Y);
            else
            {
                if (IsEnemy(i, Pos.Y)) ret[arrPos++] = new BoardPos(i, Pos.Y, BoardPos.MoveType.AttackMove);
                break;
            }
        }
        
        for (int i = Pos.X + 1; i <= BoardPos.MaxPos; ++i)
        {
            if (IsEmpty(i, Pos.Y))
                ret[arrPos++] = new BoardPos(i, Pos.Y);
            else
            {
                if (IsEnemy(i, Pos.Y)) ret[arrPos++] = new BoardPos(i, Pos.Y, BoardPos.MoveType.AttackMove);
                break;
            }
        }
        
        for (int i = Pos.Y - 1; i >= BoardPos.MinPos; --i)
        {
            if (IsEmpty(Pos.X, i))
                ret[arrPos++] = new BoardPos(Pos.X, i);
            else
            {
                if (IsEnemy(Pos.X, i)) ret[arrPos++] = new BoardPos(Pos.X, i, BoardPos.MoveType.AttackMove);
                break;
            }
        }
        
        for (int i = Pos.Y + 1; i <= BoardPos.MaxPos; ++i)
        {
            if (IsEmpty(Pos.X, i))
                ret[arrPos++] = new BoardPos(Pos.X, i);
            else
            {
                if (IsEnemy(Pos.X, i)) ret[arrPos++] = new BoardPos(Pos.X, i, BoardPos.MoveType.AttackMove);
                break;
            }
        }
 
        return (ret, arrPos);
    }
    
    internal const int RookCorrectTiles = 14;
}

public class Queen : Figure
{
    public Queen(int x, int y, ColorT color) :
        base(x, y, color, color == ColorT.White ? Board.ChessComponents.WhiteQueen : Board.ChessComponents.BlackQueen){}
    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        BoardPos[] ret = new BoardPos[QueenMaxTiles];
        
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
    
    private const int QueenMaxTiles = Rook.RookCorrectTiles + Bishop.MaxPossibleTiles;
}

public class King : Figure
{
    public King(int x, int y, ColorT color):
        base(x, y, color, color == ColorT.White? Board.ChessComponents.WhiteKing : Board.ChessComponents.BlackKing) {}
    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        BoardPos[] ret = new BoardPos[KingMaxTiles];
        int arrPos = 0;

        for (int i = -1; i < 2; ++i)
        {
            for (int j = -1; j < 2; ++j)
            {
                if (i == 0 && j == 0) continue;

                int tempX = Pos.X + i;
                int tempY = Pos.Y + j;
                if (BoardPos.isOnBoard(tempX, tempY))
                {
                    if (IsEmpty(tempX, tempY))
                        ret[arrPos++] = new BoardPos(tempX, tempY);
                    else if (IsEnemy(tempX, tempY))
                        ret[arrPos++] = new BoardPos(tempX, tempY, BoardPos.MoveType.AttackMove);
                }
            }
        }
        
        if (!IsMoved)
            arrPos = GetRochadeMoves(ret, arrPos);

        return (ret, arrPos);
    }
    
    
    int GetRochadeMoves(BoardPos[] moves, int arrPos)
    {
        const int shortRoshadeX= 6;
        const int longRoshadeX = 2;
        Board.ChessComponents rookType =
            Color == ColorT.White ? Board.ChessComponents.WhiteRook : Board.ChessComponents.BlackRook; 
        int i;
        
        // TODO: ADD PROTECTION FROM DOUBLE TOWER MOVE THEN ROSHADE XDDDD 

        if (!IsEmpty(BoardPos.MinPos, Pos.Y) 
            && Parent.BoardFigures[BoardPos.MinPos, Pos.Y].TextureIndex == rookType 
            && Parent.BoardFigures[BoardPos.MinPos, Pos.Y].IsMoved == false)
        {
            for (i = Pos.X; i < BoardPos.MaxPos; ++i)
            {
                if (!IsEmpty(i, Pos.Y))
                    break;

                // TODO: check for attacks
            }

            if (i == BoardPos.MaxPos)
                moves[arrPos++] = new BoardPos(shortRoshadeX, Pos.Y, BoardPos.MoveType.CastlingMove);
        }

        if (!IsEmpty(BoardPos.MaxPos, Pos.Y) 
            && Parent.BoardFigures[BoardPos.MaxPos, Pos.Y].TextureIndex == rookType 
            && Parent.BoardFigures[BoardPos.MaxPos, Pos.Y].IsMoved == false)
        {
            for (i = Pos.X; i > BoardPos.MinPos; --i)
            {
                if (!IsEmpty(i, Pos.Y))
                    break;

                // TODO: check for attacks
            }

            if (i == BoardPos.MinPos)
                moves[arrPos++] = new BoardPos(longRoshadeX, Pos.Y, BoardPos.MoveType.CastlingMove);
        }

        return arrPos;
    }
    
    private const int KingMaxTiles = 8 + 2;
}