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
        _rButton = new ResetButton();
        _fenButton = new FenButton();
        // _board = new Board("r1bk3r/p2pBpNp/n4n2/1p1NP2P/6P1/3P4/P1P1K3/q5b1 b - - 1");
        
        _promMenu = new PromotionMenu();
        _timer = new Timer();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();

        // calculating minimal possible sizes of window to fit all elements
        const int minHeight = Board.Height;
        const int minWidth = Board.Width + Timer.TimerBoardOffset * 4 + Timer.TimerXSize * 2;
        
        // adapting size to half of the screen with respect to the minimal size
        var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        _graphics.PreferredBackBufferHeight = Math.Max(minHeight, display.Height / 2);
        _graphics.PreferredBackBufferWidth = Math.Max(minWidth, display.Width / 2);

        // centring board position and creating connection with spriteBatch class
        int boardHorOffset = (_graphics.PreferredBackBufferWidth - Board.Width) / 2;
        int boardVerOffset = (_graphics.PreferredBackBufferHeight - Board.Height) / 2;
        _board.InitializeUiApp(boardHorOffset, boardVerOffset, _spriteBatch);
        
        // centring other components with respects to others
        _promMenu.Initialize(boardHorOffset, _spriteBatch);
        _timer.Initialize(boardHorOffset, _spriteBatch);
        _rButton.Initialize(_timer.TimerWhiteX, 2 * (Timer.FontHeight + Timer.TimerNameBoardOffset), _spriteBatch, _board);
        _fenButton.Initialize(_timer.TimerWhiteX, _rButton.YOffset + ResetButton.ySize + Timer.TimerNameBoardOffset, _spriteBatch, _board);
        
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
        
        foreach (var val in Enum.GetValues<Board.EndGameTexts>())
        {
            Board.GameEnds[(int)val] = Content.Load<Texture2D>(Enum.GetName(val));
        }

        Board.TileHighlightersTextures[(int)BoardPos.MoveType.PromAndAttack] =
            Board.TileHighlightersTextures[(int)BoardPos.MoveType.AttackMove];
        
        _promMenu.Texture = Content.Load<Texture2D>(_promMenu.TextureName);
        _timer.GameFont = Content.Load<SpriteFont>(_timer.FontName);
        _rButton.Texture = Content.Load<Texture2D>(_rButton.TextureName);
        _fenButton.Texture = Content.Load<Texture2D>(_fenButton.TextureName);
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        
        var mState = Mouse.GetState();
        
        if (mState.LeftButton == ButtonState.Pressed)
        {
            if (_isMouseHold == false)
            {
                _fenButton.ProcessMouseClick(mState.X, mState.Y);
            
                if (_rButton.ProcessMouseClick(mState.X, mState.Y))
                {
                    _promMenu.ResetRequest();
                    base.Update(gameTime);
                    return;
                }
            
                if (_promMenu.IsOn)
                {
                    var fig = _promMenu.ProcessMouseClick(mState.X, mState.Y);
                    _board.Promote(fig);
            
                    base.Update(gameTime);
                    return;
                }
                
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
        _timer.Draw(_board.WhiteTime, _board.BlackTime);
        _rButton.Draw();
        _fenButton.Draw();
        
        _spriteBatch.End();
        
        base.Draw(gameTime);
    }
    
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private readonly PromotionMenu _promMenu;
    private readonly Board _board;
    private readonly Timer _timer;
    private readonly ResetButton _rButton;
    private readonly FenButton _fenButton;
    private bool _isMouseHold = false;
}