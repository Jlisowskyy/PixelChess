using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PongGame;

public abstract class UIObject
{
// --------------------------------
// type construction / setups
// --------------------------------

    public UIObject(string textureName)
    {
        _textureName = textureName;
    }

    public void Initialize(int xOffset, int yOffset, SpriteBatch batch)
    {
        _batch = batch;
        _xOffset = xOffset;
        _yOffset = yOffset;
    }
    
// --------------------------------
// type methods
// --------------------------------

    public void Draw()
    {
        _batch.Draw(_texture, new Vector2(_xOffset, _yOffset), Color.White);
    }

// ------------------------------
// protected & private variables
// ------------------------------

    private readonly string _textureName;
    
    protected SpriteBatch _batch;
    protected Texture2D _texture;
    protected int _xOffset;
    protected int _yOffset;
    
// ------------------------------
// public properties
// ------------------------------

    public string TextureName => _textureName; 
        
    public int XSize => _texture.Width;
    public int YSize => _texture.Height;
    public int XOffset => _xOffset;
    public int YOffset => _yOffset;

    public Texture2D Texture
    {
        set => _texture = value;
    }
}
