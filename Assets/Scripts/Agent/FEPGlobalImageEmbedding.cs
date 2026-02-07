using SimpleNN.Graph;
using UnityEngine;
using SimpleNN.Module;
using SimpleNN.Util;

/// <summary>
/// Global Image Embedding model for the FEP Agent.
/// Encodes visual input (TensorBox) into a state vector.
/// Input shape: (Batch, Channels, Depth(Time), Height, Width)
/// Uses Conv3d layers similar to FEPImageEmbedding's Conv2d structure.
/// </summary>
public class FEPGlobalImageEmbedding : Module
{
    private Sequential _net;
    private int _outputSize;

    public int GetOutputSize()
    {
        return _outputSize;
    }

    public FEPGlobalImageEmbedding(int inputChannels, int depth, int height, int width)
    {
        var sz0 = new int[]{1, inputChannels, depth, height, width};
        Debug.Log(StringExt.ArrayString("Global Image Input: ", sz0));

        // Use Conv3d instead of Conv2d
        // Input: (B, C, D, H, W)
        // Conv3d args: in_channels, out_channels, kernel_size, stride, padding
        
        var conv1 = new Conv3d(inputChannels, 8, 3, stride: 2, padding: 1);
        var sz1 = conv1.GetOutputSize(sz0);
        Debug.Log(StringExt.ArrayString("Global Conv1 Out: ", sz1));
        
        var conv2 = new Conv3d(8, 16, 3, stride: 2, padding: 1);
        var sz2 = conv2.GetOutputSize(sz1);
        Debug.Log(StringExt.ArrayString("Global Conv2 Out: ", sz2));
        
        var conv3 = new Conv3d(16, 8, 3, stride: 2, padding: 1);
        var sz3 = conv3.GetOutputSize(sz2);
        Debug.Log(StringExt.ArrayString("Global Conv3 Out: ", sz3));
        
        var conv4 = new MaxPool3d(3, stride: 2, padding: 1);
        var sz4 = conv4.GetOutputSize(sz3);
        Debug.Log(StringExt.ArrayString("Global Conv4 Out: ", sz4));
        
        _outputSize = ArrayExt.IntProd(sz4) / 4;
        Debug.Log($"Global Embedding Out: {ArrayExt.IntProd(sz4)} -> {_outputSize}");

        _net = AddModule(new Sequential(
            // Input: (B, C, D, H, W)
            conv1, // Downsample
            new ShiftedSin(1.0f),
            conv2, // Downsample
            new ShiftedSin(2.0f),
            conv3, // Downsample
            new Tanh(),
            conv4, // 64 -> 32
            new Flatten(1, -1),
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
