#define DEBUG_

using System;
using PixelChess.Figures;

namespace PixelChess.ChessBackend;

// TODO: probably there should be some check whether king stays on correct not moved position
public static class FenTranslator
{
        public static string GetPosString(Board chessBoard){
            char[] fenResult = new char[MaxFenLength];
            int tabIndex = 0;

            for (int y = BoardPos.MaxPos; y >= BoardPos.MinPos; --y)
            {
                int dist = 0;
                
                for (int x = BoardPos.MinPos; x <= BoardPos.MaxPos; ++x)
                {
                    if (chessBoard.BoardFigures[x, y] == null)
                    {
                        dist++;
                        continue;
                    }

                    if (dist != 0)
                    {
                        fenResult[tabIndex++] = (char)('0' + dist);
                        dist = 0;
                    }

                    fenResult[tabIndex++] = _getCharFigRepresentation(chessBoard.BoardFigures[x, y].TextureIndex);
                }
                
                if (dist != 0) fenResult[tabIndex++] = (char)('0' + dist);
                if (y != BoardPos.MinPos) fenResult[tabIndex++] = '/';
            }
            
            return (new string(fenResult))[..tabIndex];
        }
        public static string Translate(Board chessBoard)
        {
            char[] fenResult = new char[MaxFenLength];
            int tabIndex = 0;

            var posString = GetPosString(chessBoard).ToCharArray();
            posString.CopyTo(fenResult, 0);
            tabIndex += posString.Length;

            fenResult[tabIndex++] = ' ';
            fenResult[tabIndex++] = chessBoard.MovingColor == Figure.ColorT.White ? 'w' : 'b';
            fenResult[tabIndex++] = ' ';

            tabIndex = _appendCastlingPossibilities(chessBoard, fenResult, tabIndex);
            fenResult[tabIndex++] = ' ';
            tabIndex = _checkElPassant(chessBoard, fenResult, tabIndex);
            fenResult[tabIndex++] = ' ';

            var halfMovesArr = chessBoard.HalfMoves.ToString().ToCharArray();
            halfMovesArr.CopyTo(fenResult, tabIndex);
            tabIndex += halfMovesArr.Length;
            
            fenResult[tabIndex++] = ' ';
            
            var fullMovesArr = chessBoard.FullMoves.ToString().ToCharArray();
            fullMovesArr.CopyTo(fenResult, tabIndex);
            tabIndex += fullMovesArr.Length;
            
            return (new string(fenResult))[..tabIndex];
        }

