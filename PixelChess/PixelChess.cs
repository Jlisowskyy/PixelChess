using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PongGame;

public class PixelChess : Game
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
    
    public PixelChess()
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
        bool isMouseHold = false;
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // var mstate = Mouse.GetState();
        //
        // if (mstate.LeftButton == ButtonState.Pressed)
        // {
        //     if (isMoseHold) return;
        //
        //     isMoseHold = true;
        //     var tilePos = Board.Translate(mstate.X, mstate.Y);
        //
        //     if (tilePos.isOnBoard())
        //     {
        //         _board.SelectFigure(tilePos);
        //     }else
        //         _board.UnselectFigure();
        //     
        //     // Draw(gameTime);
        // }
        // else
        // {
        //     if (isMoseHold)
        //     {
        //         _board.DropFigure(Board.Translate(mstate.X, mstate.Y));
        //         isMoseHold = false;
        //     }
        // }

        var mState = Mouse.GetState();

        if (mState.LeftButton == ButtonState.Pressed)
        {
            if (isMouseHold == false)
            {
                if (BoardPos.isOnBoard(mState.X, mState.Y))
                {
                    _board.SelectFigure(Board.Translate(mState.X, mState.Y));
                }
            }

            isMouseHold = true;
        }
        else
        {
            if (isMouseHold == true)
            {
                _board.DropFigure(Board.Translate(mState.X, mState.Y));
            }
            
            isMouseHold = false;
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
            
            _spriteBatch.Draw(_tileHighlightersTextures[(int)TileHighliters.SelectedTile], Board.Translate(_board.SelFigPos), Color.White);
            
            for (int i = 0; i < movs.moveCont; ++i)
            {
                _spriteBatch.Draw(_tileHighlightersTextures[(int)movs.moves[i].MoveT], Board.Translate(movs.moves[i]), Color.White);
            }

            if (_board.IsHold)
            {
                var mState = Mouse.GetState();
                
                _spriteBatch.Draw(_componentsTextures[(int)_board.SelFigTextIndex],
                    _board.CenterFigurePosOnMouse(mState.X, mState.Y), Color.White);
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
