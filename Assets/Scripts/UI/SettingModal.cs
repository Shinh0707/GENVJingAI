using UnityEngine;
using extOSC;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class SettingModal : MonoBehaviour
{
    [SerializeField] private UpdateChecker m_upChecker;
    [SerializeField] private GameObject ModalObject;
    [SerializeField] private FEPAgent _agent;
    [SerializeField] private FEPOscAction _oscAction;
    private OSCTransmitter transmitter => _oscAction.Transmitter;
    [SerializeField] private TMP_InputField _addressInput;
    [SerializeField] private TMP_InputField _portInput;
    [SerializeField] private DropdownDeviceSelector _micDeviceSelector;
    [SerializeField] private DropdownDeviceSelector _webcamDeviceSelector;
    [SerializeField] private Button _openButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private TextMeshProUGUI _versionTextMesh;
    [SerializeField] private Button _updateDownloadLinkButton;
    void Start()
    {
        _addressInput.onEndEdit.AddListener((address) =>
        {
            transmitter.RemoteHost = address;
        });
        _portInput.onEndEdit.AddListener((portText) =>
        {
            var background = _portInput.targetGraphic as Image;
            var port = int.Parse(portText);
            if (1024 <= port && port < 65535){
                transmitter.RemotePort = port;
                background.color = Color.white;
            }
            else
            {
                background.color = new Color(1f, 0.5f, 0.5f);
            }
        });
        _openButton.onClick.AddListener(() =>
        {
            ModalObject.SetActive(true);
            OnActive();
        });
        _closeButton.onClick.AddListener(Close);
        _micDeviceSelector.Setup(
            () => {return Microphone.devices;},
            (name) => {_agent.MicDeviceName = name;},
            _agent.SetMicrophoneActive
        );
        _webcamDeviceSelector.Setup(
            () => {return WebCamTexture.devices.Select((d) => d.name).ToArray();},
            (name) => {_agent.WebcamDeviceName = name;},
            _agent.SetWebcamActive
        );
        _versionTextMesh.text = "Ver. "+Application.version;
        SetUpdateButtonState(m_upChecker.HasUpdate,m_upChecker.LatestVersionData);
        m_upChecker.onChecked.AddListener(SetUpdateButtonState);
        Close();
    }
    void SetUpdateButtonState(bool hasUpdate,VersionData versionData)
    {
        _updateDownloadLinkButton.interactable = hasUpdate;
        var textMesh = _updateDownloadLinkButton.GetComponentInChildren<TextMeshProUGUI>();
        textMesh.text = hasUpdate ? "UPDATE" : ((versionData is null) ? "SUPPORTED" : "LATEST");
        if (versionData is not null)
        {
            _updateDownloadLinkButton.onClick.RemoveAllListeners();
            _updateDownloadLinkButton.onClick.AddListener(() =>
            {
                Application.OpenURL(versionData.dllink);
            });
        }
    }
    void Close()
    {
        ModalObject.SetActive(false);
    }
    void OnActive()
    {
        _micDeviceSelector.OnActive(
            _agent.UseMicrophone,
            _agent.MicDeviceName
        );
        _webcamDeviceSelector.OnActive(
            _agent.UseWebcam,
            _agent.WebcamDeviceName
        );
        _addressInput.text = transmitter.RemoteHost;
        _portInput.text = transmitter.RemotePort.ToString();
    }
}
