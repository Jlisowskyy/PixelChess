using System;
using System.Collections.Generic;
using System.Diagnostics;
using PixelChess.Figures;

namespace PixelChess.ChessBackend;

public partial class Board
{
    private class MoveGenerationTester
    {
        // ------------------------------------
        // class creation and interaction
        // ------------------------------------

        public MoveGenerationTester(Board bd)
        {
            _bd = bd;
        }

        public void PerformDeepMoveGenerationTest(int depth)
        {
            var invalidPaths = _findInvalidPaths(depth);

            foreach (var invalidPath in invalidPaths)
            {
                foreach (var invalidMove in invalidPath)
                {
                    Console.Write($"{invalidMove} --> ");
                }
                
                Console.Write(invalidPath.Count == 0 ? "\n" : "null\n");
            }
        }
        
        public ulong[] PerformShallowMoveGenerationTest(int depth)
        {
            var stockOutput = _getStockfishPerft(depth);
            var processedStock = _breakDownToMoves(stockOutput);
            
            // Performs own tests on positions
            long t1 = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            ulong[] arr = _testMoveGeneration(depth, depth, processedStock);
            long t2 = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("                 Final test summary");
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine($"Spent time: {(t2-t1)} (milliseconds)");
            for (int i = 0; i < arr.Length-1; ++i)
            {
                Console.WriteLine($"On depth {i + 1}, game generated {arr[i]} moves");
            }
            
            ulong stockResult = processedStock["total"];
            ulong diff = ulong.Max(arr[^1], stockResult) - ulong.Min(arr[^1], stockResult);       
            Console.WriteLine(diff == 0 ?
                $"On depth {arr.Length}, game generated {arr[^1]} moves" :
                $"On depth {arr.Length}, game generated {arr[^1]} moves ({stockResult}, diff={diff})" 
                );

            return arr;
        }
        
        // ------------------------------
        // private methods
        // ------------------------------

        private LinkedList<LinkedList<string>> _findInvalidPaths(int depth)
        {
            LinkedList<LinkedList<string>> ret = new LinkedList<LinkedList<string>>();
            var stockOutput = _getStockfishPerft(depth);
            var processedStock = _breakDownToMoves(stockOutput);
            var invalidMoves = _findInvalidMoves(depth, processedStock);
            
            if (depth == 2)
            {
                foreach (var invalidMove in invalidMoves)
                {
                    var list = new LinkedList<string>();
                    list.AddLast(invalidMove);
                    ret.AddLast(list);
                }

                return ret;
            }

            foreach (var invalidMove in invalidMoves)
            {
                _bd._isGameEnded = false; // TODO: there is some reason that this flags turns on - repair it
                _bd.MakeUciMove(invalidMove);
                var recResult = _findInvalidPaths(depth - 1);
                _bd._isGameEnded = false; // TODO: there is some reason that this flags turns on - repair it
                _bd._undoMove();
                
                foreach (var list in recResult)
                {
                    list.AddFirst(invalidMove);
                    ret.AddLast(list);
                }
            }
            
            return ret;
        }
        
