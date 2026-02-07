using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class DropdownTextElement : DropdownElement
{
    [SerializeField] private Button m_button;
    [SerializeField] private TextMeshProUGUI m_textMesh;
    public string Text
    {
        get
        {
            return m_textMesh.text;
        }
        set
        {
            m_textMesh.text = value;
        }
    }
    public override void OnCreated(DropdownElement _, Action onSelect)
    {
        m_button.onClick.AddListener(() => onSelect.Invoke());
    }
}