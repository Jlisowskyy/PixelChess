using System;
using PixelChess.ChessBackend;

namespace PixelChess;

public static class Program
{
    public static void Main()
    {
        // BoardTests.FenGeneratingTest();
        BoardTests.MoveGenerationTest(2);
        
        // using var game = new PixelChess();
        // game.Run();
    }
}