using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelChess.ChessBackend;

namespace PixelChess.Ui;

public class Timer
{
// --------------------------------
// type construction / setups
// --------------------------------

    public void Initialize(int horOffset, SpriteBatch batch)
    {
        _spriteBatch = batch;
        _horOffset = horOffset;

        _timerWhiteX = _horOffset - TimerBoardOffset - TimerXSize;
        _timerBlackX = _horOffset + Board.Width + TimerBoardOffset;
    }
    
// ------------------------------
// type interaction
// ------------------------------

    public void Draw(double whiteTime, double blackTime)
    {
        string wString = "White:\n";
        string bString = "Black:\n";
        
        _spriteBatch.DrawString(_gameFont, wString, new Vector2(_timerWhiteX, TimerNameBoardOffset), Color.Black);
        _spriteBatch.DrawString(_gameFont, bString, new Vector2(_timerBlackX, TimerNameBoardOffset), Color.Black);
        
        var tWhite = TranslateMs(whiteTime);
        var tBlack = TranslateMs(blackTime);
        
        _spriteBatch.DrawString(_gameFont, tWhite, new Vector2(_timerWhiteX, TimerBoardOffset), Color.Black);
        _spriteBatch.DrawString(_gameFont, tBlack, new Vector2(_timerBlackX, TimerBoardOffset), Color.Black);
    }
    
// ------------------------------
// private methods
// ------------------------------

    private string TranslateMs(double time)
    {
        int mins = (int)(time / Board.Minute);
        int secs = (int)(time % Board.Minute / Board.Second);
        
        return $"{mins:D2}:{secs:D2}";
    }

// ------------------------------
// private variables
// ------------------------------

    private SpriteFont _gameFont;
    private SpriteBatch _spriteBatch;
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
    public readonly string FontName = "GameFont";

    public int TimerWhiteX => _timerWhiteX;

    public SpriteFont GameFont
    {
        set => _gameFont = value;
    }
}