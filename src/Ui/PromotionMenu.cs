using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelChess.ChessBackend;
using PixelChess.Figures;

namespace PixelChess.Ui;

public class PromotionMenu
{
// --------------------------------
// type construction / setups
// --------------------------------

    public void Initialize(int xOffset, SpriteBatch batch)
    {
        _spriteBatch = batch;
        _xOffset = xOffset;
        _drawXStartPos = _xOffset + Board.XTilesBeg + (float)(Board.Width - Width) / 2;

        for (int i = 0; i < FieldsCount; ++i)
        {
            _fields[i].Y = FieldYBeg;
            _fields[i].X = Board.XTilesBeg + _xOffset + FieldXMenuOffset + i * FieldBegDist;
        }
    }
    
// ------------------------------
// type interaction
// ------------------------------

    public void RequestPromotion(Figure promPawn)
    {
        _isOn = true;
        _promotionPawn = promPawn;
    }

    public void ResetRequest()
    {
        _isOn = false;
        _promotionPawn = null;
    }

    public Figure ProcessMouseClick(int x, int y)
    {
        int i;
        for (i = 0; i < FieldsCount; ++i)
        {
            if (x >= _fields[i].X && x <= _fields[i].X + Board.FigureWidth && y >= _fields[i].Y && y <= _fields[i].Y + Board.FigureHeight)
                break;
        }

        if (i == FieldsCount) return null;
        Figure fig;
        
        switch (i)
        {
            case Knight:
                fig = new Knight(_promotionPawn.Pos.X, _promotionPawn.Pos.Y, _promotionPawn.Color);
                break;
            case Bishop:
                fig = new Bishop(_promotionPawn.Pos.X, _promotionPawn.Pos.Y, _promotionPawn.Color);
                break;
            case Rook:
                fig = new Rook(_promotionPawn.Pos.X, _promotionPawn.Pos.Y, _promotionPawn.Color);
                break;
            case Queen:
                fig = new Queen(_promotionPawn.Pos.X, _promotionPawn.Pos.Y, _promotionPawn.Color);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        fig.Parent = _promotionPawn.Parent;
        fig.IsMoved = true;
        _isOn = false;

        return fig;
    }

    public void Draw()
    {
        if (_isOn == false) return;
        
        int TextIndOffset = _promotionPawn.Color == Figure.ColorT.White ? 0 : 4;
        _spriteBatch.Draw(_texture, new Vector2( _drawXStartPos, 0), Color.White);

        for (int i = 0; i < FieldsCount; ++i)
        {
            _spriteBatch.Draw(Board.ComponentsTextures[TextureIndexes[TextIndOffset + i]], _fields[i], Color.White);
        }
    }
    
// ------------------------------
// private variables
// ------------------------------
    
    private SpriteBatch _spriteBatch;
    private int _xOffset; 
    private Figure _promotionPawn;
    private Texture2D _texture;
    private bool _isOn = false;
    private const int FieldsCount = 4;
    private const int Width = 462;
    private const int Height = 444;
    private const int FieldYBeg = 196 + 12;
    private const int FieldXMenuOffset = 100;
    private const int FieldDist = 10;
    private const int FieldBegDist = Board.FigureWidth + FieldDist;
    private const int Knight = 0;
    private const int Bishop = 1;
    private const int Rook = 2;
    private const int Queen = 3;
    private float _drawXStartPos;
    private Vector2[] _fields = new Vector2[FieldsCount];

    private static int[] TextureIndexes = new int[2 * FieldsCount]
    {
        (int) Board.ChessComponents.WhiteKnight,
        (int) Board.ChessComponents.WhiteBishop,
        (int) Board.ChessComponents.WhiteRook,
        (int) Board.ChessComponents.WhiteQueen,
        (int) Board.ChessComponents.BlackKnight,
        (int) Board.ChessComponents.BlackBishop,
        (int) Board.ChessComponents.BlackRook,
        (int) Board.ChessComponents.BlackQueen
    };
    
// ------------------------------
// public properties
// ------------------------------

    public readonly String TextureName = "PromotionMenu";
    public bool IsOn => _isOn;
    public Texture2D Texture
    {
        set => _texture = value;
    }
}