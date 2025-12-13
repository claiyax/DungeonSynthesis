using DungeonCore.Model;
using DungeonCore.Shared;
using DungeonCore.Topology;

namespace DungeonCore.Propagator;

public sealed class Ac3Propagator : IPropagator
{
    private readonly Stack<int> _dirtyStack = new();
    
    public bool Collapse(WaveGrid grid, IModel model, int cellId)
    {
        var state = model.PickState(grid.Cells[cellId]);
        // Contradiction = state is invalid, or cell can't be observed
        if (state == -1 || !grid.Observe(cellId, state)) return false;
        
        // Mark first changed cell as "dirty" and propagate from there
        _dirtyStack.Clear();
        _dirtyStack.Push(cellId);
        return Propagate(grid, model);
    }

    private bool Propagate(WaveGrid grid, IModel model)
    {
        while (_dirtyStack.Count > 0)
        {
            var cellId = _dirtyStack.Pop();
            var cell = grid.Cells[cellId];

            // Check every neighbor of the "dirty" cell
            foreach (var (neighborId, dir) in grid.NeighborsOf(cellId))
            {
                var nCell = grid.Cells[neighborId];
                var oppositeDir = Direction.Invert(dir);
                var changed = false;

                // Check every state of the neighbor
                for (var nState = 0; nState < model.StateCount; nState++)
                {
                    if (!nCell.Domain[nState]) continue;
                    
                    // Check compatibility (exhaustive)
                    // AC-2001 improves over this by caching the "last support" index
                    var isCompatible = model
                        .GetNeighbors(nState, oppositeDir)
                        .Any(support => cell.Domain[support]);
                    if (isCompatible) continue;
                    
                    // Ban if incompatible
                    var weight = model.GetWeight(nState);
                    if (!grid.Ban(neighborId, nState, weight)) continue;
                    // Check for contradiction here (this is where it can happen)
                    if (nCell.DomainCount < 1) return false;
                    changed = true;
                }

                // If there was a ban, mark the neighbor as "dirty"
                if (!changed) continue;
                _dirtyStack.Push(neighborId);
            }
        }

        return true;
    }
}
