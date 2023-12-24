using PixelChess.ChessBackend;

namespace PixelChess.Ui;

public class ModeChangeButton : Button
{
    public ModeChangeButton(Board board, UciTranslator translationUnit)
    {
        _board = board;
        _uciUnit = translationUnit;
    }

    protected sealed override void ClickReaction()
    {
        stateCounter = (stateCounter + 1) % Names.Length;
        SetActiveTexture(stateCounter);
    }

    protected sealed override string[] TextureNames
        => Names;

    private static readonly string[] Names = { "player", "computer" };
    private int stateCounter;

    private Board _board;
    private UciTranslator _uciUnit;
}