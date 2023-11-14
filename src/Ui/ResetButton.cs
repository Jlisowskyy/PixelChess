namespace PongGame.Ui;

public class ResetButton : Button
{ 
    public ResetButton(Board board) : base("reset")
    {
        _board = board;
    }
    
    protected override void ClickReaction() => _board.ResetBoard();

    private readonly Board _board;
}