namespace PongGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class PromotionMenu
{
    public PromotionMenu(SpriteBatch batch, int width, int height)
    {
        _spriteBatch = batch;
        _width = width;
        _height = height;
    }

    public void RequestPromotion(Figure promPawn)
    {
        _isOn = true;
        _promotionPawn = promPawn;
    }

    public Figure ProcessMouseClick(int x, int y)
    {


        return null;
    }
    
    
    private readonly SpriteBatch _spriteBatch;
    private readonly int _width;
    private readonly int _height;
    private Figure _promotionPawn;
    
    private bool _isOn = false;
    public bool IsOn => _isOn;
}