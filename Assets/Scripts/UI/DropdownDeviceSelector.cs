using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DropdownDeviceSelector: MonoBehaviour
{
    [SerializeField] private DropdownSelector _dropdown;
    [SerializeField] private Button _dropdownButton;
    [SerializeField] private TextMeshProUGUI _dropdownResultTextMesh;
    [SerializeField] private GameObject _dropdownElementPrefab;
    [SerializeField] private Button _reloadButton;
    [SerializeField] private SpriteToggle _activeButton;
    public void Setup(
        Func<string[]> GetDevicesFunction,
        Action<string> onDeviceSelected,
        Action<bool> onActiveButtonClicked
    )
    {
        _dropdown.onSelect.AddListener((elem) =>
        {
            string name = ((DropdownTextElement)elem).Text;
            onDeviceSelected?.Invoke(name);
            _dropdownResultTextMesh.text = name;
        });
        _activeButton.onValueChanged.AddListener((value) => onActiveButtonClicked(value));
        _dropdownButton.onClick.AddListener(_dropdown.Toggle);
        _reloadButton.onClick.AddListener(() => RefreshDevices(GetDevicesFunction()));
        RefreshDevices(GetDevicesFunction());
    }
    public void OnActive(bool active, string currentDevice)
    {
        _activeButton.SetIsOnWithoutNotify(active);
        _dropdownResultTextMesh.text = currentDevice;
    }
    void RefreshDevices(string[] devices)
    {
        string selectedDevice = "";
        if (_dropdown.TryGetCurrentItem(out DropdownElement elem))
        {
            selectedDevice = ((DropdownTextElement)elem).Text;
        }
        _dropdown.ClearElements();
        for(int i = 0; i < devices.Length; i++)
        {
            string device = devices[i];
            var elm = _dropdown.AddElementPrefab<DropdownTextElement>(_dropdownElementPrefab);
            elm.Text = device;
            if (device == selectedDevice)
            {
                _dropdown.ForceSelect(i);
            }
        }
    }
}