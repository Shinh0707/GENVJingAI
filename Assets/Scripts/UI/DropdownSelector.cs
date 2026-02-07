using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class DropdownSelector : MonoBehaviour
{
    [SerializeField] private GameObject m_container;
    [SerializeField] private bool m_autoCloseWhenSelected = true;
    private List<DropdownElement> _items = new();
    public UnityEvent<DropdownElement> onSelect = new();
    private int _selected = -1;
    public int Selected => _selected;
    public void ForceSelect(int index)
    {
        if(0 <= index && index < _items.Count)
        {
            _selected = index;
        }
    }
    public bool TryGetCurrentItem(out DropdownElement obj)
    {
        if(0 <= _selected && _selected < _items.Count)
        {
            obj = _items[_selected];
            return true;
        }
        obj = null;
        return false;
    }
    public T AddElementPrefab<T>(GameObject prefab) where T : DropdownElement
    {
        var obj = Instantiate(prefab, m_container.transform);
        int index = _items.Count;
        var comp = obj.GetComponent<T>();
        comp.OnCreated(comp,() =>
        {
            OnElementSelected(index);
        });
        _items.Add(comp);
        return comp;
    }
    public DropdownElement AddElement(DropdownElement obj, Action<DropdownElement,Action> onCreate)
    {
        obj.transform.SetParent(m_container.transform);
        int index = _items.Count;
        onCreate?.Invoke(obj,() =>
        {
            OnElementSelected(index);
        });
        _items.Add(obj);
        return obj;
    }
    public void ClearElements()
    {
        var trash = new GameObject("_trash");
        trash.transform.SetParent(m_container.transform);
        foreach(var obj in _items)
        {
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(trash.transform);
        }
        _items.Clear();
        _selected = -1;
        Destroy(trash);
    }
    public void Open()
    {
        gameObject.SetActive(true);
    }
    public void Close()
    {
        gameObject.SetActive(false);
    }
    public void Toggle()
    {
        if (gameObject.activeSelf)
        {
            Close();
            return;
        }
        Open();
    }
    void OnElementSelected(int index)
    {
        ForceSelect(index);
        if (TryGetCurrentItem(out DropdownElement obj))
        {
            onSelect?.Invoke(obj);
        }
        if (m_autoCloseWhenSelected)
        {
            Close();
        }
    }
}