using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelChess.Figures;

namespace PixelChess.ChessBackend;

public partial class Board
{
// ------------------------------
// loading methods
// ------------------------------

    public void LoadTextures(ContentManager textureLoader)
    {
        foreach (var val in Enum.GetValues<TileHighlighters>())
        {
            TileHighlightersTextures[(int)val] = textureLoader.Load<Texture2D>(Enum.GetName(val));
        }
        
        foreach (var val in Enum.GetValues<ChessComponents>())
        {
            ComponentsTextures[(int)val] = textureLoader.Load<Texture2D>(Enum.GetName(val));
        }
        
        foreach (var val in Enum.GetValues<EndGameTexts>())
        {
            GameEnds[(int)val] = textureLoader.Load<Texture2D>(Enum.GetName(val));
        }

        TileHighlightersTextures[(int)BoardPos.MoveType.PromAndAttack] =
            TileHighlightersTextures[(int)BoardPos.MoveType.AttackMove];
    }

// ------------------------------
// drawing methods
// ------------------------------

    public void Draw(SpriteBatch batch)
    {
        _drawBoard(batch);
        _drawHighlightedTiles(batch);
        _drawStaticFigures(batch);
        _drawHoveringFigure(batch);
        _drawEndGameSign(batch);
        _drawLastMove(batch);
    }

    private void _drawLastMove(SpriteBatch batch)
    {
        var lMove = _movesHistory.Last!.Value;
        
        if (lMove.HalfMoves == -1) return;

        batch.Draw(TileHighlightersTextures[(int)TileHighlighters.LastMove], Translate(lMove.MadeMove), Color.White);
        batch.Draw(TileHighlightersTextures[(int)TileHighlighters.LastMove], Translate(new BoardPos(lMove.OldX, lMove.OldY)), Color.White);
    }
    
    private void _drawBoard(SpriteBatch batch) 
        => batch.Draw(ComponentsTextures[(int)ChessComponents.Board], new Vector2(_xOffset, _yOffset), Color.White);

    private void _drawHighlightedTiles(SpriteBatch batch)
        // draws applies highlighting layers on the board to inform player about allowed move and ongoing actions
    {
        if (IsSelectedFigure())
        {
            var moves = _selectedFigure.GetMoves();
            
            batch.Draw(TileHighlightersTextures[(int)TileHighlighters.SelectedTile], Translate(_selectedFigure.Pos), Color.White);
            
            for (int i = 0; i < moves.movesCount; ++i)
                batch.Draw(TileHighlightersTextures[(int)moves.moves[i].MoveT], Translate(moves.moves[i]), Color.White);
        }

        if (IsChecked)
            batch.Draw(TileHighlightersTextures[(int)TileHighlighters.KingAttackTile], Translate(_colorMetadataMap[(int)_movingColor].King.Pos), Color.White);
        
    }

    private void _drawStaticFigures(SpriteBatch batch)
        // draws not moving figures on the board
    {
        foreach (var actFig in _figuresArray)
        {
            if (actFig.IsAlive)
                batch.Draw(ComponentsTextures[(int)actFig.TextureIndex], Translate(actFig.Pos), Color.White);
        }
    }

    private void _drawHoveringFigure(SpriteBatch batch)
        // draws figure on cursor, when figure is hold
    {
        if (_isHold)
        {
            var mState = Mouse.GetState();
                
            batch.Draw(ComponentsTextures[(int)_selectedFigure.TextureIndex],
                CenterFigurePosOnMouse(mState.X, mState.Y), Color.White);
        }
    }

    private void _drawEndGameSign(SpriteBatch batch)
    {
        if (!_isGameEnded) return;
        int horOffset = (Width - GameEnds[_endGameTextureInd].Width) / 2;
        int verOffset = (Height - GameEnds[_endGameTextureInd].Height) / 2;

        
        batch.Draw(GameEnds[_endGameTextureInd], new Vector2(_xOffset + horOffset, _yOffset + verOffset), Color.White);
    }
    
    
// ------------------------------
// Helping methods
// ------------------------------

    private Vector2 CenterFigurePosOnMouse(int x, int y) => new(x + MouseCentX, y + MouseCentY);


    private Vector2 Translate(BoardPos pos)
    {
        if (_movingColor == Figure.ColorT.White)
            return new Vector2(_xTilesCordOnScreenBeg + pos.X * FigureWidth, _yTilesCordOnScreenBeg - pos.Y * FigureHeight - 1);
        
        return new Vector2(_xTilesCordOnScreenBeg + pos.X * FigureWidth, pos.Y * FigureHeight - 1);
    }

    public BoardPos Translate(int x, int y)
    {
        if (_movingColor == Figure.ColorT.White)
            return new BoardPos((x - _xTilesCordOnScreenBeg) / FigureWidth, (_yTilesCordOnScreenBeg + 68 - y) / FigureHeight);
        
        return new BoardPos((x - _xTilesCordOnScreenBeg) / FigureWidth, y / FigureHeight);
    }
}