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

        CheckForSep(fenInput);
        
        ret.StartingColor = ProcessMovingColor(fenInput);
        CheckForSep(fenInput);
        
        ProcessCastling(fenInput);
        CheckForSep(fenInput);

        ProcessElPassant(fenInput);
        CheckForSep(fenInput);
        
        // TODO: 50 moves rule
        
        return ret;
    }
    
// ------------------------------
// private helping methods
// ------------------------------

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
                    FindCastlingRook(0, 7, Figure.ColorT.White);
                    break;
                case 'k':
                    FindCastlingRook(7, 0, Figure.ColorT.Black);
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
                arr[i].IsMoved = false;
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
            Figure.ColorT col = input == fenInput[_strPos] ? Figure.ColorT.Black : Figure.ColorT.White;
            
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
                    if (_inLineTile >= Board.BoardSize)
                        throw new ApplicationException("Invalid fen input");

                    _inLineTile = 0;
                    break;
            }
            
            _tile++;
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
    }

    private static bool OutOfBoardRange(int x) => x < BoardPos.MinPos || x > BoardPos.MaxPos;
    private static bool  IsNumeric(char x) => x <= '9' && x >= 0;
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