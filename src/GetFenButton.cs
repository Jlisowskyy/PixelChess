using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PongGame;

public class FenButton
{
// --------------------------------
// type construction / setups
// --------------------------------

    public void Initialize(int xOffset, int yOffset, SpriteBatch batch, Board board)
    {
        _batch = batch;
        _board = board;
        _xOffset = xOffset;
        _yOffset = yOffset;
    }

    public void ProcessMouseClick(int x, int y)
    {
        if (x >= _xOffset && x <= _xOffset + xSize && y >= _yOffset && y <= _yOffset + ySize)
            Console.WriteLine(FenTranslator.Translate(_board));
    }

    public void Draw()
    {
        _batch.Draw(_texture, new Vector2( _xOffset, _yOffset), Color.White);
    }
    
// ------------------------------
// private variables
// ------------------------------

    private Texture2D _texture;
    private SpriteBatch _batch;
    private int _yOffset;
    private int _xOffset;
    private Board _board;

// ------------------------------
// public properties
// ------------------------------

    public const int xSize = 120;
    public const int ySize = 35;

    public readonly String TextureName = "getFen";

    public Texture2D Texture
    {
        set => _texture = value;
    }
}