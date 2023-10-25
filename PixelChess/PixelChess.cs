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
        _board = new Board(Board.BasicBeginingLayout);
        _promMenu = new PromotionMenu(_spriteBatch, Board.Width, Board.Height);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        
        _componentsTextures = new Texture2D[Enum.GetNames(typeof(ChessComponents)).Length];
        _tileHighlightersTextures = new Texture2D[Enum.GetNames(typeof(TileHighlighters)).Length];
    }

    protected override void Initialize()
    {
        base.Initialize();
        _board.Initialize();

        _graphics.PreferredBackBufferHeight = Board.Height;
        _graphics.PreferredBackBufferWidth = Board.Width;
        _graphics.ApplyChanges();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        foreach (var val in Enum.GetValues<TileHighlighters>())
        {
            _tileHighlightersTextures[(int)val] = Content.Load<Texture2D>(Enum.GetName(val));
        }

        foreach (var val in Enum.GetValues<ChessComponents>())
        {
            _componentsTextures[(int)val] = Content.Load<Texture2D>(Enum.GetName(val));
        }

        _promMenu.Texture = Content.Load<Texture2D>(_promMenu.TextureName);
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        
        var mState = Mouse.GetState();

        if (_promMenu.IsOn)
        {
            var fig = _promMenu.ProcessMouseClick(mState.X, mState.Y);
            _board.Promote(fig);
            
            base.Update(gameTime);
            return;
        }
        
        if (mState.LeftButton == ButtonState.Pressed)
        {
            if (_isMouseHold == false)
            {
                _board.SelectFigure(Board.Translate(mState.X, mState.Y));
            }

            _isMouseHold = true;
        }
        else
        {
            if (_isMouseHold == true)
            {
                var pos = Board.Translate(mState.X, mState.Y);
                
                if (_board.DropFigure(pos) == BoardPos.MoveType.PromotionMove)
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

        _drawBoard();
        _drawHighlightedTiles();
        _drawStaticFigures();
        _drawHoveringFigure();
        _promMenu.Draw();
        
        _spriteBatch.End();
        
        base.Draw(gameTime);
    }

    private void _drawBoard()
    {
        _spriteBatch.Draw(_componentsTextures[(int)_board.TextureIndex], new Vector2(0, 0), Color.White);
    }

    private void _drawHighlightedTiles()
    {
        if (_board.IsSelectedFigure())
        {
            var movs = _board.GetSelFigMoves();
            
            _spriteBatch.Draw(_tileHighlightersTextures[(int)TileHighlighters.SelectedTile], Board.Translate(_board.SelFigPos), Color.White);
            
            for (int i = 0; i < movs.moveCont; ++i)
            {
                _spriteBatch.Draw(_tileHighlightersTextures[(int)movs.moves[i].MoveT], Board.Translate(movs.moves[i]), Color.White);
            }
        }
    }

    private void _drawStaticFigures()
    {
        for (int i = 0; i < _board.FigureList.Length; ++i)
        {
            Figure actFig = _board.FigureList[i];
            
            if (actFig.IsAlive)
            {
                _spriteBatch.Draw(_componentsTextures[(int)actFig.TextureIndex], Board.Translate(actFig.Pos), Color.White);
            }
        }
    }

    private void _drawHoveringFigure()
    {
        if (_board.IsHold)
        {
            var mState = Mouse.GetState();
                
            _spriteBatch.Draw(_componentsTextures[(int)_board.SelFigTextIndex],
                _board.CenterFigurePosOnMouse(mState.X, mState.Y), Color.White);
        }
    }
    
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private readonly PromotionMenu _promMenu;
    private readonly Board _board;
    private bool _isMouseHold = false;

    public enum ChessComponents
    {
        Board,
        WhitePawn,
        WhiteKnight,
        WhiteBishop,
        WhiteRook,
        WhiteQueen,
        WhiteKing,
        BlackPawn,
        BlackKnight,
        BlackBishop,
        BlackRook,
        BlackQueen,
        BlackKing,
    }

    private enum TileHighlighters
    {
        MoveTile,
        BasicAttackTile,
        PromotionTile,
        KingAttackTile,
        CastlingMove,
        SelectedTile,
    }

    private readonly Texture2D[] _componentsTextures;
    private readonly Texture2D[] _tileHighlightersTextures;
}