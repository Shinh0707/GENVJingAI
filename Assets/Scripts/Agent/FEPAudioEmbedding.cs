using SimpleNN.Graph;
using UnityEngine;
using SimpleNN.Module;
using SimpleNN.Util;
using SimpleNN.Tensor;

/// <summary>
/// Embedding model for the FEP Agent.
/// Encodes audio input (TensorBox) into a state vector.
/// </summary>
public class FEPAudioEmbedding : Module
{
    private Sequential _net;
    private int _outputSize;

    public int GetOutputSize()
    {
        return _outputSize;
    }
    public FEPAudioEmbedding(int inSize, int outputSize)
    {
        _outputSize = outputSize;
        _net = AddModule(new Sequential(
            new Linear(inSize, (inSize + outputSize)/2),
            new ShiftedSin(3.0f),
            new Linear((inSize + outputSize)/2, outputSize),
            new Sin()
        ));
    }

    public TensorBox Forward(TensorBox x)
    {
        return _net.Forward(x);
    }
}
