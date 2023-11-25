using PixelChess.ChessBackend;

namespace PixelChess.Ui;

public class UndoButton : Button
{
    public UndoButton(Board board) : base("undo")
    {
        _board = board;
    }

    protected override void ClickReaction() => _board.UndoMove();
    
    private readonly Board _board;
}