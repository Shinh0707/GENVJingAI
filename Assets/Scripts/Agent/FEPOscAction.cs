using System;
using extOSC;
using SimpleNN.Tensor;
using SimpleNN.Util;
using UnityEngine;
using UnityEngine.UI;

public class FEPOscAction : FEPAction
{
    [Serializable]
    public class ActionData
    {
        public float defaultValue = 0.0f;
        public bool isLocked = false;
        public bool isPulse = false;
    }
    [SerializeField] private int _topk = 5;
    [SerializeField] private float _activeThreshold = 0.5f;
    [SerializeField] private ActionData[] _actionDatas = new ActionData[40];
    [SerializeField] private OSCTransmitter _transmitter;
    public OSCTransmitter Transmitter => _transmitter;
    [SerializeField] private OSCReceiver _reciever;
    private int _runtimeActionCount = 40;
    public override int ActionCount => _runtimeActionCount;

    private float[] lastActionValues;
    private float[] lastActivatedActionMask;
    private ActionData[] _runtimeActionDatas;
    public int TopK
    {
        get
        {
            return _topk;
        }
        set
        {
            _topk = Mathf.Clamp(value,1,ActionCount);
        }
    }
    public ActionData[] ActionDatas => _runtimeActionDatas;
    public void UpdateActionData(int index, Func<ActionData,ActionData> op)
    {
        if (index < 0 || index >= _runtimeActionDatas.Length)
        {
            return;
        }
        _runtimeActionDatas[index] = op(_runtimeActionDatas[index]);
    }
    public override float[] GetLearnMask()
    {
        return lastActivatedActionMask;
    }

    void Awake()
    {
        _runtimeActionCount = _actionDatas.Length;
        _runtimeActionDatas = new ActionData[_runtimeActionCount];

        for (int i = 0; i < _runtimeActionCount; i++)
        {
            var a = _actionDatas[i];
            _runtimeActionDatas[i] = new()
            {
                defaultValue = a.defaultValue,
                isLocked = a.isLocked,
                isPulse = a.isPulse
            };
        }
        lastActionValues = new float[_runtimeActionCount];
        lastActivatedActionMask = new float[_runtimeActionCount];

        _globalActionValues = new float[_runtimeActionCount];
        _globalLogProb = new float[_runtimeActionCount];
        _lastGlobalMask = new float[_runtimeActionCount];
    }

    void SendMessage(string address, float[] values)
    {
        var message = new OSCMessage(address);
        var vals = new OSCValue[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            vals[i] = OSCValue.Float(values[i]);
        }
        message.AddRange(vals);
        _transmitter.Send(message);
    }
    void SendMessage(string address, int[] values)
    {
        var message = new OSCMessage(address);
        var vals = new OSCValue[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            vals[i] = OSCValue.Int(values[i]);
        }
        message.AddRange(vals);
        _transmitter.Send(message);
    }
    void SendMessage(string address, float value)
    {
        var message = new OSCMessage(address);
        message.AddValue(OSCValue.Float(value));
        _transmitter.Send(message);
    }
    void SendMessage(string address, int value)
    {
        var message = new OSCMessage(address);
        message.AddValue(OSCValue.Int(value));
        _transmitter.Send(message);
    }

    private float[] _globalActionValues;
    private float[] _globalLogProb;
    private float[] _lastGlobalMask;

    public override float[] GetGlobalMask()
    {
        return _lastGlobalMask;
    }

    public override void SetGlobalParameters(float[] actionValues, float[] logprob)
    {
        Array.Copy(actionValues, _globalActionValues, actionValues.Length);
        Array.Copy(logprob, _globalLogProb, logprob.Length);
    }

    public override void Execute(float[] actionValues, float[] logprob)
    {
        var useGlobal = new bool[_runtimeActionCount];
        bool hasGlobal = _globalActionValues != null && _globalActionValues.Length == _runtimeActionCount;

        for (int i = 0; i < _runtimeActionCount; i++)
        {
            if (hasGlobal && _globalLogProb[i] > logprob[i])
            {
                useGlobal[i] = true;
                _lastGlobalMask[i] = 1.0f;
            }
            else
            {
                useGlobal[i] = false;
                _lastGlobalMask[i] = 0.0f;
            }
        }

        // 2. Prepare Effective Values for TopK
        var effectiveActionValues = new float[_runtimeActionCount];
        var effectiveLogProbs = new float[_runtimeActionCount]; // For TopK sorting

        for(int i=0; i<_runtimeActionCount; i++)
        {
            if(useGlobal[i])
            {
                effectiveActionValues[i] = _globalActionValues[i];
                effectiveLogProbs[i] = _globalLogProb[i];
            }
            else
            {
                effectiveActionValues[i] = actionValues[i];
                effectiveLogProbs[i] = logprob[i];
            }
        }

        var topkMask = new bool[_runtimeActionCount];
        var k = _topk;
        var topkIndices = new int[k];
        Array.Fill(topkIndices, -1);
        int ki;
        for (ki = 0; ki < k; ki++)
        {
            var a = _runtimeActionDatas[ki];
            if (!a.isLocked){
                topkIndices[ki] = ki;
                topkMask[ki] = true;
            }
        }
        for (; ki < _runtimeActionCount; ki++)
        {
            var a = _runtimeActionDatas[ki];
            if (!a.isLocked){
                var lp = effectiveLogProbs[ki]; // Use Effective LogProb for ranking
                for (int kj = 0; kj < k; kj++)
                {
                    int tki = topkIndices[kj];
                    if (tki == -1)
                    {
                        topkIndices[kj] = ki;
                        topkMask[ki] = true;
                    }
                    else if (effectiveLogProbs[tki] < lp)
                    {
                        topkIndices[kj] = ki;
                        topkMask[tki] = false;
                        topkMask[ki] = true;
                        break;
                    }
                }
            }
        }
        for (int i = 0; i < _runtimeActionCount; i++)
        {
            lastActivatedActionMask[i] = 0.0f;
            var a = _runtimeActionDatas[i];
            if (a.isLocked)
            {
                actionValues[i] = a.defaultValue;
                continue;
            }
            var active = topkMask[i];
            var v = effectiveActionValues[i]; // Use Effective Value
            if (!float.IsFinite(v))
            {
                v = float.IsNaN(v) ? 0.0f : (v > 1.0f ? 1.0f : 0.0f);
            }
            if (a.isPulse)
            {
                var activated = (v >= _activeThreshold) && active;
                actionValues[i] = activated ? 1.0f : 0.0f;
            }
            else
            {
                //var lv = lastActionValues[i];
                //var msk = lastActivatedActionMask[i] > 0.5f;
                actionValues[i] = active ? v : a.defaultValue;
            }
            lastActivatedActionMask[i] = active ? 1.0f : 0.0f;
        }

        // 受信アドレスの送信
        int[] ipaddress = new int[4];
        int ipi = 0;
        foreach(var ip in _reciever.LocalHost.Split(".")){
            ipaddress[ipi] = Int32.Parse(ip);
            ipi++;
        }
        SendMessage(
            $"/params/ip", ipaddress
        );
        SendMessage(
            $"/params/port", _reciever.LocalPort
        );
        SendMessage(
            $"/params/size", _runtimeActionCount
        );
        SendMessage(
            $"/params/outh", 17
        );
        SendMessage(
            $"/params/outw", 30
        );
        SendMessage(
            $"/params", actionValues
        );
        Array.Copy(actionValues, lastActionValues, _runtimeActionCount);
    }
}