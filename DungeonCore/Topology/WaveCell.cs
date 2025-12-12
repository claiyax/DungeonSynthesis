using System.Collections;

namespace DungeonCore.Topology;

public class WaveCell(int states, double sumWeights)
{
    public int Observed { get; private set; } = -1;
    public BitArray Domain { get; } = new(states, true);
    public int DomainCount { get; private set; } = states;
    public double SumWeights { get; private set; } = sumWeights;

    public void Observe(int stateId)
    {
        Observed = stateId;
        for (var i = 0; i < Domain.Length; i++) 
            Domain[i] = i == stateId;
        DomainCount = 1;
    }

    public bool Ban(int stateId, double weight)
    {
        if (!Domain[stateId]) return false;
        Domain[stateId] = false;
        DomainCount--;
        SumWeights -= weight;
        if (SumWeights < 1e-9) SumWeights = 0;
        return true;
    }
}