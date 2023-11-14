namespace PongGame.Ui;

public abstract class Button: UIObject
{
    protected Button(string textureName) : base(textureName) {}
    
    public bool ProcessMouseClick(int x, int y)
    {
        if (x >= _xOffset && x <= _xOffset + _texture.Width && y >= _yOffset && y <= _yOffset + _texture.Height)
        {
            ClickReaction();
            return true;
        }

        return false;
    }

    protected abstract void ClickReaction();
}