using DungeonCore.Model;
using DungeonCore.Shared;
using DungeonCore.Topology;

namespace DungeonCore.Propagator;

public sealed class RecursivePropagator(int maxDepth = int.MaxValue) : IPropagator
{
    public bool Collapse(WaveGrid grid, IModel model, int cellId)
    {
        var state = model.PickState(grid.Cells[cellId]);
        // Contradiction = state is invalid, or cell can't be observed
        if (state == -1 || !grid.Observe(cellId, state)) return false;
        return Propagate(grid, model, cellId, maxDepth);
    }

    private bool Propagate(WaveGrid grid, IModel model, int cellId, int depth)
    {
        var cell = grid.Cells[cellId];
        var valid = cell.DomainCount > 0;
        // Bail when max set recursion depth is reached,
        // or there is a contradiction
        if (depth <= 0 || !valid) return valid;
        
        // For every neighbor...
        foreach (var (neighborId, dir) in grid.NeighborsOf(cellId))
        {
            var nCell = grid.Cells[neighborId];
            var oppositeDir = Direction.Invert(dir);

            // ...check every valid state...
            for (var nState = 0; nState < model.StateCount; nState++)
            {
                if (!nCell.Domain[nState]) continue;
                
                // ...and check if it's still supported
                var isCompatible = model
                    .GetNeighbors(nState, oppositeDir)
                    .Any(support => cell.Domain[support]);
                if (isCompatible) continue;
                
                // Ban when incompatible
                var weight = model.GetWeight(nState);
                if (!grid.Ban(neighborId, nState, weight)) continue;
                // Propagate changes from this changed cell recursively
                if (Propagate(grid, model, neighborId, depth - 1)) continue;
                // If propagation contradicted, we also contradict here
                return false;
            }
        }
        
        return true;
    }
}
