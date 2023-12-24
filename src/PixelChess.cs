using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelChess.ChessBackend;
using PixelChess.Ui;

namespace PixelChess
{
    public partial class PixelChess : Game
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
        
            // Actual elements
            _board = new Board();
        
            _promMenu = new PromotionMenu();
            _timer = new Timer();
            _leftButtons = new ButtonList(
                new Button[,] { { new ResetButton(_board, _promMenu), new FenButton(_board), new UndoButton(_board) } },
                0, Timer.TimerNameBoardOffset, 35, 120
            );
        }

        ~PixelChess()
        {
            _debugSWriter?.Close();
            _debugFStream?.Close();
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
            _board.InitializeUiApp(boardHorOffset, boardVerOffset, _spriteBatch);
        
            // centring other components with respects to others
            _promMenu.Initialize(boardHorOffset, _spriteBatch);
            _timer.Initialize(boardHorOffset, _spriteBatch);
            _leftButtons.Initialize(_timer.TimerWhiteX, 2 * (Timer.FontHeight + Timer.TimerNameBoardOffset), _spriteBatch);
            _graphics.ApplyChanges();
        }
    
// ------------------------------
// Content loading
// ------------------------------

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

            _leftButtons.Load(Content);
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
        
            _board.Draw();
            _promMenu.Draw();
            _timer.Draw(_board.WhiteTime, _board.BlackTime);
            _leftButtons.Draw();
        
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
    
        // Mouse state
        private bool _isMouseHold;

        // debugging state
        private bool _debugToFile;
        private FileStream _debugFStream;
        private StreamWriter _debugSWriter;
    }
}