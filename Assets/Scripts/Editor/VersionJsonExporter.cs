using UnityEngine;
using UnityEditor;
using System.IO;

public class VersionJsonExporter : EditorWindow
{
    // デフォルトのダウンロードURL（必要に応じて書き換えてください）
    private string downloadUrl = "https://your-site.com/download";
    private string exportFileName = "version.json";

    [MenuItem("Tools/Export version.json")]
    public static void ShowWindow()
    {
        GetWindow<VersionJsonExporter>("Export Version JSON");
    }

    void OnGUI()
    {
        GUILayout.Label("Export Server Version File", EditorStyles.boldLabel);

        // PlayerSettingsからバージョンを取得して表示
        string currentVersion = PlayerSettings.bundleVersion;
        EditorGUILayout.HelpBox($"Current Player Version: {currentVersion}", MessageType.Info);

        EditorGUILayout.Space();

        // ダウンロードURLの入力
        GUILayout.Label("Download URL (for users to click):");
        downloadUrl = EditorGUILayout.TextField(downloadUrl);

        EditorGUILayout.Space();

        if (GUILayout.Button("Export version.json"))
        {
            Export(currentVersion, downloadUrl);
        }
    }

    private void Export(string version, string url)
    {
        // データ作成
        var data = new VersionData
        {
            version = version,
            dllink = url
        };

        // JSON変換
        string json = JsonUtility.ToJson(data, true);

        // プロジェクトのルートフォルダに保存
        string fullPath = Path.Combine(Application.dataPath, "../", exportFileName);
        File.WriteAllText(fullPath, json);

        Debug.Log($"[Export] '{exportFileName}' created successfully!\nPath: {fullPath}\nVersion: {version}");
        
        // 保存先のフォルダを開く（Mac/Win対応）
        EditorUtility.RevealInFinder(fullPath);
    }
}