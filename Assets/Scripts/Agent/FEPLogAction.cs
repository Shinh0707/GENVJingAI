using System;
using SimpleNN.Tensor;
using SimpleNN.Util;
using UnityEngine;
using UnityEngine.UI;

public class FEPLogAction : FEPAction
{
    [SerializeField] private int _actionCount = 10;
    [SerializeField] private Transform _target;
    [SerializeField] private SpriteRenderer _sprite;
    private int _actionCountRuntime = 10;
    void Awake()
    {
        _actionCountRuntime = _actionCount;
    }
    /// <summary>
    /// Number of action parameters required by this action.
    /// This determines the size of the output from the Actor model.
    /// </summary>
    public override int ActionCount { get {return _actionCountRuntime;} }

    /// <summary>
    /// Executes the action based on the provided values.
    /// </summary>
    /// <param name="actionValues">Normalized action values (0.0 to 1.0) from the Beta distribution.</param>
    public override void Execute(float[] actionValues, float[] _)
    {
        Debug.Log(StringExt.ArrayString("Executed: ", actionValues));
        var speed = actionValues[0] * 10;
        _target.position += new Vector3((actionValues[1] * 2 - 1)*speed, (actionValues[2] * 2 - 1)*speed);
        _target.localScale = new Vector2(actionValues[3] * 10 + 0.1f, actionValues[4] * 10 + 0.1f);
        _sprite.color = new Color(actionValues[4],actionValues[5],actionValues[6]);
    }
}
