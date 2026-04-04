using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Manages the quest log UI panel built with UI Toolkit.
/// Toggle visibility with a configurable key (default: J).
/// Displays active quests and their objective progress.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class QuestLogUI : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Key to toggle the quest log.")]
    [SerializeField] private Key toggleKey = Key.J;

    private UIDocument _uiDocument;
    private VisualElement _root;
    private VisualElement _questLogPanel;
    private VisualElement _questListContainer;
    private Label _noQuestsLabel;
    private Button _closeButton;

    private QuestManager _questManager;
    private bool _isVisible;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        VisualElement root = _uiDocument.rootVisualElement;
        if (root == null) return;

        _root = root;
        _questLogPanel = root.Q<VisualElement>("QuestLogPanel");
        _questListContainer = root.Q<VisualElement>("QuestListContainer");
        _noQuestsLabel = root.Q<Label>("NoQuestsLabel");
        _closeButton = root.Q<Button>("QuestLogCloseBtn");

        if (_closeButton != null)
            _closeButton.clicked += Hide;

        Hide();
    }

    private void Start()
    {
        _questManager = QuestManager.Instance;
        if (_questManager == null)
        {
            Debug.LogWarning("[QuestLogUI] No QuestManager found.");
            return;
        }

        _questManager.OnQuestStateChanged += OnQuestStateChanged;
        _questManager.OnObjectiveProgressed += OnObjectiveProgressed;
    }

    private void OnDisable()
    {
        if (_closeButton != null)
            _closeButton.clicked -= Hide;

        if (_questManager != null)
        {
            _questManager.OnQuestStateChanged -= OnQuestStateChanged;
            _questManager.OnObjectiveProgressed -= OnObjectiveProgressed;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            if (_isVisible) Hide();
            else Show();
        }
    }

    // ───────────────────── Visibility ─────────────────────

    /// <summary>
    /// Shows the quest log and refreshes its contents.
    /// </summary>
    public void Show()
    {
        _isVisible = true;
        _questLogPanel.style.display = DisplayStyle.Flex;
        RefreshQuestList();
    }

    /// <summary>
    /// Hides the quest log.
    /// </summary>
    public void Hide()
    {
        _isVisible = false;
        if (_questLogPanel != null)
            _questLogPanel.style.display = DisplayStyle.None;
    }

    // ───────────────────── Refresh ─────────────────────

    private void RefreshQuestList()
    {
        if (_questListContainer == null || _questManager == null) return;

        _questListContainer.Clear();

        List<QuestData> tracked = _questManager.GetTrackedQuests();

        if (tracked.Count == 0)
        {
            if (_noQuestsLabel != null)
                _noQuestsLabel.style.display = DisplayStyle.Flex;
            return;
        }

        if (_noQuestsLabel != null)
            _noQuestsLabel.style.display = DisplayStyle.None;

        foreach (QuestData quest in tracked)
        {
            VisualElement questEntry = CreateQuestEntry(quest);
            _questListContainer.Add(questEntry);
        }
    }

    private VisualElement CreateQuestEntry(QuestData quest)
    {
        VisualElement entry = new VisualElement();
        entry.AddToClassList("quest-entry");

        // Quest title
        QuestState state = _questManager.GetQuestState(quest.questId);
        string stateTag = state == QuestState.ReadyToComplete ? " [READY]" : "";

        Label title = new Label($"{quest.questTitle}{stateTag}");
        title.AddToClassList("quest-entry__title");
        entry.Add(title);

        // Quest description
        if (!string.IsNullOrEmpty(quest.questDescription))
        {
            Label desc = new Label(quest.questDescription);
            desc.AddToClassList("quest-entry__desc");
            entry.Add(desc);
        }

        // Objectives
        List<QuestObjective> objectives = _questManager.GetActiveObjectives(quest.questId);
        if (objectives != null)
        {
            foreach (QuestObjective obj in objectives)
            {
                string checkMark = obj.IsComplete ? "\u2713" : "\u2022";
                string progress = obj.objectiveType == QuestObjectiveType.ReachLevel
                    ? $"{checkMark} {obj.description}"
                    : $"{checkMark} {obj.description} ({obj.currentAmount}/{obj.requiredAmount})";

                Label objLabel = new Label(progress);
                objLabel.AddToClassList(obj.IsComplete ? "quest-objective--done" : "quest-objective");
                entry.Add(objLabel);
            }
        }

        return entry;
    }

    // ───────────────────── Event Handlers ─────────────────────

    private void OnQuestStateChanged(QuestData quest, QuestState newState)
    {
        if (_isVisible)
            RefreshQuestList();
    }

    private void OnObjectiveProgressed(QuestData quest, QuestObjective objective)
    {
        if (_isVisible)
            RefreshQuestList();
    }
}
