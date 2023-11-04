using PongGame.Figures;
using System;
using System.Collections.Generic;
using PongGame;

namespace PongGame;

public static class FenTranslator
{
        public static string Translate(Figure[,] chessBoard)
    {


        throw new NotImplementedException();
    }

    public static Board.Layout Translate(string fenInput)
    {
        Board.Layout  ret = new Board.Layout ();
        _blackFigs = new Figure[BoardTiles];
        _whiteFigs = new Figure[BoardTiles];
        _strPos = _whitePos = _blackPos = _inLineTile = _tile = 0;
        
        for (; fenInput[_strPos] != ' ' && _strPos < fenInput.Length; ++_strPos)
            ProcessPositionalInput(fenInput);

        if (_inLineTile != Board.BoardSize || _tile != BoardTiles)
            throw new ApplicationException("Not every tile on board was declared");

        ret.FigArr = MergeArrays();
        ret.FirstBlackFig = _whitePos;
        
        CheckForSep(fenInput);
        
        ret.StartingColor = ProcessMovingColor(fenInput);
        CheckForSep(fenInput);
        
        ProcessCastling(fenInput);
        CheckForSep(fenInput);

        ret.ElPassantPos = ProcessElPassant(fenInput);
        CheckForSep(fenInput);

        (ret.HalfMoves, ret.FullMoves) = ProcessMovesCounters(fenInput);
        
        return ret;
    }
    
// ------------------------------
// private helping methods
// ------------------------------

    private static Figure[] MergeArrays()
    {
        Figure[] arr = new Figure[_whitePos + _blackPos];
        _whiteFigs[.._whitePos].CopyTo(arr, 0);
        _blackFigs[.._blackPos].CopyTo(arr, _whitePos);

        return arr;
    }

    private static (int, int) ProcessMovesCounters(string fenInput)
    {
        string rest = fenInput[_strPos..];
        var components = rest.Split(' ');

        if (components.Length > 2)
            throw new ApplicationException("Too many components in moves counters");

        int[] movesCounters = new int[2];

        for (int i = 0; i < 2; ++i)
        {
            if (i == components.Length) break;
            if (components[i] == "-") continue;

            if (int.TryParse(components[i], out int mvs))
                movesCounters[i] = mvs;
            else throw new ApplicationException("Unexpected token passed to moves counters");
        }

        return (movesCounters[0], movesCounters[1]);
    }

    private static BoardPos ProcessElPassant(string fenInput)
    {
        CheckForRange(fenInput);
        
        if (fenInput[_strPos] == '-')
        {
            _strPos++;
            return new BoardPos(-1, -1);
        }

        int xPos = char.ToLower(fenInput[_strPos++]) - 'a';

        if (OutOfBoardRange(xPos))
            throw new ApplicationException("Invalid position passed at el passant field");

        CheckForRange(fenInput);
        int yPos = fenInput[_strPos++] - '1';
        
        if (OutOfBoardRange(xPos))
            throw new ApplicationException("Invalid position passed at el passant field");

        return new BoardPos(xPos, yPos);
    }

    private static void ProcessCastling(string fenInput)
    {
        CheckForRange(fenInput);
        
        if (fenInput[_strPos] == '-')
        {
            ++_strPos;
            return;
        }

        for (; fenInput[_strPos] != ' ' && _strPos < fenInput.Length; ++_strPos)
        {
            switch (fenInput[_strPos])
            {
                case 'K':
                    FindCastlingRook(0, 0, Figure.ColorT.White);
                    break;
                case 'Q':
                    FindCastlingRook(7, 0, Figure.ColorT.White);
                    break;
                case 'k':
                    FindCastlingRook(0, 7, Figure.ColorT.Black);
                    break;
                case 'q':
                    FindCastlingRook(7, 7, Figure.ColorT.Black);
                    break;
                default:
                    throw new ApplicationException("Invalid fen input inside castling section");
            }
        }
    }

