using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PixelChess.Ui;

public interface IDrawable
{
    public void Draw(SpriteBatch batch);
    public void LoadTextures(ContentManager textureLoader);
}

public abstract class StaticUiObject: IDrawable
    // This type describes simplest drawable object possible. Single texture, eventually modified during execution.
{
// --------------------------------
// type construction / setups
// --------------------------------
    protected virtual string TextureName => "null";

    public void Initialize(int xOffset, int yOffset,  float xScale = 1, float yScale = 1)
    {
        _xOffset = xOffset;
        _yOffset = yOffset;
        _xScale = xScale;
        _yScale = yScale;
    }
    
// --------------------------------
// type methods
// --------------------------------

    public virtual void Draw(SpriteBatch batch) 
        => batch.Draw(Texture, new Vector2(_xOffset, _yOffset), null, Color.White, 0,
            Vector2.Zero, new Vector2(_xScale, _yScale), 0, 0);

    public virtual void LoadTextures(ContentManager textureLoader)
    {
        Texture = textureLoader.Load<Texture2D>(TextureName);
    }

// ------------------------------
// protected & private variables
// ------------------------------

    protected Texture2D Texture;
    protected int _xOffset;
    protected int _yOffset;
    
    protected float _xScale;
    protected float _yScale;
    
    // TODO: add rotations etc
    
// ------------------------------
// public properties
// ------------------------------

    public int XSize => Texture.Width;
    public int YSize => Texture.Height;
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
}
