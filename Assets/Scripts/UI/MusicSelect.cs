using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using SFB;
using System.Collections;
using System.IO;

public class MusicSelect : MonoBehaviour
{
    [SerializeField] private FEPAgent _agent;
    [SerializeField] private GameObject _selectMusicBoxPrefab;
    [SerializeField] private RectTransform _selectMusicBoxContainer;
    
    [Header("Buttons")]
    [SerializeField] private Button _microphoneButton;
    [SerializeField] private Button _restartLearnButton;
    [SerializeField] private Button _pauseLearnButton;
    [SerializeField] private Button _randomSelectButton;
    [SerializeField] private Button _importButton;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI _microphoneButtonLabel;
    [SerializeField] private TextMeshProUGUI _pauseLearnButtonLabel;
    [SerializeField] private TextMeshProUGUI _randomSelectButtonLabel;
    
    [SerializeField] private MusicPlayer _musicPlayer;

    private bool _randomSelect = false;
    private int _selected = -1;
    private List<MusicSelectBox> _musics;

    void Start()
    {
        // ... (既存のリスナー設定はそのまま) ...
        _microphoneButton.onClick.AddListener(() => {
                _agent.SetMicrophoneActive(!_agent.UseMicrophone);
                _microphoneButtonLabel.text = _agent.UseMicrophone ? "Stop Mic" : "Start Mic";
                foreach(var m in _musics) m.TargetButton.enabled = !_agent.UseMicrophone;
                _randomSelectButton.enabled = !_agent.UseMicrophone;
                _importButton.enabled = !_agent.UseMicrophone; // マイク中はインポートも不可にする
                if (_selected != -1) {
                    _selected = -1;
                    if(_selected >= 0 && _selected < _musics.Count) _musics[_selected].SetState(0);
                }
            }
        );

        _restartLearnButton.onClick.AddListener(() => {
                RestartLearn();
                _pauseLearnButtonLabel.text = _agent.IsPaused ? "Resume" : "Pause";
            }
        );

        _pauseLearnButton.onClick.AddListener(() => {
                _agent.SetPause(!_agent.IsPaused);
                _pauseLearnButtonLabel.text = _agent.IsPaused ? "Resume" : "Pause";
            }
        );

        _randomSelectButton.onClick.AddListener(() => {
                _randomSelect = !_randomSelect;
                _randomSelectButtonLabel.text = _randomSelect ? "Manual" : "Random";
            }
        );

        // ★追加: インポートボタンのリスナー
        if (_importButton != null)
        {
            _importButton.onClick.AddListener(OnClickImport);
        }

        _musicPlayer.onPause.AddListener((value) => {
            var auds = _agent.GetAudioSource();
            if (value) auds.Pause();
            else if(!auds.isPlaying) auds.UnPause();
        });

        _musicPlayer.onTimeChanged.AddListener((value) => {
            if(_selected != -1) {
                var auds = _agent.GetAudioSource();
                var nt = Mathf.Clamp01(value);
                var msc = _musics[_selected];
                var ns = (int)(nt * msc.Clip.samples);
                if (!Mathf.Approximately(auds.timeSamples,ns)){
                    auds.timeSamples = ns;
                    SetTime(auds);
                }
            }
        });

        // Load default resources
        AudioClip[] clips = Resources.LoadAll<AudioClip>("");
        _musics = new();
        foreach (var clip in clips)
        {
            AddAudioClip(clip);
        }
    }

    // ★追加: ファイルダイアログを開く処理
    private void OnClickImport()
    {
        // 許可する拡張子
        var extensions = new [] {
            new ExtensionFilter("Audio Files", "mp3", "wav", "ogg", "aiff"), 
        };

        // SFBでファイル選択 (マルチセレクトfalse)
        var paths = StandaloneFileBrowser.OpenFilePanel("Import Audio", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            StartCoroutine(LoadAudioClip(paths[0]));
        }
    }
    private IEnumerator LoadAudioClip(string path)
    {
        string url = "file://" + path;
        AudioType audioType = AudioType.UNKNOWN;
        string ext = Path.GetExtension(path).ToLower();
        switch (ext)
        {
            case ".mp3": audioType = AudioType.MPEG; break;
            case ".wav": audioType = AudioType.WAV; break;
            case ".ogg": audioType = AudioType.OGGVORBIS; break;
            case ".aiff": audioType = AudioType.AIFF; break;
        }

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Failed to load audio: {www.error}");
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip != null)
                {
                    // ファイル名をクリップ名にする
                    clip.name = Path.GetFileNameWithoutExtension(path);
                    
                    // リストに追加
                    AddAudioClip(clip);
                    
                    Debug.Log($"Imported: {clip.name}");
                }
                else
                {
                    Debug.LogError("Downloaded data could not be converted to AudioClip.");
                }
            }
        }
    }

    void AddAudioClip(AudioClip clip)
    {
        string title = clip.name;
        string composer = "Imported";
        if (title.Contains("_"))
        {
            string[] parts = title.Split('_');
            if (parts.Length >= 2)
            {
                composer = parts[parts.Length - 1];
                title = string.Join("_", parts, 0, parts.Length - 1);
            }
        }
        
        var boxObj = Instantiate(_selectMusicBoxPrefab, _selectMusicBoxContainer);
        if (boxObj.TryGetComponent<MusicSelectBox>(out var box))
        {
            _musics.Add(box);
            box.AssignAudioClip(clip, title, composer);
            int j = _musics.Count - 1;
            box.OnClick.AddListener((_) => SelectAudio(j));
        }
    }

    void RestartLearn()
    {
        if (_agent.IsReady)
        {
            _agent.ResetLearn();
        }
    }

    void SelectAudio(int id)
    {
        if (_selected != -1)
        {
            _musics[_selected].SetState(0);
        }
        _selected = id;
        if (_selected == -1) return;
        _agent.SetAudio(_musics[id].Clip);
        _musics[_selected].SetState(0);
        _musicPlayer.NormalizedValue = 0;
        _musicPlayer.SetPauseWithputNotify(false);
    }

    void SetTime(AudioSource auds)
    {
        if(_selected != -1)
        {
            var msc = _musics[_selected];
            var nt = auds.time/msc.Clip.length;
            msc.SetState(nt);
            _musicPlayer.NormalizedValue = nt;
        }
    }

    void Update()
    {
        if (_agent.UseMicrophone) return;
        var auds = _agent.GetAudioSource();
        if (_selected == -1)
        {
            if (!auds.isPlaying)
            {
                if (_randomSelect)
                {
                    SelectAudio(Random.Range(0,_musics.Count-1));
                    RestartLearn();
                }
            }
        }
        else
        {
            var msc = _musics[_selected];
            if (auds.isPlaying)
            {
                SetTime(auds);
            }
            else
            {
                if (auds.time >= msc.Clip.length)
                {
                    if (_randomSelect)
                    {
                        SelectAudio(Random.Range(0,_musics.Count-1));
                        RestartLearn();
                    }
                }
            }
        }
    }
}