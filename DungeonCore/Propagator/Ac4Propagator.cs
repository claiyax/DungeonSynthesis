using System.Runtime.CompilerServices;
using DungeonCore.Model;
using DungeonCore.Shared.Data;
using DungeonCore.Topology;

namespace DungeonCore.Propagator;

public sealed class Ac4Propagator : IPropagator
{
    // Layout: Cell -> State -> Direction
    // [Cell * (StateCount * 4) + State * 4 + Direction]
    // Stores the number of supports per state per direction
    private int[] _supports = []; 
    private int _stateCount;
    private int _stateDirCount;
    private bool _initialized;
    private readonly Queue<(int cellId, int stateId)> _removalQueue = new();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetSupportIndex(int cellId, int stateId, int dir) => cellId * _stateDirCount + (stateId << 2) + dir;

    public void Initialize(WaveGrid grid, IModel model)
    {
        const int dirCount = 4;
        _stateCount = model.StateCount;
        _stateDirCount = _stateCount * dirCount;
        _supports = new int[grid.CellCount * _stateDirCount];
        
        // Track every cell such...
        for (var cellId = 0; cellId < grid.CellCount; cellId++)
        {
            var cell = grid.Cells[cellId];
            // ...that we go through all its neighbors in 4 directions...
            foreach (var (neighborId, dir) in grid.NeighborsOf(cellId))
            {
                var neighbor = grid.Cells[neighborId];
                // ...and count compatible states (support count)
                for (var s = 0; s < model.StateCount; s++)
                {
                    if (!cell.Domain[s]) continue;
                    var compatibles = model.GetNeighbors(s, dir);
                    var count = compatibles.Count(t => neighbor.Domain[t]);
                    _supports[GetSupportIndex(cellId, s, dir)] = count;
                }
            }
        }
        
        _initialized = true;
    }

    public bool Collapse(WaveGrid grid, IModel model, int cellId)
    {
        if (!_initialized) Initialize(grid, model);

        var cell = grid.Cells[cellId];
        var state = model.PickState(cell);
        if (state == -1) return false;
        
        _removalQueue.Clear();
        // Add all invalid states (all except the observed state)
        // to the removal queue for the selected cell to collapse
        for (var s = 0; s < model.StateCount; s++)
        {
            if (!cell.Domain[s]) continue;
            if (s == state) continue;
            _removalQueue.Enqueue((cellId, s));
        }
        
        // We check observation here, then propagate
        return grid.Observe(cellId, state) && Propagate(grid, model);
    }

    private bool Propagate(WaveGrid grid, IModel model)
    {
        // Pulling array reference to the local stack for tiny optimization
        var supports = _supports;
        
        while (_removalQueue.Count > 0)
        {
            var (removedCellId, removedState) = _removalQueue.Dequeue();
            // Check every neighbor of the changed cell
            foreach (var (neighborId, dir) in grid.NeighborsOf(removedCellId))
            {
                // Get the list of states in the neighbor that were supported by 'removedState'
                var supportedStates = model.GetNeighbors(removedState, dir);
                var oppositeDir = Direction.Invert(dir);
                var nCell = grid.Cells[neighborId];
                
                // Check all supported states that are still true
                foreach (var state in supportedStates)
                {
                    if (!nCell.Domain[state]) continue;
                    
                    // Reduce support count that neighbor cell
                    var supportIdx = GetSupportIndex(neighborId, state, oppositeDir);
                    supports[supportIdx]--;

                    if (supports[supportIdx] != 0) continue;
                    // No support = invalid state. Ban it.
                    if (!grid.Ban(neighborId, state, model.GetWeight(state))) continue;
                    // Check for contradiction here (this is where it can happen)
                    if (nCell.DomainCount == 0) return false;
                    // State was removed, enqueue the removal for this neighbor
                    _removalQueue.Enqueue((neighborId, state));
                }
            }
        }

        return true;
    }
}