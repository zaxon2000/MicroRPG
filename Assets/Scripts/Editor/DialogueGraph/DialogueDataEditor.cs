using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom inspector for DialogueData that provides a button to open the visual node editor.
/// </summary>
[CustomEditor(typeof(DialogueData))]
public class DialogueDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open in Dialogue Editor", GUILayout.Height(32)))
        {
            var data = (DialogueData)target;
            var window = EditorWindow.GetWindow<DialogueEditorWindow>("Dialogue Editor");
            window.minSize = new Vector2(700, 450);
            window.LoadDialogueData(data);
        }

        EditorGUILayout.Space(8);
        DrawDefaultInspector();
    }
}
