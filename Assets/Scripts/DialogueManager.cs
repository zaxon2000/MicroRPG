using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages the UI Toolkit dialogue panel and interact prompt.
/// Attach to a GameObject that also has a UIDocument component.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class DialogueManager : MonoBehaviour
{
    private static DialogueManager _instance;

    /// <summary>
    /// Singleton accessor.
    /// </summary>
    public static DialogueManager Instance => _instance;

    /// <summary>
    /// True while a dialogue conversation is active.
    /// </summary>
    public bool IsDialogueActive { get; private set; }

    /// <summary>
    /// Raised when a dialogue conversation ends.
    /// </summary>
    public event Action OnDialogueEnded;

    // UI references
    private UIDocument _uiDocument;
    private VisualElement _root;
    private VisualElement _interactPrompt;
    private VisualElement _dialogueOverlay;
    private Label _speakerLabel;
    private Label _dialogueTextLabel;
    private VisualElement _responsesContainer;

    // State
    private DialogueData _currentData;
    private Transform _promptTarget;
    private Camera _mainCamera;
    private Action<int> _onResponseChosen;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        _uiDocument = GetComponent<UIDocument>();
        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        var root = _uiDocument.rootVisualElement;
        if (root == null) return;

        _root = root;
        _interactPrompt = root.Q<VisualElement>("InteractPrompt");
        _dialogueOverlay = root.Q<VisualElement>("DialogueOverlay");
        _speakerLabel = root.Q<Label>("SpeakerName");
        _dialogueTextLabel = root.Q<Label>("DialogueText");
        _responsesContainer = root.Q<VisualElement>("ResponsesContainer");

        // Make the interact prompt clickable
        _interactPrompt.RegisterCallback<ClickEvent>(OnPromptClicked);

        HidePrompt();
        HideDialogue();
    }

    private void OnDisable()
    {
        if (_interactPrompt != null)
            _interactPrompt.UnregisterCallback<ClickEvent>(OnPromptClicked);
    }

    private void LateUpdate()
    {
        UpdatePromptPosition();
    }

    // ───────────────────── Interact Prompt ─────────────────────

    /// <summary>
    /// Shows the "E" prompt above the given world-space target.
    /// </summary>
    public void ShowPrompt(Transform target)
    {
        _promptTarget = target;
        _interactPrompt.style.display = DisplayStyle.Flex;
    }

    /// <summary>
    /// Hides the "E" interact prompt.
    /// </summary>
    public void HidePrompt()
    {
        _interactPrompt.style.display = DisplayStyle.None;
        _promptTarget = null;
    }

    private void UpdatePromptPosition()
    {
        if (_promptTarget == null || _mainCamera == null || _interactPrompt == null) return;
        if (_interactPrompt.resolvedStyle.display == DisplayStyle.None) return;

        // Offset above the NPC's head
        Vector3 worldPos = _promptTarget.position + Vector3.up * 0.8f;
        Vector2 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

        // Flip Y: screen coordinates have origin at bottom-left, UI Toolkit at top-left
        screenPos.y = Screen.height - screenPos.y;

        // Convert screen position to panel position
        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(
            _root.panel, new Vector2(screenPos.x, screenPos.y));

        _interactPrompt.style.left = panelPos.x;
        _interactPrompt.style.top = panelPos.y;
    }

    private void OnPromptClicked(ClickEvent evt)
    {
        // Trigger the interact on the current NPC
        if (_promptTarget != null)
        {
            var npcDialogue = _promptTarget.GetComponent<NPCDialogue>();
            if (npcDialogue != null)
                npcDialogue.StartDialogue();
        }
    }

    // ───────────────────── Dialogue Panel ─────────────────────

    /// <summary>
    /// Begins a dialogue conversation with the given data.
    /// </summary>
    public void StartDialogue(DialogueData data)
    {
        if (data == null || data.nodes.Count == 0)
        {
            Debug.LogError("[DialogueManager] Cannot start dialogue: data is null or empty.");
            return;
        }

        _currentData = data;
        IsDialogueActive = true;
        HidePrompt();

        ShowNode(0);
    }

    /// <summary>
    /// Ends the current dialogue conversation.
    /// </summary>
    public void EndDialogue()
    {
        IsDialogueActive = false;
        _currentData = null;
        HideDialogue();
        OnDialogueEnded?.Invoke();
    }

    private void ShowNode(int nodeIndex)
    {
        if (_currentData == null || nodeIndex < 0 || nodeIndex >= _currentData.nodes.Count)
        {
            EndDialogue();
            return;
        }

        DialogueNode node = _currentData.nodes[nodeIndex];

        _speakerLabel.text = node.speakerName;
        _dialogueTextLabel.text = node.text;

        // Clear previous responses
        _responsesContainer.Clear();

        if (node.responses.Count > 0)
        {
            // Add response buttons
            foreach (DialogueResponse response in node.responses)
            {
                var btn = new Button();
                btn.text = response.responseText;
                btn.AddToClassList("dialogue-response-btn");

                int nextIndex = response.nextNodeIndex;
                btn.clicked += () => OnResponseSelected(nextIndex);

                _responsesContainer.Add(btn);
            }
        }
        else
        {
            // Terminal node: add a "click to close" button
            var continueBtn = new Button();
            continueBtn.text = "[Click to close]";
            continueBtn.AddToClassList("dialogue-continue-btn");
            continueBtn.clicked += EndDialogue;
            _responsesContainer.Add(continueBtn);
        }

        _dialogueOverlay.style.display = DisplayStyle.Flex;
    }

    private void OnResponseSelected(int nextNodeIndex)
    {
        if (nextNodeIndex < 0)
        {
            EndDialogue();
            return;
        }
        ShowNode(nextNodeIndex);
    }

    private void HideDialogue()
    {
        if (_dialogueOverlay != null)
            _dialogueOverlay.style.display = DisplayStyle.None;
    }
}