    private static void FindCastlingRook(int x, int y, Figure.ColorT col)
    {
        Figure[] arr;
        Board.ChessComponents ind;
        int bound;

        if (col == Figure.ColorT.White)
        {
            arr = _whiteFigs;
            bound = _whitePos;
            ind = Board.ChessComponents.WhiteRook;
        }
        else
        {
            arr = _blackFigs;
            bound = _blackPos;
            ind = Board.ChessComponents.BlackRook;
        }

        for (int i = 0; i < bound; ++i)
        {
            if (arr[i].TextureIndex == ind && arr[i].Pos.X == x && arr[i].Pos.Y == y)
            {
                arr[i].IsMoved = false;
                return;
            }
        }

        throw new ApplicationException("Fen inputs says castling is available bur there is no such rook");
    }

    private static Figure.ColorT ProcessMovingColor(string fen)
    {
        CheckForRange(fen);
        Figure.ColorT ret;
        
        if (fen[_strPos] == 'w') ret = Figure.ColorT.White;
        else if (fen[_strPos] == 'b') ret =  Figure.ColorT.Black;
        else throw new ApplicationException("Unrecognized color passed as moving");

        _strPos++;
        return ret;
    }
    
    private static void ProcessPositionalInput(string fenInput)
    {
        if (IsNumeric(fenInput[_strPos]))
        {
            var toSkip = ToNumeric(fenInput[_strPos]);
            
            for (int j = 0; j < toSkip; ++j)
            {
                ++_tile;
                ++_inLineTile;
            }
        }
        else
        {
            int x = GetXPos(_tile);
            int y = GetYPos(_tile);

            char input = char.ToUpper(fenInput[_strPos]);
            Figure.ColorT col = input == fenInput[_strPos] ? Figure.ColorT.White : Figure.ColorT.Black;
            
            switch (input)
            {
                case 'R':
                    PlaceFigure(new Rook(x, y, col) { IsMoved = true });
                    break;
                case 'N':
                    PlaceFigure(new Knight(x, y, col));
                    break;
                case 'B':
                    PlaceFigure(new Bishop(x, y, col));
                    break;
                case 'Q':
                    PlaceFigure(new Queen(x, y, col));
                    break;
                case 'K':
                    PlaceFigure(new King(x, y, col));
                    break;
                case 'P':
                    PlaceFigure(new Pawn(x, y, col));
                    break;
                case '/':
                    if (_inLineTile > Board.BoardSize)
                        throw new ApplicationException("To much figures on line");
                    else if (_inLineTile < Board.BoardSize)
                        throw new ApplicationException("Not enough figures on line");

                    _inLineTile = 0;
                    break;
                default:
                    throw new ApplicationException("Unrecognized figure sign");
            }
        }
    }

    private static void CheckForRange(string fen)
    {
        if (_strPos == fen.Length)
            throw new ApplicationException("Fen input too short");
    }
    private static void CheckForSep(string fen)
    {
        CheckForRange(fen);
        
        if (fen[_strPos++] != ' ')
            throw new ApplicationException("Lack of separator");
    }
    private static void PlaceFigure(Figure fig)
    {
        if (fig.Color == Figure.ColorT.White)
            _whiteFigs[_whitePos++] = fig;
        else _blackFigs[_blackPos++] = fig;

        ++_inLineTile;
        ++_tile;
    }

    private static bool OutOfBoardRange(int x) => x < BoardPos.MinPos || x > BoardPos.MaxPos;
    private static bool  IsNumeric(char x) => x <= '9' && x >= '0';
    private static int ToNumeric(char x) => x - '0';

    private static int GetXPos(int x) => x % Board.BoardSize;
    private static int GetYPos(int x) => BoardPos.MaxPos - x / Board.BoardSize;
    
// ------------------------------
// private static fields
// ------------------------------

    private static int _tile;
    private static int _inLineTile;
    private static int _strPos;
        
    private static Figure[] _blackFigs;
    private static int _blackPos;
    private static Figure[] _whiteFigs;
    private static int _whitePos;

// ------------------------------
// private static consts
// ------------------------------

    const int BoardTiles = Board.BoardSize * Board.BoardSize;
    
}