using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PongGame;

public class PixelChess : Game
{ 
    public PixelChess()
    {
        _graphics = new GraphicsDeviceManager(this);
        _board = new Board(Board.BasicBeginningLayout);
        _promMenu = new PromotionMenu();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
        
        var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        _graphics.PreferredBackBufferHeight = Math.Max(Board.Height, display.Height / 2);
        _graphics.PreferredBackBufferWidth = Math.Max(Board.Width, display.Width / 2);

        int boardHorOffset = (_graphics.PreferredBackBufferWidth - Board.Width) / 2;
        int boardVerOffset = (_graphics.PreferredBackBufferHeight - Board.Height) / 2;
        _board.Initialize(boardHorOffset, boardVerOffset);
        _promMenu.Initialize(boardHorOffset, _spriteBatch);
        Board.SpriteBatch = _spriteBatch;
        
        _graphics.ApplyChanges();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        foreach (var val in Enum.GetValues<Board.TileHighlighters>())
        {
            Board.TileHighlightersTextures[(int)val] = Content.Load<Texture2D>(Enum.GetName(val));
        }

        foreach (var val in Enum.GetValues<Board.ChessComponents>())
        {
            Board.ComponentsTextures[(int)val] = Content.Load<Texture2D>(Enum.GetName(val));
        }

        _promMenu.Texture = Content.Load<Texture2D>(_promMenu.TextureName);
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        
        var mState = Mouse.GetState();
        
        
        if (mState.LeftButton == ButtonState.Pressed)
        {
            if (_promMenu.IsOn)
            {
                var fig = _promMenu.ProcessMouseClick(mState.X, mState.Y);
                _board.Promote(fig);
            
                base.Update(gameTime);
                return;
            }
            
            if (_isMouseHold == false)
            {
                bool wasHold = _board.IsHold;
                var res = _board.DropFigure(_board.Translate(mState.X, mState.Y));

                if (res.mType == BoardPos.MoveType.PromotionMove)
                    _promMenu.RequestPromotion(_board.PromotionPawn);
                
                if (!wasHold && !res.WasHit)
                    _board.SelectFigure(_board.Translate(mState.X, mState.Y));
            }
            _isMouseHold = true;
        }
        else
        {
            if (_isMouseHold == true)
            {
                var pos = _board.Translate(mState.X, mState.Y);
                
                if (_board.DropFigure(pos).mType == BoardPos.MoveType.PromotionMove)
                    _promMenu.RequestPromotion(_board.PromotionPawn);
            }

            _isMouseHold = false;
        }
        
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.White);

        _spriteBatch.Begin();
        
        _board.Draw();
        _promMenu.Draw();
        
        _spriteBatch.End();
        
        base.Draw(gameTime);
    }
    
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private readonly PromotionMenu _promMenu;
    private readonly Board _board;
    private bool _isMouseHold = false;
}