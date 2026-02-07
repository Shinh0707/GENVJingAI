using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AIMonitor : MonoBehaviour
{
    [SerializeField] FEPAgent m_agent;
    [SerializeField] BoxButtonManager m_buttonManager;
    [SerializeField] GraphTextureRenderer m_imageDataRenderer;
    [SerializeField] GraphTextureRenderer m_audioDataRenderer;
    [SerializeField] GraphTextureRenderer m_rewardDataRenderer;
    [SerializeField] Slider m_topKSlider;
    [SerializeField] int m_rewardHistoryLength = 100;
    private int pending = 0;
    [SerializeField] private int interval = 120;
    private bool hasData = false;
    private List<float> _rewardHistory;
    private int _rewardHistoryAdded;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rewardHistory = new(m_rewardHistoryLength);
        _rewardHistoryAdded = 0;
        m_topKSlider.minValue = 1;
        m_topKSlider.maxValue = m_agent.ActionDatas.Length;
        m_topKSlider.SetValueWithoutNotify(m_agent.TopK);
        m_topKSlider.onValueChanged.AddListener((value) => m_agent.TopK = (int)value);
        m_agent.onAIUpdated.AddListener(() => hasData = true);
        StartCoroutine(SetupAsync());
    }
    IEnumerator SetupAsync()
    {
        FEPOscAction.ActionData[] ads = m_agent.ActionDatas;
        var n_acts = ads.Length;
        m_buttonManager.AddButtons(n_acts);
        yield return null;
        for(int i = 0; i < n_acts; i++)
        {
            var ad = ads[i];
            m_buttonManager.ActionForButton(i,(bb) =>
            {
               bb.Value = ad.defaultValue;
               bb.isAILocked = ad.isLocked;
               bb.isPulse = ad.isPulse;
            });
        }
        m_buttonManager.onButtonValueChanged.AddListener((index,value) =>
        {
           m_agent.UpdateActionData(index, (ad) =>
           {
              ad.defaultValue = value;
              return ad;
           });
        });
        m_buttonManager.onButtonLockChanged.AddListener((index,value) =>
        {
           m_agent.UpdateActionData(index, (ad) =>
           {
              ad.isLocked = value;
              return ad;
           });
        });
    }

    void Update()
    {
        if (!hasData) return;
        pending++;
        if (pending < interval) return;
        pending = 0;
        if (_rewardHistoryAdded == m_rewardHistoryLength)
        {
            _rewardHistory.RemoveAt(0);
        }
        else if (_rewardHistoryAdded > m_rewardHistoryLength)
        {
            for(int i = 0; i < _rewardHistoryAdded - m_rewardHistoryLength; i++)
            {
                _rewardHistory.Remove(0);
            }
            _rewardHistoryAdded = m_rewardHistoryLength;
        }
        else
        {
            _rewardHistoryAdded++;
        }
        _rewardHistory.Add(m_agent.AvgReward);
        m_imageDataRenderer.Values = m_agent.LastEmbeddedImageData;
        m_audioDataRenderer.Values = m_agent.LastEmbededAudioData;
        m_rewardDataRenderer.Values = _rewardHistory.ToArray();
        var probs = m_agent.LastLogProbData;
        var acts = m_agent.LastActionsData;
        for(int i = 0; i < probs.Length; i++)
        {
            m_buttonManager.ActionForButton(i,(bb) =>
            {
               bb.Prob = probs[i];
               bb.Value = acts[i];
            });
        }
    }
}
