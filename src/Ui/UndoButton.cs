using PixelChess.ChessBackend;

namespace PixelChess.Ui;

public class UndoButton : Button
{
    public UndoButton(Board board)
    {
        _board = board;
    }

    protected sealed override string[] TextureNames
        => Names;

    private static readonly string[] Names = { "undo" };

    protected sealed override void ClickReaction() => _board.UndoMove();
    
    private readonly Board _board;
}