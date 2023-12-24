using PixelChess.ChessBackend;

namespace PixelChess.Ui;

public class ResetButton : Button
{ 
    public ResetButton(Board board, PromotionMenu promMenu)
    {
        _board = board;
        _promMenu = promMenu;
    }

    protected sealed override void ClickReaction()
    {
        _board.ResetBoard();
        _promMenu.ResetRequest();
    }

    protected sealed override string[] TextureNames
        => Names;

    private static readonly string[] Names = { "reset" };

    private readonly Board _board;
    private readonly PromotionMenu _promMenu;
}