#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
/// <summary>
/// <see cref="MicrophoneDevice"/> の描画を行うクラス.
/// 使用可能なマイクデバイスを取得し, ドロップダウンリストとして表示する.
/// </summary>
[CustomPropertyDrawer(typeof(MicrophoneDeviceAttribute))]
public class MicrophoneDeviceDrawer : PropertyDrawer
{
    /// <summary>
    /// GUIを描画する.
    /// </summary>
    /// <param name="position">描画領域の矩形</param>
    /// <param name="property">対象のプロパティ</param>
    /// <param name="label">ラベル</param>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 文字列型以外には適用しない
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        // 取得時にアロケーションが発生するが, Unity APIの仕様上回避不可であるため許容する.
        string[] devices = Microphone.devices;

        // デバイスが存在しない場合の処理
        if (devices.Length == 0)
        {
            EditorGUI.LabelField(position, label.text, "No Microphone found");
            return;
        }

        // 現在設定されている値のインデックスを探索する.
        // LINQ (Where, Select等) は使用せず, forループで処理する.
        int currentIndex = 0;
        string currentName = property.stringValue;

        for (int i = 0; i < devices.Length; ++i)
        {
            if (devices[i] == currentName)
            {
                currentIndex = i;
                break;
            }
        }

        // ポップアップを表示し, 選択されたインデックスを取得する.
        int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, devices);

        // 値が変更された場合のみプロパティを更新する.
        if (selectedIndex >= 0 && selectedIndex < devices.Length)
        {
            string selectedName = devices[selectedIndex];
            if (property.stringValue != selectedName)
            {
                property.stringValue = selectedName;
            }
        }
    }
}
#endif