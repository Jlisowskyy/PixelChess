using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PongGame;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    
    private Board _board;

    public enum chessComponents
    {
        Board,
        WhitePawn,
        WhiteKnight,
        WhiteBishop,
        WhiteRook,
        WhiteQueen,
        WhiteKing,
        BlackPawn,
        BlackKnight,
        BlackBishop,
        BlackRook,
        BlackQueen,
        BlackKing,
    }

    private Texture2D[] _componentsTextures = new Texture2D[Enum.GetNames(typeof(chessComponents)).Length];
    
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _board = new Board(Board.basicBeginingLayout);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
        _board.Initialize();

        _graphics.PreferredBackBufferHeight = Board.Height;
        _graphics.PreferredBackBufferWidth = Board.Width;
        _graphics.ApplyChanges();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        foreach (var val in Enum.GetValues<chessComponents>())
        {
            _componentsTextures[(int)val] = Content.Load<Texture2D>(Enum.GetName(val));
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.White);

        _spriteBatch.Begin();
        _spriteBatch.Draw(_componentsTextures[(int)_board.TextureIndex], new Vector2(0, 0), Color.White);

        for (int i = 0; i < _board.FigureList.Length; ++i)
        {
            Figure actFig = _board.FigureList[i];
            
            if (actFig.IsAlive)
            {
                _spriteBatch.Draw(_componentsTextures[(int)actFig.TextureIndex], Board.Translate(actFig.Pos), Color.White);
            }
        }
        
        
        _spriteBatch.End();
        
        base.Draw(gameTime);
    }
}
