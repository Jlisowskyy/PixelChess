using System;
using PixelChess.ChessBackend;
using PixelChess.Figures;

namespace PixelChess.Additional;

public class IntegerNotationTranslator
{
    public static ulong GetFullMap(Figure[,] inGameMap)
    {
        ulong map = 0;
        foreach (var fig in inGameMap)
        {
            if (fig == null) continue;
            
            map |= TranslatePosToMap(fig.Pos);
        }

        return map;
    }

    public static ulong GetDesiredFigMap(Figure[,] inGameMap, Board.ChessComponents figType)
    {
        ulong map = 0;
        foreach (var fig in inGameMap)
            if (fig != null && fig.TextureIndex == figType) map |= TranslatePosToMap(fig.Pos);

        return map;
    }

    public static int TranslatePos(BoardPos pos)
        => pos.X + pos.Y * 8;

    public static ulong TranslatePosToMap(BoardPos pos)
        => 1UL << TranslatePos(pos);
}