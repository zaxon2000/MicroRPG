using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Editor window that hosts the dialogue graph view for visual editing of DialogueData assets.
/// Open via Window > Dialogue > Dialogue Editor or by double-clicking a DialogueData asset.
/// </summary>
public class DialogueEditorWindow : EditorWindow
{
    private DialogueGraphView _graphView;
    private DialogueData _currentData;
    private Label _assetNameLabel;

    private const string WindowTitle = "Dialogue Editor";
    private static readonly Vector2 MinWindowSize = new Vector2(700, 450);

    // ───────────────────── Open Hooks ─────────────────────

    /// <summary>
    /// Opens the editor window from the top menu bar.
    /// </summary>
    [MenuItem("Window/Dialogue/Dialogue Editor")]
    public static void OpenWindow()
    {
        var window = GetWindow<DialogueEditorWindow>(WindowTitle);
        window.minSize = MinWindowSize;
    }

    /// <summary>
    /// Opens the editor automatically when double-clicking a DialogueData asset.
    /// </summary>
    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        var asset = EditorUtility.InstanceIDToObject(instanceID) as DialogueData;
        if (asset == null) return false;

        var window = GetWindow<DialogueEditorWindow>(WindowTitle);
        window.minSize = MinWindowSize;
        window.LoadDialogueData(asset);
        return true;
    }

    // ───────────────────── Lifecycle ─────────────────────

    private void OnEnable()
    {
        BuildUI();
    }

    private void OnDisable()
    {
        if (_graphView != null)
            rootVisualElement.Remove(_graphView);
    }

    // ───────────────────── UI Construction ─────────────────────

    private void BuildUI()
    {
        var root = rootVisualElement;

        // ── Toolbar ──
        var toolbar = new VisualElement();
        toolbar.style.flexDirection = FlexDirection.Row;
        toolbar.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
        toolbar.style.paddingLeft = 8;
        toolbar.style.paddingRight = 8;
        toolbar.style.paddingTop = 4;
        toolbar.style.paddingBottom = 4;
        toolbar.style.alignItems = Align.Center;

        _assetNameLabel = new Label("No asset loaded");
        _assetNameLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
        _assetNameLabel.style.flexGrow = 1;
        _assetNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        toolbar.Add(_assetNameLabel);

        // Load button — pick a DialogueData asset from the project
        var loadBtn = new Button(OnLoad) { text = "Load" };
        toolbar.Add(loadBtn);

        var newNodeBtn = new Button(OnAddNode) { text = "New Node" };
        newNodeBtn.style.marginLeft = 4;
        toolbar.Add(newNodeBtn);

        var saveBtn = new Button(OnSave) { text = "Save" };
        saveBtn.style.marginLeft = 4;
        saveBtn.style.backgroundColor = new Color(0.2f, 0.45f, 0.2f, 0.9f);
        toolbar.Add(saveBtn);

        root.Add(toolbar);

        // ── Graph View ──
        _graphView = new DialogueGraphView();
        root.Add(_graphView);

        // Reload previously opened asset if the window was just re-enabled
        if (_currentData != null)
        {
            LoadDialogueData(_currentData);
        }
    }

    // ───────────────────── Public API ─────────────────────

    /// <summary>
    /// Loads the specified DialogueData asset into the graph editor.
    /// </summary>
    public void LoadDialogueData(DialogueData data)
    {
        _currentData = data;
        _assetNameLabel.text = data != null ? data.name : "No asset loaded";
        _graphView?.LoadFromData(data);
    }

    // ───────────────────── Toolbar Callbacks ─────────────────────

    private void OnLoad()
    {
        string path = EditorUtility.OpenFilePanel("Select DialogueData", "Assets", "asset");
        if (string.IsNullOrEmpty(path)) return;

        // Convert absolute path to project-relative path
        if (path.StartsWith(Application.dataPath))
        {
            path = "Assets" + path.Substring(Application.dataPath.Length);
        }

        var data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        if (data != null)
        {
            LoadDialogueData(data);
        }
        else
        {
            Debug.LogWarning("[DialogueEditor] Selected file is not a DialogueData asset.");
        }
    }

    private void OnAddNode()
    {
        if (_graphView == null) return;

        // Place the new node roughly in the center of the visible area
        var center = _graphView.contentViewContainer.WorldToLocal(
            _graphView.LocalToWorld(_graphView.layout.center));

        _graphView.CreateNode(center, "NPC", "", false);
    }

    private void OnSave()
    {
        if (_currentData == null)
        {
            Debug.LogWarning("[DialogueEditor] No DialogueData loaded. Nothing to save.");
            return;
        }

        _graphView?.SaveToData(_currentData);
    }

    // ───────────────────── Keyboard Shortcuts ─────────────────────

    private void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.S &&
            (e.control || e.command))
        {
            OnSave();
            e.Use();
        }
    }
}
