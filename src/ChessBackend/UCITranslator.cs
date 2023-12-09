#define DEBUG_
using System;
using System.Diagnostics;
using PixelChess.Figures;

namespace PixelChess.ChessBackend;

public class UCITranslator
{
    public static string GetUCIMoveCode(BoardPos prevPos, BoardPos nextPos, Figure updatedFigure = null)
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

    public static (BoardPos fPos, BoardPos nPos, char prom) FromUCIToInGameWout(string UCICode)
    {
        throw new NotImplementedException();
    }
    
}