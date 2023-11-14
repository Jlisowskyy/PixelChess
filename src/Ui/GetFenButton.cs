using System;

namespace PongGame.Ui;

public class FenButton : Button
{
    public FenButton(Board board) : base("getFen")
    {
        _board = board;
    }
    
    protected override void ClickReaction() => Console.WriteLine(FenTranslator.Translate(_board));

    private readonly Board _board;
}