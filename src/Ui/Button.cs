namespace PongGame.Ui;

public abstract class Button: UiObject
    // IMPORTANT: Button should not be rotated, only scaling will work
{
    protected Button(string textureName) : base(textureName) {}
    
    public bool ProcessMouseClick(int x, int y)
    {
        if (x >= _xOffset && x <= _xOffset + _texture.Width * _xScale && y >= _yOffset && y <= _yOffset + _texture.Height * _yScale)
        {
            ClickReaction();
            return true;
        }

        return false;
    }

    protected abstract void ClickReaction();
}