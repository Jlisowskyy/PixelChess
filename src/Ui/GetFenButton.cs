using System;
using PixelChess.ChessBackend;

namespace PixelChess.Ui;

public class FenButton : Button
{
    public FenButton(Board board)
    {
        _board = board;
    }
    
    protected sealed override void ClickReaction() => Console.WriteLine(FenTranslator.Translate(_board));

    protected sealed override string[] TextureNames
        => Names;

    private static readonly string[] Names = { "getFen" };
    private readonly Board _board;
}