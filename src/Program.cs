using System;
using PixelChess.ChessBackend;

namespace PixelChess;

public static class Program
{
    public static void Main()
    {
        // using var game = new PixelChess();
        // game.Run();

        Board board = new Board("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        var ret = board.TestMoveGeneration(4);
        foreach (var res in ret)
        {
            Console.WriteLine(res);
        }
    }
}