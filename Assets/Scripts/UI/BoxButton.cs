using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BoxButton : Selectable, IBeginDragHandler, IDragHandler
{
    [SerializeField] private Image m_filler;
    [SerializeField] private SpriteToggle m_aiLock;
    [SerializeField] private Guage m_probGuage;
    private float m_currentValue = 0f;
    public bool isPulse = false;
    public UnityEvent<float> onValueChanged = new();
    public UnityEvent<bool> onLockChanged = new();
    private Vector2 _mouseDownStartPos = new();
    private float _mouseDownStartValue = 0f;
    private ColorBlock cacheCB = new();
    public float Value
    {
        get
        {
            return m_currentValue;
        }
        set
        {
            SetValueWithoutNotify(value);
            onValueChanged?.Invoke(m_currentValue);
        }
    }
    public bool isAILocked
    {
        get
        {
            return m_aiLock.isOn;
        }
        set
        {
            m_aiLock.isOn = value;
        }
    }
    public float Prob
    {
        get
        {
            return m_probGuage.Value;
        }
        set
        {
            m_probGuage.Value = value;
        }
    }
    protected override void Start()
    {
        base.Start();
        cacheCB.colorMultiplier = 1f;
        cacheCB.fadeDuration = 0.1f;
        m_aiLock.onValueChanged.AddListener(onLockChanged.Invoke);
    }
    public void SetValueWithoutNotify(float value)
    {
        if (isPulse)
        {
            m_currentValue = Mathf.Round(Mathf.Clamp01(value));
        }
        else{
            m_currentValue = Mathf.Clamp01(value);
        }
        m_filler.fillAmount = 1 - m_currentValue;
    }
    public void SetLockWithoutNotify(bool value)
    {
        m_aiLock.SetIsOnWithoutNotify(value);
    }
    public void SetButtonColor(float hue)
    {
        cacheCB.normalColor = Color.HSVToRGB(hue,0.7f,0.8f);
        cacheCB.highlightedColor = Color.HSVToRGB(hue,0.9f,0.7f);
        cacheCB.pressedColor = Color.HSVToRGB(hue,0.9f,0.4f);
        cacheCB.selectedColor = Color.HSVToRGB(hue,0.7f,0.9f);
        cacheCB.disabledColor = Color.HSVToRGB(hue,0.4f,0.5f);
        cacheCB.colorMultiplier = 1f;
        cacheCB.fadeDuration = 0.1f;
        colors = cacheCB;
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        _mouseDownStartPos = eventData.position;
        _mouseDownStartValue = m_currentValue;
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.dragging){
            var delta = (_mouseDownStartPos - eventData.position).y;
            var sz = ((RectTransform)transform).sizeDelta.y;
            float nv = delta/sz * 0.5f;
            Value = _mouseDownStartValue + nv;
        }
    }
}
