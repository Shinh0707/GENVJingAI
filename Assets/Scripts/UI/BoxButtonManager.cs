using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BoxButtonManager : MonoBehaviour
{
    [SerializeField] private RectTransform m_container;
    [SerializeField] private GameObject m_boxButtonPrefab;
    private List<BoxButton> boxButtons = new();
    public UnityEvent<int,float> onButtonValueChanged = new();
    public UnityEvent<int,bool> onButtonLockChanged = new();
    public void AddButtons(int n)
    {
        int id = boxButtons.Count;
        for(int i = 0; i < n; i++){
            var inst = Instantiate(m_boxButtonPrefab, m_container);
            var bb = inst.GetComponent<BoxButton>();
            int bid = id;
            bb.onValueChanged.AddListener((value) => onButtonValueChanged.Invoke(bid,value));
            bb.onLockChanged.AddListener((value) => onButtonLockChanged.Invoke(bid,value));
            boxButtons.Add(bb);
            id++;
        }
        float hstep = 1.0f/(id + 1);
        float h = 0;
        foreach(var cbb in boxButtons){
            cbb.SetButtonColor(h);
            h += hstep;
        }
    }
    public void ActionForButton(int id, Action<BoxButton> action)
    {
        if (-1 < id && id < boxButtons.Count)
        {
            action?.Invoke(boxButtons[id]);
        }
    }
}
