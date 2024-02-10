using System;
using System.Collections.Generic;
using System.IO;
using PixelChess.ChessBackend;
using PixelChess.Figures;

namespace PixelChess.Additional;

public abstract class PerformanceTestsGenerator
{
    public static void GeneratePerformanceTest(string filename, string[] fenPositions, int maxDepth, List<Board.ChessComponents> testFigs)
    {
        if (testFigs.Count == 0) return;
        
        using FileStream fs = new FileStream(filename, FileMode.CreateNew);
        using BinaryWriter writer = new BinaryWriter(fs);
        
        foreach (var position in fenPositions)
        {
            var bd = new Board(position);
            ulong recordCount = 0;
            
            // Counting moves
            bd.PerformMoveTraversal(_ => { ++recordCount; }, maxDepth);

            writer.Write(recordCount);
            
            // Saving records
            bd.PerformMoveTraversal(
                board =>
                {
                    ulong fullMap = IntegerNotationTranslator.GetFullMap(board.BoardFigures);
                    writer.Write(fullMap);

                    ulong testFigMap = 0;
                    foreach (var figType in testFigs)
                        testFigMap |= IntegerNotationTranslator.GetDesiredFigMap(board.BoardFigures, figType);

                    writer.Write(testFigMap);
                },
                maxDepth
            );
        }
    }
}