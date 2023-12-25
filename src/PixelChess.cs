using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelChess.ChessBackend;
using PixelChess.Figures;
using PixelChess.Ui;
using IDrawable = PixelChess.Ui.IDrawable;

namespace PixelChess;
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
    
        // Core backend elements
        _board = new Board();
        _chessEngineToGameInterface = new UciTranslator();
    
        // UI elements
        _promMenu = new PromotionMenu();
        _timer = new Timer();
        _leftButtons = new ButtonList(
            new Button[,]
            {
                { 
                    new ModeChangeButton(_board, _chessEngineToGameInterface, _gameMode),
                    new ResetButton(_board, _promMenu, _chessEngineToGameInterface),
                    new FenButton(_board),
                    new UndoButton(_board) 
                }
            },
            0, Timer.TimerNameBoardOffset, 35, 120
        );
        
        _uiTargets = new []{ _board, (IDrawable)_promMenu,  _timer, _leftButtons} ;
    }

    public new void Dispose()
    {
        base.Dispose();
        _debugSWriter?.Close();
        _debugFStream?.Close(); 
        Console.SetError(_stdErr);
        Console.SetOut(_stdOut);
        
        _chessEngineToGameInterface.Dispose();
    }

    protected override void Initialize()
    {
        base.Initialize();

        // Load configuration parameters from file
        InitOptions opt = ConfigReader.LoadSettings();
        _applyInitSettings(opt);
        
        Console.WriteLine($"Loaded options:\n{opt}");
        
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

        if (opt.ChessEngineDir != "NONE")
            _chessEngineToGameInterface.Initialize(_board, opt.ChessEngineDir);
        
        // centring other components with respects to others
        _promMenu.Initialize(boardHorOffset);
        _timer.Initialize(boardHorOffset, _board);
        _leftButtons.Initialize(_timer.TimerWhiteX, 2 * (Timer.FontHeight + Timer.TimerNameBoardOffset));
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
                
                _processBoardInteraction(_processClickOnBoard, mState, gameTime);
            }
            _isMouseHold = true;
        }
        else
        {
            if (_isMouseHold)
                _processBoardInteraction(_processDropOnBoard, mState, gameTime);

            _isMouseHold = false;
        }
    
        _board.ProcTimers(gameTime.ElapsedGameTime.TotalMilliseconds);
        if (_gameMode.ActMod == ModeChangeButton.GameMode.ModeT.PlayerVsComputer)
            _processComputersRound(gameTime);
        
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

// -------------------------------------
// Chess board interaction methods
// -------------------------------------

    private bool _processClickOnBoard(MouseState mState)
    {
        bool wasHold = _board.IsHold;
        var res = _board.DropFigure(_board.Translate(mState.X, mState.Y));

        if (res.mType == BoardPos.MoveType.PromotionMove || res.mType == BoardPos.MoveType.PromAndAttack)
            _promMenu.RequestPromotion(_board.PromotionPawn);
            
        if (!wasHold && !res.WasHit)
            _board.SelectFigure(_board.Translate(mState.X, mState.Y));

        return res.WasHit;
    }

    private bool _processDropOnBoard(MouseState mState)
    {
        var ret = _board.DropFigure(_board.Translate(mState.X, mState.Y));
        if (ret.mType == BoardPos.MoveType.PromotionMove || ret.mType == BoardPos.MoveType.PromAndAttack)
            _promMenu.RequestPromotion(_board.PromotionPawn);

        return ret.WasHit;
    }

    private void _processBoardInteraction(Func<MouseState, bool> reactionMethod, MouseState mState, GameTime time)
    {
        if (_board.IsGameEnded) return;
        
        switch (_gameMode.ActMod)
        {
            case ModeChangeButton.GameMode.ModeT.PlayerVsPlayerLocal:
                reactionMethod(mState);
                break;
            case ModeChangeButton.GameMode.ModeT.PlayerVsComputer:
                if (_board.MovingColor == Figure.ColorT.White)
                {
                    var wasHit = reactionMethod(mState);

                    if (wasHit)
                        _chessEngineToGameInterface.StartSearch();
                }
                break;
        }
    }

    private void _processComputersRound(GameTime timeSpent)
    {
        if (_board.MovingColor == Figure.ColorT.White) return;
        
        var result = _chessEngineToGameInterface.ChecksForSearchResult(timeSpent);
        if (result == "err")
            // Faulty engine, rebooting to Player versus player mode.
        {
            _gameMode.ActMod = ModeChangeButton.GameMode.ModeT.PlayerVsPlayerLocal;
            _board.ResetBoard();
        }
        else if (result != String.Empty)
            _board.MakeUciMove(result);
    }
// ------------------------------
// private fields
// ------------------------------

    // Monogame components
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // whole game board backed & drawing
    private readonly Board _board;
    private readonly ModeChangeButton.GameMode _gameMode = new();
    private readonly UciTranslator _chessEngineToGameInterface;

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