        private LinkedList<string> _findInvalidMoves(int depth, Dictionary<string, ulong> correctNumbers)
            // outputs only moves, which leads to incorrect generated moves counts
        {
            var ret = new LinkedList<string>();

            var range = _bd._colorMetadataMap[(int)_bd._movingColor].AliesRangeOnFigArr;
            for (int i = range[0]; i < range[1]; ++i)
                if (_bd._figuresArray[i].IsAlive)
                {
                    var mv = _bd._figuresArray[i].GetMoves();
                    
                    for (int j = 0; j < mv.movesCount; ++j)
                    {
                        
                        // performing promotions when necessary
                        if ((mv.moves[j].MoveT & BoardPos.MoveType.PromotionMove) != 0)
                        {
                            Figure[] upgrades = {
                                new Knight(mv.moves[j].X, mv.moves[j].Y, _bd._figuresArray[i].Color),
                                new Bishop(mv.moves[j].X, mv.moves[j].Y, _bd._figuresArray[i].Color),
                                new Rook(mv.moves[j].X, mv.moves[j].Y, _bd._figuresArray[i].Color),
                                new Queen(mv.moves[j].X, mv.moves[j].Y, _bd._figuresArray[i].Color)
                            };
                            
                            foreach (var upgrade in upgrades)
                            {
                                upgrade.IsMoved = true;
                                upgrade.Parent = _bd;
                                
                                _bd._selectedFigure = _bd._figuresArray[i];
                                _bd._processMove(mv.moves[j]);
                                _bd.Promote(upgrade);
                                var recResult = _testMoveGeneration(depth - 1, depth, correctNumbers);
                                _bd._undoMove();

                                // adding to invalid moves list if necessary
                                string uciMoveCode = UciTranslator.GetUciMoveCode(_bd._figuresArray[i].Pos,
                                    mv.moves[j], upgrade);
                                _validateMove(uciMoveCode, recResult[^1], correctNumbers, ret);
                            }
                        }
                        else
                        {
                            _bd._selectedFigure = _bd._figuresArray[i];
                            _bd._processMove(mv.moves[j]);
                            var recResult = _testMoveGeneration(depth - 1, depth, correctNumbers);
                            _bd._undoMove(); 
                            
                            // adding to invalid moves list if necessary
                            string uciMoveCode = UciTranslator.GetUciMoveCode(_bd._figuresArray[i].Pos, mv.moves[j]);
                            _validateMove(uciMoveCode, recResult[^1], correctNumbers, ret);
                        }
                    }
                }

            return ret;
        }

        private string _getStockfishPerft(int depth)
        {
            // Loading stockfish perft to compare
            Console.WriteLine("Starting stockfish to get correct numbers...");
            
            Process stock = new Process();
            stock.StartInfo.UseShellExecute = false;
            stock.StartInfo.RedirectStandardOutput = true;
            stock.StartInfo.RedirectStandardInput = true;
            stock.StartInfo.FileName = "Deps/stockfish";
            stock.Start();
            stock.StandardInput.WriteLine($"position fen {FenTranslator.Translate(_bd)}");
            stock.StandardInput.WriteLine($"go perft {depth}");
            stock.StandardInput.WriteLine("quit");
            string stockOutput = stock.StandardOutput.ReadToEnd();
            stock.WaitForExit();
            Console.WriteLine("Stockfish finished his job!");

            return stockOutput;
        }
        
        private void _writeMoveGenerationResult(string uciMoveCode, ulong acquiredResult, Dictionary<string, ulong> correct)
        {
            if (!correct.ContainsKey(uciMoveCode))
                throw new ApplicationException(
                    $"Stockfish didnt found such move: {uciMoveCode}!!!");

            ulong stockResult = correct[uciMoveCode];
            ulong diff = ulong.Max(acquiredResult, stockResult) - ulong.Min(acquiredResult, stockResult);
            Console.WriteLine(diff == 0
                ? $"{uciMoveCode}: {acquiredResult}"
                : $"{uciMoveCode}: {acquiredResult} ({correct[uciMoveCode]}, diff={diff})");
        }
        
