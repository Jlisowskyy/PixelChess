using System;
using PixelChess.ChessBackend;

namespace PixelChess.Ui;

public class ModeChangeButton : Button
{
    public ModeChangeButton(Board board, UciTranslator translationUnit, GameMode mode)
    {
        _board = board;
        _uciUnit = translationUnit;
        _mode = mode;
    }

    protected sealed override void ClickReaction()
    {
        if (_board.IsGameStarted) return;

        _switchToNextTexture();

        switch (Names[_stateCounter])
        {
            case "player":
                _mode.ActMod = GameMode.ModeT.PlayerVsPlayerLocal;
                break;
            case "computer":
                if (!_uciUnit.IsOperational)
                    ClickReaction();
                else
                    _mode.ActMod = GameMode.ModeT.PlayerVsComputer;
                break;
            case "remote":
                throw new NotImplementedException();
        }
        
        SetActiveTexture(_stateCounter);
    }
    
    public class GameMode
    {
        public enum ModeT
        {
            PlayerVsPlayerLocal,
            PlayerVsComputer,
            PlayerVsPlayerRemote
        }
        
        public ModeT ActMod { get; set; }
    }

    private void _switchToNextTexture()
        => _stateCounter = (_stateCounter + 1) % Names.Length;
    protected sealed override string[] TextureNames
        => Names;

    private static readonly string[] Names = { "player", "computer" };
    private int _stateCounter;

    private readonly GameMode _mode;
    private readonly Board _board;
    private readonly UciTranslator _uciUnit;
}