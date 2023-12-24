using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PixelChess.ChessBackend;

namespace PixelChess.Ui;

public class Timer : IDrawable
{
// --------------------------------
// type construction / setups
// --------------------------------

    public void Initialize(int horOffset, Board board)
    {
        _board = board;
        _horOffset = horOffset;

        _timerWhiteX = _horOffset - TimerBoardOffset - TimerXSize;
        _timerBlackX = _horOffset + Board.Width + TimerBoardOffset;
    }
    
// ------------------------------
// type interaction
// ------------------------------

    public void Draw(SpriteBatch batch)
    {
        string wString = "White:\n";
        string bString = "Black:\n";
        
        batch.DrawString(_gameFont, wString, new Vector2(_timerWhiteX, TimerNameBoardOffset), Color.Black);
        batch.DrawString(_gameFont, bString, new Vector2(_timerBlackX, TimerNameBoardOffset), Color.Black);
        
        var tWhite = TranslateMs(_board.WhiteTime);
        var tBlack = TranslateMs(_board.BlackTime);
        
        batch.DrawString(_gameFont, tWhite, new Vector2(_timerWhiteX, TimerBoardOffset), Color.Black);
        batch.DrawString(_gameFont, tBlack, new Vector2(_timerBlackX, TimerBoardOffset), Color.Black);
    }

    public void LoadTextures(ContentManager manager)
    {
        _gameFont = manager.Load<SpriteFont>(FontName);
    }
    
// ------------------------------
// private methods
// ------------------------------

    private string TranslateMs(double time)
    {
        int minutes = (int)(time / Board.Minute);
        int secs = (int)(time % Board.Minute / Board.Second);
        
        return $"{minutes:D2}:{secs:D2}";
    }

// ------------------------------
// private variables
// ------------------------------

    private Board _board;
    private SpriteFont _gameFont;
    private int _horOffset;
    private int _timerWhiteX;
    private int _timerBlackX;
    
// ------------------------------
// public properties
// ------------------------------

    public const int TimerNameBoardOffset = 40;
    public const int TimerXSize = 120;
    public const int FontHeight = 50;
    public const int TimerBoardOffset = FontHeight + TimerNameBoardOffset;
    private const string FontName = "GameFont";

    public int TimerWhiteX => _timerWhiteX;
}