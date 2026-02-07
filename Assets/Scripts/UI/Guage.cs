using UnityEngine;
using UnityEngine.UI;

public class Guage : MonoBehaviour
{
    [SerializeField] Image m_guageImage;
    [SerializeField] bool m_reverse;
    [SerializeField] float vMin = 0;
    [SerializeField] float vMax = 1;
    private float _currentValue;
    public float Value
    {
        get
        {
            return _currentValue;
        }
        set
        {
            _currentValue = value;
            float range = vMax - vMin;
            float nv;
            if (range == 0)
            {
                nv = 1;
            }
            else
            {
                nv = (_currentValue - vMin)/range;
            }
            nv = Mathf.Clamp01(nv);
            m_guageImage.fillAmount = m_reverse ? (1-nv) : nv;
        }
    }
}