        private void _validateMove(string uciMoveCode, ulong acquiredResult, Dictionary<string, ulong> correct, LinkedList<string> invalidMoves)
        {
            if (!correct.ContainsKey(uciMoveCode))
                throw new ApplicationException(
                    $"Stockfish didnt found such move: {uciMoveCode}!!!");

            ulong stockResult = correct[uciMoveCode];
            ulong diff = ulong.Max(acquiredResult, stockResult) - ulong.Min(acquiredResult, stockResult);

            if (diff != 0)
            {
                invalidMoves.AddLast(uciMoveCode);
            }
        }
        private ulong[] _testMoveGeneration(int depth, int maxDepth, Dictionary<string, ulong> correctNumbers)
            // should not be considered as valid performance measurement, is only used to check whether move generation
            // is working as it should. 
        {
            ulong[] ret = new ulong[depth];

            var range = _bd._colorMetadataMap[(int)_bd._movingColor].AliesRangeOnFigArr;
            for (int i = range[0]; i < range[1]; ++i)
                if (_bd._figuresArray[i].IsAlive)
                {
                    var mv = _bd._figuresArray[i].GetMoves();
                    ret[0] += (ulong)mv.movesCount;
                    
                    // to correctly count moves with promotion, performance affect does not matter.
                    if (_bd._figuresArray[i] is Pawn)
                    {
                        for (int z = 0; z < mv.movesCount; z++)
                        {
                            if ((mv.moves[z].MoveT & BoardPos.MoveType.PromotionMove) != 0)
                            {
                                ret[0] += 3;
                            }
                        }
                    }
                    
                    // no need to process all moves when deeper depth is not expected
                    if (depth == 1) continue;
                    
                    for (int j = 0; j < mv.movesCount; ++j)
                    {
                        ulong[] recResult;
                        
                        // performing promotions when necessary
                        if ((mv.moves[j].MoveT & BoardPos.MoveType.PromotionMove) != 0)
                        {
                            Figure[] upgrades = new Figure[] {
                                new Knight(mv.moves[j].X, mv.moves[j].Y, _bd._figuresArray[i].Color),
                                new Bishop(mv.moves[j].X, mv.moves[j].Y, _bd._figuresArray[i].Color),
                                new Rook(mv.moves[j].X, mv.moves[j].Y, _bd._figuresArray[i].Color),
                                new Queen(mv.moves[j].X, mv.moves[j].Y, _bd._figuresArray[i].Color)
                            };
                            
                            foreach (var upgrade in upgrades)
                            {
                                upgrade.IsMoved = true;
                                upgrade.Parent = _bd;
                                
                                _bd._selectedFigure = _bd._figuresArray[i];
                                _bd._processMove(mv.moves[j]);
                                _bd.Promote(upgrade);
                                recResult = _testMoveGeneration(depth - 1, maxDepth, correctNumbers);
                                _bd._undoMove(); 
                            
                                // displaying results after specific moves
                                if (depth == maxDepth)
                                {
                                    string uciMoveCode = UciTranslator.GetUciMoveCode(_bd._figuresArray[i].Pos,
                                        mv.moves[j], upgrade);
                                    _writeMoveGenerationResult(uciMoveCode, recResult[^1], correctNumbers);
                                }
                        
                                // adding generated moves
                                for (int k = 0; k < recResult.Length; ++k)
                                {
                                    ret[1 + k] += recResult[k];
                                }
                            }
                        }
                        else
                        {
                            _bd._selectedFigure = _bd._figuresArray[i];
                            _bd._processMove(mv.moves[j]);
                            recResult = _testMoveGeneration(depth - 1, maxDepth, correctNumbers);
                            _bd._undoMove(); 
                            
                            // displaying results after specific moves
                            if (depth == maxDepth)
                            {
                                string uciMoveCode = UciTranslator.GetUciMoveCode(_bd._figuresArray[i].Pos, mv.moves[j]);
                                _writeMoveGenerationResult(uciMoveCode, recResult[^1], correctNumbers);
                            }
                        
                            // adding generated moves
                            for (int k = 0; k < recResult.Length; ++k)
                            {
                                ret[1 + k] += recResult[k];
                            }
                        }
                    }
                }

            return ret;
        }
        
        private static Dictionary<string, ulong> _breakDownToMoves(string stockfishPerftOutput)
            // Used to get extract values from go perft command output into literal values.
            // Assumes identical go perft command output layout.
        {
            Dictionary<string, ulong> ret = new Dictionary<string, ulong>();
            var lines = stockfishPerftOutput.Split('\n');

            for (int i = 1; i < lines.Length-4; ++i)
            {
                var items = lines[i].Split(": ");
                ulong mvCount = Convert.ToUInt64(items[1]);
                ret.Add(items[0], mvCount);
            }

            var totalString = lines[^3].Split(": ");
            ulong totalMoves = Convert.ToUInt64(totalString[1]);
            ret.Add("total", totalMoves);
            return ret;
        }
        
        // ------------------------------
        // private fields
        // ------------------------------
        
        private readonly Board _bd;
    }
}