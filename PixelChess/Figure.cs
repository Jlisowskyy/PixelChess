using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PongGame;

public abstract class Figure
{
    public abstract Texture2D getText();
}

public abstract class BlackPawn : Figure
{
    static BlackPawn()
    {
        // _texture = Content
    }
    
    
    
    private static readonly Texture2D _texture;
}