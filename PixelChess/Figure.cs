using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PongGame;
    
    /*  GENERAL TODO:
     * - make fen translator
     * - add top level class to interact better with monogame
     */

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
    public abstract Figure Clone();
    
    
// ------------------------------
// helping protected fields
// ------------------------------

    protected bool IsEmpty(int x, int y) => Parent.BoardFigures[x, y] == null;

    protected bool IsEnemy(int x, int y) => Parent.BoardFigures[x, y].Color != this.Color;
    
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
    
    public readonly Board.ChessComponents TextureIndex;
    // also used to identify figures color or type
    
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
            
            // TODO: checks speed changes after this removal
            if (nx > BoardPos.MaxPos || nx < BoardPos.MinPos) continue;
            
            if (!IsEmpty(nx, ny) && IsEnemy(nx, ny))
            {
                moves[arrPos++] = new BoardPos(nx, ny, _addPromTile(BoardPos.MoveType.AttackMove, ny));
            }
        }
        
        return (moves, arrPos);
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
        
        return (ret, arrPos);
    }
    
// ------------------------------
// variables and properties
// ------------------------------
    
    private const int MaxPossibleTiles = 8;
    private static readonly int[] XPosTable = new[] { -2, -1, 1, 2 };
    private static readonly int[] YPosTable = new[] { 1, 2, 2, 1 };
    private static BoardPos[,][] movesTable = new BoardPos[8,8][];
}

public class Bishop : Figure
{
// --------------------------------
// type construction / setups
// --------------------------------
    public Bishop(int x, int y, ColorT color) :
        base(x, y, color, color == ColorT.White ? Board.ChessComponents.WhiteBishop : Board.ChessComponents.BlackBishop) {}

    static Bishop()
    {
        MoveLimMap = new int[8, 8][];
        for (int x = 0; x < 8; ++x)
        {
            for (int y = 0; y < 8; ++y)
            {
                // in order [ sw nw ne se ]
                MoveLimMap[x, y] = new int[4];

                MoveLimMap[x, y][0] = Math.Min(x, y);
                MoveLimMap[x, y][1] = Math.Min(x, Math.Abs(BoardPos.MaxPos - y));
                MoveLimMap[x, y][2] = Math.Min(Math.Abs(BoardPos.MaxPos - x), Math.Abs(BoardPos.MaxPos - y));
                MoveLimMap[x, y][3] = Math.Min(Math.Abs(BoardPos.MaxPos - x), y);
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

        // loops through all directions from [ sw nw ne se ]
        for (int i = 0; i < 4; ++i)
        {
            int nx = Pos.X;
            int ny = Pos.Y;
            for (int j = 0; j < MoveLimMap[Pos.X, Pos.Y][i]; ++j)
            {
                nx += XMoves[i];
                ny += YMoves[i];

                if (!IsEmpty(nx, ny))
                {
                    if (IsEnemy(nx, ny))
                        ret[arrPos++] = new BoardPos(nx, ny, BoardPos.MoveType.AttackMove);
                    break;
                }

                ret[arrPos++] = new BoardPos(nx, ny);
            }
        }
        
        return (ret, arrPos);
    }
    
    public override Figure Clone() => new Bishop(Pos.X, Pos.Y, Color)
    {
        IsAlive = this.IsAlive,
        IsMoved = this.IsMoved
    };
    
// ------------------------------
// variables and properties
// ------------------------------

    internal const int MaxPossibleTiles = 13;
    
    public enum dir
    {
        Sw,
        Nw,
        Ne,
        Se
    }
    
    // Contains limits for each diagonal moves on all specific fields
    // in order [ sw nw ne se ]
    public static readonly int[,][] MoveLimMap;

    // in order [ sw nw ne se ]
    public static readonly int[] XMoves = { -1, -1, 1, 1 };
    public static readonly int[] YMoves = { -1, 1, 1, -1 };
}

public class Rook : Figure
{
// --------------------------------
// type construction / setups
// --------------------------------
    public Rook(int x, int y, ColorT color) :
        base(x, y, color, color == ColorT.White ? Board.ChessComponents.WhiteRook : Board.ChessComponents.BlackRook)
    {}

// --------------------------------
// abstract method overwrite
// --------------------------------
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
    
    public override Figure Clone() => new Rook(Pos.X, Pos.Y, Color)
    {
        IsAlive = this.IsAlive,
        IsMoved = this.IsMoved
    };

// ------------------------------
// variables and properties
// ------------------------------

    internal const int RookCorrectTiles = 14;
}

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

public class King : Figure
{
// --------------------------------
// type construction / setups
// --------------------------------
    public King(int x, int y, ColorT color):
        base(x, y, color, color == ColorT.White? Board.ChessComponents.WhiteKing : Board.ChessComponents.BlackKing) {}

// --------------------------------
// abstract method overwrite
// --------------------------------
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
                if (BoardPos.isOnBoard(tempX, tempY) && Parent.BlockedTiles[(int)Color][tempX, tempY] == Board.TileState.UnblockedTile)
                {
                    if (IsEmpty(tempX, tempY))
                        ret[arrPos++] = new BoardPos(tempX, tempY);
                    else if (IsEnemy(tempX, tempY))
                        ret[arrPos++] = new BoardPos(tempX, tempY, BoardPos.MoveType.AttackMove);
                }
            }
        }
        
