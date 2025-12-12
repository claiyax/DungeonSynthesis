using DungeonCore.Topology;

namespace DungeonCore.Heuristic;

public interface IHeuristic
{
    int PickNextCell(WaveGrid grid);
    void OnBanned(int cellId, int state) { /* no-op */ }
    void OnObserved(int cellId, int state) { /* no-op */ }
}
