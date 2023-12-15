using System;
using PixelChess.ChessBackend;

namespace PixelChess;

public static class Program
{
    public static void Main()
    {
        // BoardTests.FenGeneratingTest();
        // BoardTests.MoveGenerationTest(5);

        string testCase = " rr3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1\n";
        var bd = new Board(testCase);
        // bd.PerformShallowTest(4);
        bd.PerformDeepTest(5,1);
        using var game = new PixelChess(testCase);
        game.Run();
    }
}