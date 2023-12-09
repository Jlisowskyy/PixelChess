#define DEBUG_
using System;
using PixelChess.Figures;

namespace PixelChess.ChessBackend;

public abstract class UciTranslator
{
    public static string GetUciMoveCode(BoardPos prevPos, BoardPos nextPos, Figure updatedFigure = null)
    {
        string fPos = $"{(char)('a' + prevPos.X)}{1 + prevPos.Y}";
        string nPos = $"{(char)('a' + nextPos.X)}{1 + nextPos.Y}";

        if ((nextPos.MoveT & BoardPos.MoveType.PromotionMove) != 0)
        {
#if DEBUG_
            if (updatedFigure == null)
                throw new ApplicationException("Not able to correctly translate move to UCI, updateFigure not passed!");
#endif

            return fPos + nPos + updatedFigure switch
            {
                Rook => 'r',
                Knight => 'n',
                Bishop => 'b',
                Queen => 'q',
#if DEBUG_
                _ =>
                    throw new ApplicationException("Passed figure is not valid update figure!")
#endif
            };
        }
        else return fPos + nPos;
    }

    public static (BoardPos fPos, BoardPos nPos, char prom) FromUciToInGame(string uciCode)
        // Does not perform any sanity checks, before use make sure that input is correct
        // There is assumption that if no promotion char was on position 4, 'x' is an output of prom field
    {
        BoardPos fPos = new BoardPos(uciCode[0] - 'a', uciCode[1] - '1');
        BoardPos nPos = new BoardPos(uciCode[2] - 'a', uciCode[3] - '1');
        char prom = uciCode.Length == 5 ? uciCode[4] : 'x';

        return (fPos, nPos, prom);
    }

    public static Figure GetUpdateType(char promChar, Figure promPawn, Board bd)
        => promChar switch
        {
            'r' => new Rook(promPawn.Pos.X, promPawn.Pos.Y, promPawn.Color) { Parent = bd },
            'b' => new Bishop(promPawn.Pos.X, promPawn.Pos.Y, promPawn.Color) { Parent = bd },
            'n' => new Knight(promPawn.Pos.X, promPawn.Pos.Y, promPawn.Color) { Parent = bd },
            'q' => new Queen(promPawn.Pos.X, promPawn.Pos.Y, promPawn.Color) { Parent = bd },
#if DEBUG_
            _ => throw new ApplicationException("Passed character is not recognizable as promotion character!!!"),
#endif
        };
}