using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PixelChess.Ui;

public abstract class VaryingUiObject : StaticUiObject
    // This object describes slightly more complex object than parent class, but still is quite primitive.
    // Introduces new possibility to hold more than single texture inside of it.
{
    protected abstract string[] TextureNames { get; }
    public override void LoadTextures(ContentManager textureLoader)
        // Loads textures from selected names array and setups active one on textures under first index.
    {
        _textures = new Texture2D[TextureNames.Length];

        for (int i = 0; i < TextureNames.Length; ++i)
        {
            _textures[i] = textureLoader.Load<Texture2D>(TextureNames[i]);
        }

        Texture = _textures[0];
    }

    protected void SetActiveTexture(int textureIndex)
        => Texture = _textures[textureIndex];
    
    private Texture2D[] _textures;
}