        if (!IsMoved)
            arrPos = GetCastlingMoves(ret, arrPos);

        return (ret, arrPos);
    }
    
    public override Figure Clone() => new King(Pos.X, Pos.Y, Color)
    {
        IsAlive = this.IsAlive,
        IsMoved = this.IsMoved
    };
    
// ------------------------------
// private help method
// ------------------------------
    
    private int GetCastlingMoves(BoardPos[] moves, int arrPos)
    {
        Board.ChessComponents rookType =
            Color == ColorT.White ? Board.ChessComponents.WhiteRook : Board.ChessComponents.BlackRook; 
        int i;

        if (!IsEmpty(BoardPos.MinPos, Pos.Y) 
            && Parent.BoardFigures[BoardPos.MinPos, Pos.Y].TextureIndex == rookType 
            && Parent.BoardFigures[BoardPos.MinPos, Pos.Y].IsMoved == false)
        {
            for (i = Pos.X + 1; i < BoardPos.MaxPos; ++i)
            {
                if (!IsEmpty(i, Pos.Y) || Parent.BlockedTiles[(int)Color][i, Pos.Y] == Board.TileState.BlockedTile)
                    break;

                // TODO: check for attacks
            }

            if (i == BoardPos.MaxPos)
                moves[arrPos++] = new BoardPos(ShortCastlingX, Pos.Y, BoardPos.MoveType.CastlingMove);
        }

        if (!IsEmpty(BoardPos.MaxPos, Pos.Y) 
            && Parent.BoardFigures[BoardPos.MaxPos, Pos.Y].TextureIndex == rookType 
            && Parent.BoardFigures[BoardPos.MaxPos, Pos.Y].IsMoved == false)
        {
            for (i = Pos.X - 1; i > BoardPos.MinPos; --i)
            {
                if (!IsEmpty(i, Pos.Y) || Parent.BlockedTiles[(int)Color][i, Pos.Y] == Board.TileState.BlockedTile)
                    break;
            }

            if (i == BoardPos.MinPos)
                moves[arrPos++] = new BoardPos(LongCastlingX, Pos.Y, BoardPos.MoveType.CastlingMove);
        }

        return arrPos;
    }
    
// ------------------------------
// variables and properties
// ------------------------------

    private const int ShortCastlingX= 6;
    private const int LongCastlingX = 2;
    public const int ShortCastlingRookX = 5;
    public const int LongCastlingRookX= 3;
    private const int KingMaxTiles = 8 + 2;
}