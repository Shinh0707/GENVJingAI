using System;
using UnityEngine;
using UnityEngine.UI;

public class GraphTextureRenderer : MonoBehaviour
{
    [SerializeField] private RawImage m_targetImage;
    [SerializeField] private int m_width = 64;
    [SerializeField] private int m_height = 64;
    [SerializeField] private Color m_lineColor = Color.white;
    public float vMin = 0f;
    public float vMax = 1f;
    private Vector2Int _runtimeSize = new();
    private Texture2D _texture;
    private Color32[] _texturePixcels;
    private float[] _values;
    public float[] Values
    {
        get
        {
            return _values;
        }
        set
        {
            _values = value;
            Render();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _runtimeSize = new(m_width, m_height);
        _texture = new(m_width, m_height, TextureFormat.RGBA32, false); 
        _texturePixcels = new Color32[m_width * m_height];
        m_targetImage.texture = _texture;
        ClearPixels();
        _texture.Apply();
    }

    private void ClearPixels()
    {
        Array.Fill(_texturePixcels, new Color32(0, 0, 0, 0));
    }

    public void Render()
    {
        if (_values == null || _values.Length == 0) return;
        ClearPixels();
        var range = vMax - vMin;
        if (range == 0f) return;
        int step = Mathf.Max(1, (int)((float)_values.Length / _runtimeSize.x));
        int xstep = (_values.Length < _runtimeSize.x) 
            ? Mathf.Max(1, _runtimeSize.x / _values.Length) 
            : 1;
        float lnv = Mathf.Clamp01((_values[0] - vMin) / range);
        int x = 0; 
        for (int i = 0; i < _values.Length; i += step)
        {
            if (x >= _runtimeSize.x) break;

            float nv = Mathf.Clamp01((_values[i] - vMin) / range);
            for (int j = 0; j < xstep; j++)
            {
                int currentX = x + j;
                if (currentX >= _runtimeSize.x) break;
                float t = (float)j / xstep;
                float r = Mathf.Lerp(lnv, nv, t);
                
                int y = (int)((_runtimeSize.y - 1) * r);
                y = Mathf.Clamp(y, 0, _runtimeSize.y - 1);

                int idx = y * _runtimeSize.x + currentX;
                _texturePixcels[idx] = m_lineColor;
            }

            lnv = nv;
            x += xstep;
        }
        _texture.SetPixels32(_texturePixcels);
        _texture.Apply();
    }
}
