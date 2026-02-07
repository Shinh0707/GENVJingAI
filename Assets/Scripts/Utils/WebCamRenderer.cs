using UnityEngine;
using System.Collections;

public class WebCamRenderer : MonoBehaviour
{
    [SerializeField]
    private string deviceName;
    [SerializeField]
    public RenderTexture renderTexture;
    [SerializeField]
    private int requestedFPS = 30;
    private bool _isReady = false;
    public bool IsReady => _isReady && (webCamTexture != null) && (renderTexture != null);
    public int Width => renderTexture.width;
    public int Height => renderTexture.height;
    private WebCamTexture webCamTexture;

    public WebCamTexture WebCamTexture => webCamTexture;
    private Rect _renderRect;

    public string DeviceName
    {
        get => deviceName;
        set => deviceName = value;
    }
    public int RequestedFPS => requestedFPS;

    public void Play()
    {
        if ((webCamTexture != null) && webCamTexture.isPlaying)
        {
            return;
        }
        _isReady = false;
        StartCoroutine(InitializeCamera());
    }

    public void Stop()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
        }
    }

    private IEnumerator InitializeCamera()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            if (string.IsNullOrEmpty(deviceName))
            {
                WebCamDevice[] devices = WebCamTexture.devices;
                if (devices.Length > 0)
                {
                    deviceName = devices[0].name;
                }
            }

            webCamTexture = new WebCamTexture(deviceName, Width, Height, requestedFPS);
            webCamTexture.Play();
            while ((webCamTexture.width < Width) || (webCamTexture.height < Height))
            {
                yield return null;
            }
            _isReady = true;
            _renderRect = new Rect(0,0,Width,Height);
        }
        else
        {
            Debug.LogError("WebCam permission denied.");
        }
    }

    public void Render(Texture2D target)
    {
        Graphics.Blit(webCamTexture, renderTexture);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;

        target.ReadPixels(_renderRect, 0, 0);
        target.Apply();

        RenderTexture.active = previous;
    }

    void OnDestroy()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
        }
    }
}
