using DungeonCore.Model;
using DungeonCore.Topology;

namespace DungeonCore.Propagator;

public interface IPropagator
{
    void Initialize(WaveGrid grid, IModel model) { /* no-op */ }
    bool Collapse(WaveGrid grid, IModel model, int cellId);
}
