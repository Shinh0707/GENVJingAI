using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MusicPlayer : MonoBehaviour
{
    [SerializeField] private Slider m_timeSlider;
    [SerializeField] private SpriteToggle m_pauseToggle;
    public UnityEvent<bool> onPause = new();
    public UnityEvent<float> onTimeChanged = new();
    public float NormalizedValue
    {
        get
        {
            return m_timeSlider.value;
        }
        set
        {
            m_timeSlider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }
    }
    public void SetPauseWithputNotify(bool value)
    {
        m_pauseToggle.SetIsOnWithoutNotify(value);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_pauseToggle.onValueChanged.AddListener(onPause.Invoke);
        m_timeSlider.onValueChanged.AddListener(onTimeChanged.Invoke);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
