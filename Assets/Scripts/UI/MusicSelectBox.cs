using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MusicSelectBox : MonoBehaviour
{
    [SerializeField] private Image _bkg;
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _titleTextMesh;
    [SerializeField] private TextMeshProUGUI _composerTextMesh;
    [SerializeField] private TextMeshProUGUI _lengthTextMesh;
    private AudioClip _audioClip;
    public AudioClip Clip => _audioClip;
    public Button TargetButton => _button;
    public UnityEvent<AudioClip> OnClick = new();

    void Start()
    {
        _button.onClick.AddListener(() => OnClick?.Invoke(Clip));
    }
    public void AssignAudioClip(AudioClip clip, string title, string composer)
    {
        _audioClip = clip;
        _titleTextMesh.text = title;
        var sec = clip.length;
        TimeSpan ts = TimeSpan.FromSeconds(sec);
        _lengthTextMesh.text = ts.ToString(@"mm\:ss\:ff");
        if (!string.IsNullOrEmpty(composer))
        {
            _composerTextMesh.text = composer;
        }
    }
    public void SetState(float normalizedTime)
    {
        _bkg.fillAmount = normalizedTime;
    }
}