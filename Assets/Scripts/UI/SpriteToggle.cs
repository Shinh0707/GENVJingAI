using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpriteToggle : Selectable
{
    [SerializeField] private Sprite m_isOffSprite;
    [SerializeField] private ColorBlock m_isOffColors;
    [SerializeField] private Sprite m_isOnSprite;
    [SerializeField] private ColorBlock m_isOnColors;
    [SerializeField] private bool m_isOn;
    public UnityEvent<bool> onValueChanged = new();
    public bool isOn
    {
        get
        {
            return m_isOn;
        }
        set
        {
            if (m_isOn ^ value){
                SetIsOnWithoutNotify(value);
                onValueChanged?.Invoke(m_isOn);
            }
        }
    }
    public void SetIsOnWithoutNotify(bool value)
    {
        m_isOn = value;
        SetSprite();
    }
    protected override void Start()
    {
        base.Start();
        SetSprite();
    }
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        isOn = !m_isOn;
    }
    private void SetSprite()
    {
        image.sprite = isOn ? m_isOnSprite : m_isOffSprite;
        colors = isOn ? m_isOnColors : m_isOffColors;
    }
}
