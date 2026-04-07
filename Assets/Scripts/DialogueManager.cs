using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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
    /// The index of the last node that was displayed before the dialogue ended.
    /// Reset to -1 at the start of each new conversation.
    /// </summary>
    public int LastNodeIndex { get; private set; } = -1;

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
    private Transform _playerTransform;

    // All NPCs currently in interact range, evaluated each frame for closest
    private readonly HashSet<Transform> _promptCandidates = new HashSet<Transform>();

    // Ordered list of next-node indices for the currently displayed responses.
    // Index 0 = key "1", index 1 = key "2", etc.
    private readonly List<int> _activeResponseNextIndices = new List<int>();

    // When there is exactly one response (or a terminal node), pressing Enter
    // will trigger it. -2 means no single-proceed action is available.
    private const int NoSingleProceed = -2;
    private int _singleProceedNextIndex = NoSingleProceed;

    // Number keys 1-9 paired with their Input System Key enum values
    private static readonly Key[] NumberKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
        Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
    };

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

        Player player = FindFirstObjectByType<Player>();
        if (player != null)
            _playerTransform = player.transform;
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
        UpdateClosestPromptCandidate();
        UpdatePromptPosition();
        HandleInteractInput();
        HandleNumberKeyInput();
    }

    // ───────────────────── Interact Prompt ─────────────────────

    /// <summary>
    /// Registers an NPC as a candidate for the interact prompt.
    /// The closest candidate is shown each frame automatically.
    /// </summary>
    public void RegisterPromptCandidate(Transform npc)
    {
        if (npc != null)
            _promptCandidates.Add(npc);
    }

    /// <summary>
    /// Removes an NPC from the interact prompt candidates.
    /// </summary>
    public void UnregisterPromptCandidate(Transform npc)
    {
        _promptCandidates.Remove(npc);

        // If this was the active target, clear the prompt immediately
        if (_promptTarget == npc)
            ClearPrompt();
    }

    /// <summary>
    /// Force-hides the prompt and clears all state. Used when dialogue starts.
    /// </summary>
    public void HidePrompt()
    {
        _promptCandidates.Clear();
        ClearPrompt();
    }

    private void ClearPrompt()
    {
        _promptTarget = null;
        if (_interactPrompt != null)
            _interactPrompt.style.display = DisplayStyle.None;
    }

    private void UpdateClosestPromptCandidate()
    {
        if (IsDialogueActive || _promptCandidates.Count == 0)
        {
            if (!IsDialogueActive && _promptTarget != null)
                ClearPrompt();
            return;
        }

        // Pick the closest candidate to the player each frame
        Transform closest = null;
        float closestDist = float.MaxValue;

        foreach (Transform candidate in _promptCandidates)
        {
            if (candidate == null) continue;

            float dist = _playerTransform != null
                ? Vector2.Distance(_playerTransform.position, candidate.position)
                : 0f;

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = candidate;
            }
        }

        if (closest == _promptTarget) return;

        _promptTarget = closest;
        _interactPrompt.style.display = _promptTarget != null ? DisplayStyle.Flex : DisplayStyle.None;
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

    /// <summary>
    /// Checks for the interact key (E) and triggers dialogue on the closest NPC.
    /// Centralised here so only one NPC responds even when multiple are in range.
    /// </summary>
    private void HandleInteractInput()
    {
        if (IsDialogueActive) return;
        if (_promptTarget == null) return;
        if (Keyboard.current == null || !Keyboard.current[Key.E].wasPressedThisFrame) return;

        TriggerInteractOnTarget();
    }

    /// <summary>
    /// Returns true if the given transform is the current interact-prompt target.
    /// NPC scripts can use this to avoid duplicate handling.
    /// </summary>
    public bool IsCurrentPromptTarget(Transform t) => _promptTarget == t;

    private void TriggerInteractOnTarget()
    {
        if (_promptTarget == null) return;

        var questGiver = _promptTarget.GetComponent<NPCQuestGiver>();
        if (questGiver != null)
        {
            questGiver.StartDialogue();
            return;
        }

        var npcDialogue = _promptTarget.GetComponent<NPCDialogue>();
        if (npcDialogue != null)
            npcDialogue.StartDialogue();
    }

    private void OnPromptClicked(ClickEvent evt)
    {
        // Trigger the interact on the current NPC (supports both dialogue-only and quest-giving NPCs)
        if (_promptTarget != null)
        {
            var questGiver = _promptTarget.GetComponent<NPCQuestGiver>();
            if (questGiver != null)
            {
                questGiver.StartDialogue();
                return;
            }

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
        LastNodeIndex = -1;
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
        _activeResponseNextIndices.Clear();
        _singleProceedNextIndex = NoSingleProceed;
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
        LastNodeIndex = nodeIndex;

        _speakerLabel.text = node.speakerName;
        _dialogueTextLabel.text = node.text;

        // Clear previous responses
        _responsesContainer.Clear();

        if (node.responses.Count > 0)
        {
            // Add response buttons and record their next-node indices for hotkey use
            _activeResponseNextIndices.Clear();
            foreach (DialogueResponse response in node.responses)
            {
                var btn = new Button();
                btn.text = response.responseText;
                btn.AddToClassList("dialogue-response-btn");

                int nextIndex = response.nextNodeIndex;
                btn.clicked += () => OnResponseSelected(nextIndex);

                _responsesContainer.Add(btn);
                _activeResponseNextIndices.Add(nextIndex);
            }

            // Allow Enter to auto-proceed when there is only one option
            _singleProceedNextIndex = node.responses.Count == 1
                ? node.responses[0].nextNodeIndex
                : NoSingleProceed;
        }
        else
        {
            // Terminal node: clear hotkeys and add a dismiss button
            _activeResponseNextIndices.Clear();
            var continueBtn = new Button();
            continueBtn.text = "[Enter] Close";
            continueBtn.AddToClassList("dialogue-continue-btn");
            continueBtn.clicked += EndDialogue;
            _responsesContainer.Add(continueBtn);

            // Enter always dismisses a terminal node
            _singleProceedNextIndex = -1;
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

    /// <summary>
    /// Polls digit keys 1-9 and selects the matching response if one is active.
    /// When only one response (or a terminal node) is showing, Enter also proceeds.
    /// </summary>
    private void HandleNumberKeyInput()
    {
        if (!IsDialogueActive) return;
        if (Keyboard.current == null) return;

        // Enter proceeds when there is a single option or a terminal node
        if (_singleProceedNextIndex != NoSingleProceed &&
            Keyboard.current[Key.Enter].wasPressedThisFrame)
        {
            OnResponseSelected(_singleProceedNextIndex);
            return;
        }

        if (_activeResponseNextIndices.Count == 0) return;

        int maxKey = Mathf.Min(_activeResponseNextIndices.Count, NumberKeys.Length);
        for (int i = 0; i < maxKey; i++)
        {
            if (Keyboard.current[NumberKeys[i]].wasPressedThisFrame)
            {
                OnResponseSelected(_activeResponseNextIndices[i]);
                return;
            }
        }
    }

    private void HideDialogue()
    {
        if (_dialogueOverlay != null)
            _dialogueOverlay.style.display = DisplayStyle.None;
    }
}
