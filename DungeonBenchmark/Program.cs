using System.Diagnostics;
using DungeonCore;
using DungeonCore.Heuristic;
using DungeonCore.Topology;
using DungeonCore.Model;
using DungeonCore.Propagator;
using DungeonCore.Shared.Data;
using DungeonCore.Shared.Util;

var s =
    """
                
     ┌─────┐     
     │     └──┐ 
     │  WFC   │ 
     └──┐     │ 
        └─────┘ 
                
    """;
var (charData, width, height) = Helpers.StringToCharGrid(s);
var mg = new MappedGrid<char>(charData, width, height,'?');

const int oh = 28;
const int ow = 100;
var sw = new Stopwatch();
var runs = 0;
var result = PropagationResult.Contradicted;
while (result == PropagationResult.Contradicted || runs < 100)
{
    GC.Collect();
    var seed = Random.Shared.Next();
    // seed = 1190156738;
    var tm = new TileMapGenerator<char>(mg,
        new OverlappingModel(3),
        new OptimizedEntropyHeuristic(), 
        new Ac4Propagator(),
        ow, oh, seed);
    sw.Reset();
    sw.Start();
    result = tm.Generate(true);
    sw.Stop();
    runs++;
    Console.WriteLine($"Runs: {runs} | Seed: {seed} | {result} (took {sw.ElapsedMilliseconds}ms)");
}