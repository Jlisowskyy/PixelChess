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
    public readonly Game1.chessComponents TextureIndex;
    protected Figure(int x, int y, ColorT color, Game1.chessComponents textureIndex)
    {
        Pos.X = x;
        Pos.Y = y;
        Color = color;
        TextureIndex = textureIndex;
    }
    public Figure[,] Parent
    {
        set => _parent = value;
    }

    protected bool isEmpty(BoardPos pos)
    {
        return _parent[pos.X, pos.Y] == null;
    }

    protected bool isEnemy(BoardPos pos)
    {
        return _parent[pos.X, pos.Y].Color != this.Color;
    }
    
    protected bool IsEmpty(int x, int y)
    {
        return _parent[x, y] == null;
    }

    protected bool IsEnemy(int x, int y)
    {
        return _parent[x, y].Color != this.Color;
    }

    public abstract (BoardPos[] moves, int movesCount) GetMoves();

    public enum ColorT
    {
        White,
        Black,
    }

    private Figure[,] _parent;
    public bool IsAlive = true;
    public BoardPos Pos;

    public ColorT Color
    {
        get;
    }
}

public class Pawn : Figure
{
    private int _moveWhite(int cord, int dist) => cord + dist;
    private int _moveBlack(int cord, int dist) => cord - dist;

    public Pawn(int x, int y, ColorT color) :
        base(x, y, color, color == ColorT.White ? Game1.chessComponents.WhitePawn : Game1.chessComponents.BlackPawn)
    {
        _moveFunc = color == ColorT.White ? _moveWhite : _moveBlack;
        _promTile = color == ColorT.White ? BoardPos.MaxPos : BoardPos.MinPos;
    }
    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        BoardPos[] moves = new BoardPos[MaxMoves];
        int arrPos = 0;
        
        if (_isMoved)
        {
            if (IsEmpty(Pos.X, _moveFunc(Pos.Y)))
            {
                moves[arrPos++] = new BoardPos(Pos.X, _moveFunc(Pos.Y), _moveFunc(Pos.Y) == _promTile ? BoardPos.MoveType.PromotionMove : BoardPos.MoveType.NormalMove);
            }
        }
        else
        {
            for (int dist = 1; dist < 3; ++dist)
            {
                if (IsEmpty(Pos.X, _moveFunc(Pos.Y, dist)))
                {
                    moves[arrPos++] = new BoardPos(Pos.X, _moveFunc(Pos.X, dist));
                }
                else
                {
                    break;
                }
            }
        }

        if (Pos.X - 1 >= BoardPos.MinPos && !IsEmpty(Pos.X - 1, _moveFunc(Pos.Y)) && IsEnemy(Pos.X - 1, _moveFunc(Pos.Y)))
        {
            moves[arrPos++] = new BoardPos(Pos.X - 1, _moveFunc(Pos.Y),
                _moveFunc(Pos.Y) == _promTile
                    ? BoardPos.MoveType.AttackMove | BoardPos.MoveType.PromotionMove
                    : BoardPos.MoveType.AttackMove);
        }
        
        if (Pos.X + 1 >= BoardPos.MinPos && !IsEmpty(Pos.X + 1, _moveFunc(Pos.Y)) && IsEnemy(Pos.X + 1, _moveFunc(Pos.Y)))
        {
            moves[arrPos++] = new BoardPos(Pos.X + 1, _moveFunc(Pos.Y),
                _moveFunc(Pos.Y) == _promTile
                    ? BoardPos.MoveType.AttackMove | BoardPos.MoveType.PromotionMove
                    : BoardPos.MoveType.AttackMove);
        }
        
        return (moves, arrPos);
    }
    
    private bool _isMoved = false;
    private int _promTile;
    private const int MaxMoves = 4;
    private delegate int MoveFuncDelegate(int cord, int dist = 1);

    private MoveFuncDelegate _moveFunc;
}

public class Knight : Figure
{
    public Knight(int x, int y, ColorT color):
        base(x, y, color, color == ColorT.White ? Game1.chessComponents.WhiteKnight : Game1.chessComponents.BlackKnight) {}

    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        BoardPos[] ret = new BoardPos[MaxPossibleTiles];
        int arrPos = 0;

        for (int i = 0; i < 4; ++i)
        {
            int tempX = Pos.X + XPosTable[i];
            int tempY = Pos.Y + YPosTable[i];

            if (tempX >= BoardPos.MinPos && tempX <= BoardPos.MaxPos && tempY >= BoardPos.MinPos &&
                tempY <= BoardPos.MaxPos)
            {
                if (IsEmpty(tempX, tempY))
                    ret[arrPos++] = new BoardPos(tempX, tempY);
                else if (IsEnemy(tempX, tempY))
                    ret[arrPos++] = new BoardPos(tempX, tempY, BoardPos.MoveType.AttackMove);
            }

            tempX = Pos.X + XPosTable[i];
            tempY = Pos.Y - YPosTable[i];
            if (tempX >= BoardPos.MinPos && tempX <= BoardPos.MaxPos && tempY >= BoardPos.MinPos &&
                tempY <= BoardPos.MaxPos)
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
}

public class Bishop : Figure
{
    public Bishop(int x, int y, ColorT color) :
        base(x, y, color, color == ColorT.White ? Game1.chessComponents.WhiteBishop : Game1.chessComponents.BlackBishop) {}

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
        base(x, y, color, color == ColorT.White ? Game1.chessComponents.WhiteRook : Game1.chessComponents.BlackRook) {}

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
        base(x, y, color, color == ColorT.White ? Game1.chessComponents.WhiteQueen : Game1.chessComponents.BlackQueen){}
    public sealed override (BoardPos[] moves, int movesCount) GetMoves()
    {
        BoardPos[] ret = new BoardPos[QueenMaxTiles];
        
        // TODO: speedup???
        Figure rook = new Rook(Pos.X, Pos.Y, Color);
        Figure bishop = new Bishop(Pos.X, Pos.Y, Color);

        var rookRet = rook.GetMoves();
        var bishopRet = bishop.GetMoves();
        
        rookRet.moves.CopyTo(ret, 0);
        bishopRet.moves.CopyTo(ret, rookRet.movesCount);

        return (ret, Rook.RookCorrectTiles + bishopRet.movesCount);
    }
    
    private const int QueenMaxTiles = Rook.RookCorrectTiles + Bishop.MaxPossibleTiles;
}

public class King : Figure
{
    public King(int x, int y, ColorT color):
        base(x, y, color, color == ColorT.White? Game1.chessComponents.WhiteKing : Game1.chessComponents.BlackKing) {}
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
                if (tempX >= BoardPos.MinPos && tempX <= BoardPos.MaxPos && tempY >= BoardPos.MinPos && tempY <= BoardPos.MaxPos)
                {
                    if (IsEmpty(tempX, tempY))
                        ret[arrPos++] = new BoardPos(tempX, tempY);
                    else if (IsEnemy(tempX, tempY))
                        ret[arrPos++] = new BoardPos(tempX, tempY, BoardPos.MoveType.AttackMove);
                }
            }
        }

        return (ret, arrPos);
    }
    
    private const int KingMaxTiles = 8;
}