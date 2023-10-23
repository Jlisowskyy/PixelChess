using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PongGame;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    
    private Board _board;

    public enum chessComponents
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

    public enum TileHighliters
    {
        MoveTile,
        BasicAttackTile,
        KingAttackTile,
        SelectedTile,
    }

    private Texture2D[] _componentsTextures = new Texture2D[Enum.GetNames(typeof(chessComponents)).Length];
    private Texture2D[] _tileHighlightersTextures = new Texture2D[Enum.GetNames(typeof(TileHighliters)).Length];
    
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _board = new Board(Board.BasicBeginingLayout);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
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
        
        foreach (var val in Enum.GetValues<TileHighliters>())
        {
            _tileHighlightersTextures[(int)val] = Content.Load<Texture2D>(Enum.GetName(val));
        }

        foreach (var val in Enum.GetValues<chessComponents>())
        {
            _componentsTextures[(int)val] = Content.Load<Texture2D>(Enum.GetName(val));
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        var mstate = Mouse.GetState();

        if (mstate.LeftButton == ButtonState.Pressed)
        {
            var tilePos = Board.Translate(mstate.X, mstate.Y);

            if (BoardPos.isOnBoard(tilePos.X, tilePos.Y))
                _board.SelectFigure(tilePos);
            else
                _board.UnselectFigure();
            
            // Draw(gameTime);
        }
        
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.White);

        _spriteBatch.Begin();
        _spriteBatch.Draw(_componentsTextures[(int)_board.TextureIndex], new Vector2(0, 0), Color.White);

        if (_board.IsSelectedFigure())
        {
            var movs = _board.GetSelFigMoves();
            
            _spriteBatch.Draw(_tileHighlightersTextures[(int)TileHighliters.SelectedTile], Board.Translate(_board.GetSelectedFigurePos()), Color.White);
            
            for (int i = 0; i < movs.moveCont; ++i)
            {
                _spriteBatch.Draw(_tileHighlightersTextures[(int)movs.moves[i].MoveT], Board.Translate(movs.moves[i]), Color.White);
            }
        }

        for (int i = 0; i < _board.FigureList.Length; ++i)
        {
            Figure actFig = _board.FigureList[i];
            
            if (actFig.IsAlive)
            {
                _spriteBatch.Draw(_componentsTextures[(int)actFig.TextureIndex], Board.Translate(actFig.Pos), Color.White);
            }
        }
        
        
        _spriteBatch.End();
        
        base.Draw(gameTime);
    }
}
