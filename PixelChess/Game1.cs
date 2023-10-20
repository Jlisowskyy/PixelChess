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

    private static readonly string[] ComponentsNames;
    private Texture2D[] _componentsTextures = new Texture2D[ComponentsNames.Length];
    
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _board = new Board();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    static Game1()
    {
        ComponentsNames = new[] {
            "board1",
            "pawn_white",
            "knight_white",
            "bishop_white",
            "rook_white",
            "queen_white",
            "king_white",
            "pawn_black",
            "knight_black",
            "bishop_black",
            "rook_black",
            "queen_black",
            "king_black",
        };
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
            // _componentsTextures[val] = Content.Load<Texture2D>(ComponentsNames[val]);
            System.Console.WriteLine(Enum.GetName(val));
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
        // _spriteBatch.Draw(_pawnWhite, new Vector2(0,0), Color.White);
        _spriteBatch.End();
        
        base.Draw(gameTime);
    }
}
