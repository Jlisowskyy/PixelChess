namespace PongGame.Ui;

public class ResetButton : Button
{ 
    public ResetButton(Board board, PromotionMenu promMenu) : base("reset")
    {
        _board = board;
        _promMenu = promMenu;
    }

    protected override void ClickReaction()
    {
        _board.ResetBoard();
        _promMenu.ResetRequest();
    }

    private readonly Board _board;
    private readonly PromotionMenu _promMenu;
}