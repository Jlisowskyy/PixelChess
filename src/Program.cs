using System;
using PixelChess.ChessBackend;

namespace PixelChess;

public static class Program
{
    public static void Main()
    {
        // BoardTests.FenGeneratingTest();
        BoardTests.MoveGenerationTest(5);

        // string testCase = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";
        // var bd = new Board(testCase);
        // bd.PerformShallowTest(3);
        // // bd.PerformDeepTest(3, 32);
        // using var game = new PixelChess(testCase);
        // game.Run();
    }
}