﻿using System;
using PixelChess.ChessBackend;

namespace PixelChess;

public static class Program
{
    public static void Main()
    {
        // BoardTests.MoveGenerationTest(5);
        
        using var game = new PixelChess();
        game.Run();
    }
}