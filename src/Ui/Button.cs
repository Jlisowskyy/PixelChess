namespace PixelChess.Ui;

public abstract class Button: VaryingUiObject
    // IMPORTANT: Button should not be rotated, only scaling will work
{
    public bool ProcessMouseClick(int x, int y)
    {
        if (x >= _xOffset && x <= _xOffset + Texture.Width * _xScale 
                          && y >= _yOffset && y <= _yOffset + Texture.Height * _yScale)
        {
            ClickReaction();
            return true;
        }

        return false;
    }

    protected abstract void ClickReaction();
}