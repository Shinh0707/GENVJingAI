using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class BuildScript
{
    // アプリ名（ここを自分のアプリ名に変えてください）
    static string AppName = "AI VJing";

    [MenuItem("Build/Build All Platforms")]
    public static void BuildAll()
    {
        BuildWindows();
        BuildMac();
    }

    [MenuItem("Build/Build Windows (x64)")]
    public static void BuildWindows()
    {
        string buildPath = $"Builds/Windows/{AppName}.exe";
        BuildProject(BuildTarget.StandaloneWindows64, buildPath);
    }

    [MenuItem("Build/Build macOS (Universal)")]
    public static void BuildMac()
    {
        string buildPath = $"Builds/macOS/{AppName}.app";
        BuildProject(BuildTarget.StandaloneOSX, buildPath);
    }

    static void BuildProject(BuildTarget buildTarget, string buildPath)
    {
        // ビルド対象のシーンを取得（Build Settingsでチェックが入っているもの）
        string[] levels = GetEnabledScenes();

        // ビルドオプションの設定
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = levels;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = buildTarget;
        buildPlayerOptions.options = BuildOptions.None;

        // ビルド実行
        Debug.Log($"Building for {buildTarget} to {buildPath}...");
        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        // 結果確認
        UnityEditor.Build.Reporting.BuildSummary summary = report.summary;
        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {summary.totalSize / 1024 / 1024} MB");
        }
        else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
        {
            Debug.LogError("Build failed");
        }
    }

    static string[] GetEnabledScenes()
    {
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
            }
        }
        return scenes.ToArray();
    }
}