using DungeonCore.Model;
using DungeonCore.Shared;
using DungeonCore.Topology;

namespace DungeonCore.Propagator;

public class RecursivePropagator(int maxDepth = int.MaxValue) : IPropagator
{
    public bool Collapse(WaveGrid grid, IModel model, int cellId)
    {
        var state = model.PickState(grid.Cells[cellId]);
        if (state == -1 || !grid.Observe(cellId, state)) return false;
        Propagate(grid, model, cellId, maxDepth);
        return true;
    }

    public int Propagate(WaveGrid grid, IModel model, int cellId, int depth)
    {
        if (depth == 0) return maxDepth;
        var cell = grid.Cells[cellId];
        var deepest = maxDepth - depth;
        
        foreach (var (neighborId, dir) in grid.NeighborsOf(cellId))
        {
            var nCell = grid.Cells[neighborId];
            var oppositeDir = Direction.Invert(dir);

            for (var nState = 0; nState < model.StateCount; nState++)
            {
                if (!nCell.Domain[nState]) continue;
                var isCompatible = model
                    .GetNeighbors(nState, oppositeDir)
                    .Any(allowedNeighbor => cell.Domain[allowedNeighbor]);

                if (isCompatible) continue;
                if (!grid.Ban(neighborId, nState)) continue;
                var newDepth = Propagate(grid, model, neighborId, depth - 1);
                deepest = Math.Max(deepest, newDepth);
            }
        }
        
        return deepest;
    }
}
