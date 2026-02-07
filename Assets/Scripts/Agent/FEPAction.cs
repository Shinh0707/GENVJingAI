using System;
using UnityEngine;
/// <summary>
/// Abstract base class for defining actions in the FEP Agent.
/// Maps the continuous action values (from Beta distribution) to game operations.
/// </summary>
public abstract class FEPAction : MonoBehaviour
{
    /// <summary>
    /// Number of action parameters required by this action.
    /// This determines the size of the output from the Actor model.
    /// </summary>
    public abstract int ActionCount { get; }

    public virtual float[] GetLearnMask()
    {
        var mask = new float[ActionCount];
        Array.Fill(mask, 1f);
        return mask;
    }
    public virtual float[] GetGlobalMask()
    {
        var mask = new float[ActionCount];
        Array.Fill(mask, 0f);
        return mask;
    }
    public virtual void SetGlobalParameters(float[] actionValues, float[] logprob)
    {
        
    }

    /// <summary>
    /// Executes the action based on the provided values.
    /// </summary>
    /// <param name="actionValues">Normalized action values (0.0 to 1.0) from the Beta distribution.</param>
    public abstract void Execute(float[] actionValues, float[] logprob);
}
