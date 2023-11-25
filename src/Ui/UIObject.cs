using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelChess.Ui;

public abstract class UiObject
{
// --------------------------------
// type construction / setups
// --------------------------------

    protected UiObject(string textureName) => _textureName = textureName;
    
    public void Initialize(int xOffset, int yOffset, SpriteBatch batch, float xScale = 1, float yScale = 1)
    {
        _batch = batch;
        _xOffset = xOffset;
        _yOffset = yOffset;
        _xScale = xScale;
        _yScale = yScale;
    }
    
// --------------------------------
// type methods
// --------------------------------

    public void Draw() 
        => _batch.Draw(_texture, new Vector2(_xOffset, _yOffset), null, Color.White, 0, Vector2.Zero, new Vector2(_xScale, _yScale), 0, 0);

// ------------------------------
// protected & private variables
// ------------------------------

    private readonly string _textureName;

    private SpriteBatch _batch;
    protected Texture2D _texture;
    protected int _xOffset;
    protected int _yOffset;
    
    protected float _xScale;
    protected float _yScale;
    
    // TODO: add rotations etc
    
// ------------------------------
// public properties
// ------------------------------

    public string TextureName => _textureName; 
        
    public int XSize => _texture.Width;
    public int YSize => _texture.Height;
    public int XOffset => _xOffset;
    public int YOffset => _yOffset;

    public (float xScale, float yScale) Scale
    {
        set
        {
            _xScale = value.xScale;
            _yScale = value.yScale;
        }
    }

    public Texture2D Texture
    {
        set => _texture = value;
    }
}
