using System;
using PixelChess.ChessBackend;

namespace PixelChess;

public static class Program
{
    public static void Main()
    {
        // Board board = new Board("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8");
        // Board board = new Board("rnbqkbnr/pppp1ppp/4p3/8/8/5PP1/PPPPP2P/RNBQKBNR b KQkq - 0 2\n");
        // board.PerformShallowTest(2);

        Board board = new Board();
        board.PerformDeepTest(5);
        
        using var game = new PixelChess();
        game.Run();
    }
}