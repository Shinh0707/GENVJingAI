using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class UpdateChecker : MonoBehaviour
{
    // GitHubのRawファイルのURL
    [SerializeField] string versionInfoUrl = "https://raw.githubusercontent.com/User/Repo/main/version.json";
    VersionData _latest = null;
    bool _hasUpdate = false;
    bool _checked = false;
    public UnityEvent<bool,VersionData> onChecked = new();
    public VersionData LatestVersionData => _latest;
    public bool HasUpdate => _hasUpdate;
    void Start()
    {
        if (!_checked)
        {
            _checked = true;
            StartCoroutine(CheckForUpdate());
        }
    }

    IEnumerator CheckForUpdate()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(versionInfoUrl))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var json = JsonUtility.FromJson<VersionData>(www.downloadHandler.text);
                string serverVersion = json.version;

                // バージョン比較 (単純な文字列比較やSystem.Versionを使う)
                if (IsNewer(serverVersion, Application.version))
                {
                    Debug.Log($"Update available: {serverVersion}");
                    _hasUpdate = true;
                    _latest = json;
                    // ここで「アップデートがあります！」というUIを表示する
                    // ボタンが押されたら Application.OpenURL(url); する
                }
            }
        }
        onChecked?.Invoke(_hasUpdate,_latest);
        gameObject.SetActive(_hasUpdate);
    }

    bool IsNewer(string serverVer, string localVer)
    {
        // System.Versionクラスを使うと "1.0.2" vs "1.0.10" などを正しく比較できます
        return new System.Version(serverVer) > new System.Version(localVer);
    }


}