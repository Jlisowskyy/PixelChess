using System;
using PixelChess.ChessBackend;

namespace PixelChess;

public static class Program
{
    public static void Main()
    {
        // BoardTests.FenGeneratingTest();
        // BoardTests.MoveGenerationTest(5);

        string testCase = "        r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10\n";
        // var bd = new Board(testCase);
        // bd.PerformShallowTest(3);
        // // bd.PerformDeepTest(3, 32);
        using var game = new PixelChess(testCase);
        game.Run();
    }
}