    public static Board.Layout Translate(string fenInput)
    {
        Board.Layout  ret = new Board.Layout ();
        _blackFigs = new Figure[BoardTiles];
        _whiteFigs = new Figure[BoardTiles];
        _strPos = _whitePos = _blackPos = _inLineTile = _tile = 0;
        
        // assuring that, there are no additional blank characters
        fenInput = fenInput.Trim();
        
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

    // --------------------------------------------
    // Game representation to fen translation
    // --------------------------------------------

    private static readonly Board.ChessComponents[] ElPassantInd = 
        new[] { Board.ChessComponents.WhitePawn, Board.ChessComponents.BlackPawn };
    private static int _checkElPassant(Board chessBoard, char[] output, int indp)
    {
        int offset = 0;

        var lastMove = chessBoard.MovesHistory.Last!.Value;
        var fig = chessBoard.BoardFigures[lastMove.MadeMove.X, lastMove.MadeMove.Y];

        // there is sentinel on history list with null figure so we should exit before
        
        if (fig != null && fig.TextureIndex == ElPassantInd[(int)fig.Color] && Math.Abs(lastMove.OldY - lastMove.MadeMove.Y) == 2)
        {
            output[indp + offset++] = (char)('a' + lastMove.MadeMove.X);
            output[indp + offset++] = (char)('1' + lastMove.MadeMove.Y - 1);
        }

        if (offset == 0)
            output[indp + offset++] = '-';

        return indp + offset;
    }
    
    private static int _appendCastlingPossibilities(Board chessBoard, char[] output, int indp)
    {
        int offset = 0;
        
        for (int col = 0; col < 2; ++col)
        {
            var king = chessBoard.BoardFigures[KingPos[col].X, KingPos[col].Y];
            if (king != null && king.TextureIndex == KingInd[col] && king.IsMoved == false)
            {
                for (int castleType = 0; castleType < 2; ++castleType)
                {
                    var rook = chessBoard.BoardFigures[RookPos[col][castleType].X, RookPos[col][castleType].Y];

                    if (rook != null && rook.TextureIndex == RookInd[col] && rook.IsMoved == false)
                    {
                        output[indp + offset++] =
                            col == 0 ? char.ToUpper(CastleChar[castleType]) : CastleChar[castleType];
                    }
                }
            }
        }

        if (offset == 0)
        {
            output[indp] = '-';
            offset++;
        }
        
        return indp + offset;
    }

    private static char _getCharFigRepresentation(Board.ChessComponents ind)
    {
        return ind switch
        {
            Board.ChessComponents.BlackBishop => 'b',
            Board.ChessComponents.BlackKing => 'k',
            Board.ChessComponents.BlackRook => 'r',
            Board.ChessComponents.BlackKnight => 'n',
            Board.ChessComponents.BlackQueen => 'q',
            Board.ChessComponents.BlackPawn => 'p',
            Board.ChessComponents.WhiteBishop => 'B',
            Board.ChessComponents.WhiteKing => 'K',
            Board.ChessComponents.WhiteKnight => 'N',
            Board.ChessComponents.WhitePawn => 'P',
            Board.ChessComponents.WhiteQueen => 'Q',
            Board.ChessComponents.WhiteRook => 'R',
            _ => throw new ApplicationException("Unexpected figure passed to _getCharFigRepresentation")
        };
    }

    // --------------------------------------------
    // Fen to game representation translation
    // --------------------------------------------


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
                case 'Q':
                    FindCastlingRook(0, 0, Figure.ColorT.White);
                    break;
                case 'K':
                    FindCastlingRook(7, 0, Figure.ColorT.White);
                    break;
                case 'q':
                    FindCastlingRook(0, 7, Figure.ColorT.Black);
                    break;
                case 'k':
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
        Board.ChessComponents ind, kInd;
        int bound;

        if (col == Figure.ColorT.White)
        {
            arr = _whiteFigs;
            bound = _whitePos;
            ind = Board.ChessComponents.WhiteRook;
            kInd = Board.ChessComponents.WhiteKing;
        }
        else
        {
            arr = _blackFigs;
            bound = _blackPos;
            ind = Board.ChessComponents.BlackRook;
            kInd = Board.ChessComponents.BlackKing;
        }
        
        bool rookFound = false;
        // checking whole array, whether it contains required rook
        for (int i = 0; i < bound; ++i)
        {
            if (arr[i].TextureIndex == ind && arr[i].Pos.X == x && arr[i].Pos.Y == y)
            {
                arr[i].IsMoved = false;
                rookFound = true;
                break;
            }
        }
        
        if (!rookFound)
            throw new ApplicationException("Fen inputs says castling is available bur there is no such rook on required position!");
        
        // checking whole array to setup 
        for (int i = 0; i < bound; ++i)
        {
            if (arr[i].TextureIndex == kInd && arr[i].Pos.X == King.StartingXPos && arr[i].Pos.Y == y)
            {
                arr[i].IsMoved = false;
                return;
            }
        }

        throw new ApplicationException("There is no king on the input or king is placed on wrong position!");
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
                    PlaceFigure(new King(x, y, col) { IsMoved = true });
                    break;
                case 'P':
                    int unMovedPos = col == Figure.ColorT.White ? 1 : 6;
                    PlaceFigure(new Pawn(x, y, col) { IsMoved = !(y == unMovedPos)});
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
// private constants
// ------------------------------

    const int BoardTiles = Board.BoardSize * Board.BoardSize;
    
    // Used to simplify checking possible castling 
    private static readonly BoardPos[] KingPos = { new(4,0 ), new(4, 7) };

    private static readonly BoardPos[][] RookPos = {
        new[] { new BoardPos(7, 0), new BoardPos(0, 0) },
        new[] { new BoardPos(7, 7), new BoardPos(0, 7) }
    };

    private static readonly char[] CastleChar = { 'k', 'q' };

    private static readonly Board.ChessComponents[] RookInd = {
        Board.ChessComponents.WhiteRook,
        Board.ChessComponents.BlackRook
    };

    private static readonly Board.ChessComponents[] KingInd = {
        Board.ChessComponents.WhiteKing,
        Board.ChessComponents.BlackKing
    };
    
// ------------------------------
// public constants
// ------------------------------

    public const int MaxFenLength = 90;
}