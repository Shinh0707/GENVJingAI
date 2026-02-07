using SimpleNN.Graph;
using SimpleNN.Module;
/// <summary>
/// Global Actor model for the FEP Agent.
/// Maps the global state vector to a Beta distribution over actions.
/// </summary>
public class FEPGlobalActor : Module
{
    private Sequential _shared;
    private Sequential _alphaHead;
    private Sequential _betaHead;

    private const float MinParamValue = 1.01f;
    private const float OutputScale = 4.0f;

    public FEPGlobalActor(int inputSize, int actionCount)
    {
        int hidsize = (inputSize + actionCount)/2;
        _shared = AddModule(new Sequential(
            new Linear(inputSize, hidsize),
            new ShiftedSin(3.0f),
            new Linear(hidsize, hidsize),
            new ShiftedSin(2.0f)
        ));

        _alphaHead = AddModule(new Sequential(
            new Linear(hidsize, actionCount),
            new ShiftedSin(OutputScale * 0.5f, OutputScale * 0.5f + MinParamValue)
        ));

        _betaHead = AddModule(new Sequential(
            new Linear(hidsize, actionCount),
            new ShiftedSin(OutputScale * 0.5f, OutputScale * 0.5f + MinParamValue)
        ));
    }

    public BetaDistibution Forward(TensorBox state)
    {
        var x = _shared.Forward(state);
        var alpha = _alphaHead.Forward(x);
        var beta = _betaHead.Forward(x);
        return new BetaDistibution(alpha, beta);
    }
}
