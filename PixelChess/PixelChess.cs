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
        _ui = new UI();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();

        const int minHeight = Board.Height;
        const int minWidth = Board.Width + UI.TimerBoardOffset * 4 + UI.TimerXSize * 2;
        
        var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        _graphics.PreferredBackBufferHeight = Math.Max(minHeight, display.Height / 2);
        _graphics.PreferredBackBufferWidth = Math.Max(minWidth, display.Width / 2);

        int boardHorOffset = (_graphics.PreferredBackBufferWidth - Board.Width) / 2;
        int boardVerOffset = (_graphics.PreferredBackBufferHeight - Board.Height) / 2;
        _board.Initialize(boardHorOffset, boardVerOffset);
        _promMenu.Initialize(boardHorOffset, _spriteBatch);
        Board.SpriteBatch = _spriteBatch;
        _board.StartGame(Board.BasicWhiteTime, Board.BasicBlackTIme);
        _ui.Initialize(boardHorOffset, _spriteBatch);
        
        _graphics.ApplyChanges();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        foreach (var val in Enum.GetValues<Board.TileHighlighters>())
        {
            Board.TileHighlightersTextures[(int)val] = Content.Load<Texture2D>(Enum.GetName(val));
        }

        Board.TileHighlightersTextures[(int)BoardPos.MoveType.PromAndAttack] =
            Board.TileHighlightersTextures[(int)BoardPos.MoveType.AttackMove];

        foreach (var val in Enum.GetValues<Board.ChessComponents>())
        {
            Board.ComponentsTextures[(int)val] = Content.Load<Texture2D>(Enum.GetName(val));
        }

        _promMenu.Texture = Content.Load<Texture2D>(_promMenu.TextureName);
        _ui.GameFont = Content.Load<SpriteFont>(_ui.FontName);
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

                if (res.mType == BoardPos.MoveType.PromotionMove || res.mType == BoardPos.MoveType.PromAndAttack)
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
                var ret = _board.DropFigure(_board.Translate(mState.X, mState.Y)).mType;
                if (ret == BoardPos.MoveType.PromotionMove || ret == BoardPos.MoveType.PromAndAttack)
                    _promMenu.RequestPromotion(_board.PromotionPawn);
            }

            _isMouseHold = false;
        }
        
        _board.ProcTimers(gameTime.ElapsedGameTime.TotalMilliseconds);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.White);

        _spriteBatch.Begin();
        
        _board.Draw();
        _promMenu.Draw();
        _ui.Draw(_board.WhiteTime, _board.BlackTime);
        
        _spriteBatch.End();
        
        base.Draw(gameTime);
    }
    
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private readonly PromotionMenu _promMenu;
    private readonly Board _board;
    private readonly UI _ui;
    private bool _isMouseHold = false;
}