using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PixelChess.Ui;

// TODO: add here button list to creat cycling menus or just static on ui buttons

public class ButtonList : IDrawable
{
// ------------------------------
// type creation
// ------------------------------
    
    // buttonArray can't contain any nulls, every button field has to be filled, otherwise exception is called
    public ButtonList(Button[,] buttonArray, int horButtonDist, int verButtonDist, int buttonHeight, int buttonWidth)
    {
        _horButtonCount = buttonArray.GetLength(0);
        _verButtonCount = buttonArray.GetLength(1);

        _buttonHeight = buttonHeight;
        _buttonWidth = buttonWidth;

        _buttonArray = new Button[_horButtonCount * _verButtonCount];
        _extractButtonArray(buttonArray);

        _listHeight = _verButtonCount * buttonHeight + (_verButtonCount - 1) * verButtonDist;
        _listWidth = _horButtonCount * buttonWidth + (_horButtonCount - 1) * horButtonDist;

        _xButtonOffset = buttonWidth + horButtonDist;
        _yButtonOffset = buttonHeight + verButtonDist;
    }

    public void Initialize(int xOffset, int yOffset, SpriteBatch batch)
    {
        _xOffset = xOffset;
        _yOffset = yOffset;
        
        for (int x = 0; x < _horButtonCount; ++x)
        {
            for (int y = 0; y < _verButtonCount; ++y)
            {
                (int xO, int yO) = _getOffsets(x, y);
                
                var button = _buttonArray[_getIndex(x, y)];
                button.Initialize(xO, yO, (float)_buttonWidth / button.XSize, (float)_buttonHeight / button.YSize);
            }
        }
    }

    public void LoadTextures(ContentManager manager)
    {
        foreach (var button in _buttonArray)
        {
            button.LoadTextures(manager);
        }
    }
    
// ------------------------------
// type interaction
// ------------------------------

    public bool ProcessMouseClick(int x, int y)
    {
        // ugly but simple
        foreach (var button in _buttonArray)
            if (button.ProcessMouseClick(x, y)) return true;

        return false;
    }

    public void Draw(SpriteBatch batch)
    {
        foreach (var button in _buttonArray)
            button.Draw(batch);
    }

// ------------------------------
// private methods
// ------------------------------

    private void _extractButtonArray(Button[,] arr)
    {
        for (int x = 0; x < _horButtonCount; ++x)
        {
            for (int y = 0; y < _verButtonCount; ++y)
            {
                if (arr[x, y] == null)
                    throw new ApplicationException(
                        "[ERROR] Button list expects all button fields to be filed inside array");

                _buttonArray[_getIndex(x, y)] = arr[x, y];
            }
        }
    }

    private int _getIndex(int x, int y) => y * _horButtonCount + x;
    private (int, int) _getOffsets(int x, int y) => (_xOffset + x * _xButtonOffset, _yOffset + y * _yButtonOffset);
    

// ------------------------------
// private fields
// ------------------------------

    
    // Input information
    private readonly int _buttonHeight;
    private readonly int _buttonWidth;
    private int _xOffset;
    private int _yOffset;
    
    // Extracted values
    private readonly int _horButtonCount;
    private readonly int _verButtonCount;
    private readonly int _xButtonOffset;
    private readonly int _yButtonOffset;
    private readonly int _listHeight;
    private readonly int _listWidth;
    
    // Stored Buttons
    private readonly Button[] _buttonArray;
    
// ------------------------------
// public properties
// ------------------------------

    // Used to initialize textures
    public Button[] ButtonArray => _buttonArray;
    
    // Sizes and parameters
    public int ListHeight => _listHeight;
    public int ListWidth => _listWidth;
    public int XOffset => _xOffset;
    public int YOffset => _yOffset;
}