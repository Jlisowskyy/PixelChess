using System;

namespace PixelChess.ChessBackend;

public abstract class BoardTests
{
    // Collection of well known testing positions, most of them are quite tricky.
    public static readonly string[] MainTestPositions =
    {
        "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
        "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1",
        "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1",
        "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1",
        "r2q1rk1/pP1p2pp/Q4n2/bbp1p3/Np6/1B3NBn/pPPP1PPP/R3K2R b KQ - 0 1",
        "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8",
        "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10",
    };
    
    public static void MoveGenerationTest(int depth)
        // Function performs perft tests on board with set of fen position, which are commonly used to evaluate quality
        // and correctness of chess engines worldwide. 
    {
        int testCount = 0;
        foreach (var position in MainTestPositions)
        {
            Console.WriteLine("-----------------------------------------------------------------------------");
            Board bd = new Board(position);

            try
            {
                Console.WriteLine($"[ TEST {++testCount} ] Testing position: {position}");
                _printSimpleFenPos(position);
                bool result = bd.PerformShallowTest(depth);

                if (!result)
                {
                    Console.WriteLine("-----------------------------------------------------------------------------");
                    Console.WriteLine("_______________________________WARNING_______________________________________");
                    Console.WriteLine("Board failed on this position, starting deep searching...");
                    bd.PerformDeepTest(depth, 1);
                }
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine($"[ ERROR ] Aborted test, critical error occured!\nError: {exc}\nPosition before error:");
                string fenPos = FenTranslator.GetPosString(bd);
                Console.WriteLine(fenPos);
                _printSimpleFenPos(fenPos);
            }

        }
        Console.WriteLine("-----------------------------------------------------------------------------");
    }

    public static void FenGeneratingTest()
    {
        foreach (var position in MainTestPositions)
        {
            Console.WriteLine("-----------------------------------------------------------------------------");
            
            Console.WriteLine($"Testing position:\n{position}");
            Board bd = new Board(position);
            string nFen = FenTranslator.Translate(bd);
            Console.WriteLine($"Calculated results from board:\n{nFen}");
            Console.WriteLine($"Is result positive: {nFen == position}");
            
            Console.WriteLine("-----------------------------------------------------------------------------");
        }
    }

    private static void _printSimpleFenPos(string fenPos)
    {
        Console.Write("+---+---+---+---+---+---+---+---+\n|");
        for (int i = 0; i < fenPos.Length && fenPos[i] != ' '; ++i)
        {
            if (fenPos[i] == '/') Console.Write("\n+---+---+---+---+---+---+---+---+\n|");
            else if (char.IsNumber(fenPos[i]))
            {
                for (int j = 0; j < fenPos[i] - '0'; ++j)
                    Console.Write($"   |");
            }
            else Console.Write($" {fenPos[i]} |");
        }
        Console.Write("\n+---+---+---+---+---+---+---+---+\n");
    }
}