using SimpleNN.Graph;
using UnityEngine;
using SimpleNN.Module;
using SimpleNN.Util;
using SimpleNN.Tensor;

/// <summary>
/// Global Audio Embedding model for the FEP Agent.
/// Encodes audio input (TensorBox) into a state vector.
/// Input shape: (Batch, Channels, Depth, H, W)
/// Uses Conv3d layers similar to FEPImageEmbedding's Conv2d structure.
/// </summary>
public class FEPGlobalAudioEmbedding : Module
{
    private Sequential _net;
    private int _outputSize;

    public int GetOutputSize()
    {
        return _outputSize;
    }

    public FEPGlobalAudioEmbedding(int inSize, int seqLen, int outputSize)
    {
        var sz0 = new int[]{1, inSize, seqLen};
        Debug.Log(StringExt.ArrayString("Input: ",sz0));
        var conv1 = new Conv1d(inSize, inSize/2, 3, stride: 1, padding: 1);
        var sz1 = conv1.GetOutputSize(sz0);
        Debug.Log(StringExt.ArrayString("Conv1 Out: ",sz1));
        var conv2 = new Conv1d(sz1[1], sz1[1], 3, stride: 1, padding: 1);
        var sz2 = conv2.GetOutputSize(sz1);
        Debug.Log(StringExt.ArrayString("Conv2 Out: ",sz2));
        var conv3 = new Conv1d(sz2[1], sz2[1]/2, 3, stride: 1, padding: 1);
        var sz3 = conv3.GetOutputSize(sz2);
        Debug.Log(StringExt.ArrayString("Conv3 Out: ",sz3));
        var conv4 = new MaxPool1d(3, stride: 2, padding: 1);
        var sz4 = conv4.GetOutputSize(sz3);
        Debug.Log(StringExt.ArrayString("Conv4 Out: ",sz4));
        _outputSize = outputSize;
        Debug.Log($"Embedding Out: {ArrayExt.IntProd(sz4)} -> {_outputSize}");
        _net = AddModule(new Sequential(
            // Input: (B, C, H, W)
            conv1, // Downsample
            new ShiftedSin(1.0f),
            conv2, // Downsample
            new ShiftedSin(2.0f),
            conv3, // Downsample
            new Tanh(),
            conv4, // 64 -> 32
            new Flatten(1, -1), // 32 * 8 * 8 = 2048
            new ShiftedSin(2.0f),
            new Linear(ArrayExt.IntProd(sz4), _outputSize),
            new Tanh()
        ));
    }

    public TensorBox Forward(TensorBox x)
    {
        return _net.Forward(x);
    }
}
