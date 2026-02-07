using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(WebCamRenderer))]
public class WebCamRendererEditor : Editor
{
    SerializedProperty deviceNameProp;
    SerializedProperty renderTextureProp;
    SerializedProperty requestedFPSProp;

    void OnEnable()
    {
        deviceNameProp = serializedObject.FindProperty("deviceName");
        renderTextureProp = serializedObject.FindProperty("renderTexture");
        requestedFPSProp = serializedObject.FindProperty("requestedFPS");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        WebCamRenderer renderer = (WebCamRenderer)target;

        // Device Selection
        string[] devices = WebCamTexture.devices.Select(d => d.name).ToArray();
        int selectedIndex = -1;

        if (!string.IsNullOrEmpty(deviceNameProp.stringValue))
        {
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i] == deviceNameProp.stringValue)
                {
                    selectedIndex = i;
                    break;
                }
            }
        }

        if (devices.Length > 0)
        {
            int newIndex = EditorGUILayout.Popup("Device", selectedIndex, devices);
            if (newIndex >= 0 && newIndex < devices.Length)
            {
                deviceNameProp.stringValue = devices[newIndex];
            }
            else if (selectedIndex == -1 && devices.Length > 0) 
            {
                 // Default to first device if none selected or current not found
                 // But maybe better to leave empty if user hasn't selected? 
                 // The runtime script defaults to first if empty. 
                 // Let's just show "None" or similar if -1? 
                 // Popup doesn't support -1 well if it's not in the list.
                 // If selectedIndex is -1, we can show a specific label or just let the user pick.
                 // Actually, if we pass -1 to Popup, it shows nothing selected.
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No WebCam devices found.", MessageType.Warning);
        }

        EditorGUILayout.PropertyField(renderTextureProp);
        EditorGUILayout.PropertyField(requestedFPSProp);

        serializedObject.ApplyModifiedProperties();
    }
}
