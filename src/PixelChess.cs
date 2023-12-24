using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelChess.ChessBackend;
using PixelChess.Ui;
using IDrawable = PixelChess.Ui.IDrawable;

namespace PixelChess
{
    public partial class PixelChess : Game, IDisposable
    { 
    
// ------------------------------------
// type creation / initialization
// ------------------------------------

        public PixelChess()
        {
            // Monogame components
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            
            // Saving old console streams
            _stdErr = Console.Error;
            _stdOut = Console.Out;
        
            // Actual elements
            _board = new Board();
        
            _promMenu = new PromotionMenu();
            _timer = new Timer();
            _leftButtons = new ButtonList(
                new Button[,]
                {
                    { 
                        new ModeChangeButton(_board, null),
                        new ResetButton(_board, _promMenu),
                        new FenButton(_board),
                        new UndoButton(_board) 
                    }
                },
                0, Timer.TimerNameBoardOffset, 35, 120
            );
            
            _uiTargets = new []{ (IDrawable)_promMenu, _board, _timer, _leftButtons} ;
        }

        public new void Dispose()
        {
            base.Dispose();
            _debugSWriter?.Close();
            _debugFStream?.Close(); 
            Console.SetError(_stdErr);
            Console.SetOut(_stdOut);
        }

        protected override void Initialize()
        {
            base.Initialize();

            // Load configuration parameters from file
            InitOptions opt = ConfigReader.LoadSettings();
            _applyInitSettings(opt);
            
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
            _board.InitializeUiApp(boardHorOffset, boardVerOffset);
        
            // centring other components with respects to others
            _promMenu.Initialize(boardHorOffset, _spriteBatch);
            _timer.Initialize(boardHorOffset, _board);
            _leftButtons.Initialize(_timer.TimerWhiteX, 2 * (Timer.FontHeight + Timer.TimerNameBoardOffset), _spriteBatch);
            _graphics.ApplyChanges();
        }
    
// ------------------------------
// Content loading
// ------------------------------

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            foreach (var uiTarget in _uiTargets)
                uiTarget.LoadTextures(Content);
        }
    
// ------------------------------
// Game state updating
// ------------------------------

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
        
            var mState = Mouse.GetState();
        
            if (mState.LeftButton == ButtonState.Pressed)
            {
                if (_isMouseHold == false)
                {
                    if (_leftButtons.ProcessMouseClick(mState.X, mState.Y))
                    {
                        base.Update(gameTime);
                        _isMouseHold = true;
                        return;
                    }

                    if (_promMenu.IsOn)
                    {
                        var fig = _promMenu.ProcessMouseClick(mState.X, mState.Y);
                        _board.Promote(fig);
            
                        base.Update(gameTime);
                        _isMouseHold = true;
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
                if (_isMouseHold)
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

            foreach (var uiTarget in _uiTargets)
                uiTarget.Draw(_spriteBatch);
        
            _spriteBatch.End();
        
            base.Draw(gameTime);
        }
    
// ------------------------------
// private fields
// ------------------------------

        // Monogame components
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // whole game backed & drawing - consider segmentation
        private readonly Board _board;
    
        // UI elements
        private readonly PromotionMenu _promMenu;
        private readonly Timer _timer;
        private readonly ButtonList _leftButtons;
    
        // Aggregated UI components
        private readonly IDrawable[] _uiTargets;
        
        // Mouse state
        private bool _isMouseHold;

        // debugging state
        private bool _debugToFile;
        private FileStream _debugFStream;
        private StreamWriter _debugSWriter;
        private readonly TextWriter _stdOut;
        private readonly TextWriter _stdErr;
    }
}