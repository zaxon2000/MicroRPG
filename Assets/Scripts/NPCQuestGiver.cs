using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to an NPC to make it a quest giver. Uses the DialogueManager to present
/// quest offer, in-progress, and completion dialogues. Manages proximity interaction
/// the same way NPCDialogue does.
/// </summary>
public class NPCQuestGiver : MonoBehaviour
{
    [Header("Quests")]
    [Tooltip("Quests this NPC can offer, checked top-to-bottom by prerequisite.")]
    [SerializeField] private List<QuestData> quests = new List<QuestData>();

    [Header("Fallback")]
    [Tooltip("Dialogue shown when no quests are available or all are completed.")]
    [SerializeField] private DialogueData idleDialogue;

    [Header("Interaction")]
    [Tooltip("Distance at which the interact prompt appears.")]
    [SerializeField] private float interactionRange = 2f;

    [Tooltip("Key used to start interaction.")]
    [SerializeField] private Key interactKey = Key.E;

    private Transform _playerTransform;
    private bool _playerInRange;
    private DialogueManager _dialogueManager;
    private QuestManager _questManager;

    // Tracks which quest we're currently offering so we can handle the accept callback.
    private QuestData _pendingOfferQuest;

    private void Start()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
            _playerTransform = player.transform;
        else
            Debug.LogWarning("[NPCQuestGiver] No Player found in scene.");

        _dialogueManager = DialogueManager.Instance;
        if (_dialogueManager == null)
            Debug.LogWarning("[NPCQuestGiver] No DialogueManager found. Add one to the scene.");

        _questManager = QuestManager.Instance;
        if (_questManager == null)
            Debug.LogWarning("[NPCQuestGiver] No QuestManager found. Add one to the scene.");

        // Register all quests with the manager
        if (_questManager != null)
        {
            foreach (QuestData quest in quests)
                _questManager.RegisterQuest(quest);
        }
    }

    private void Update()
    {
        if (_playerTransform == null || _dialogueManager == null) return;
        if (_dialogueManager.IsDialogueActive) return;

        float distance = Vector2.Distance(transform.position, _playerTransform.position);
        bool inRange = distance <= interactionRange;

        if (inRange && !_playerInRange)
        {
            _playerInRange = true;
            _dialogueManager.RegisterPromptCandidate(transform);
        }
        else if (!inRange && _playerInRange)
        {
            _playerInRange = false;
            _dialogueManager.UnregisterPromptCandidate(transform);
        }

        if (_playerInRange && Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
        {
            Interact();
        }
    }

    /// <summary>
    /// Determines which dialogue to show based on quest state and starts it.
    /// </summary>
    public void Interact()
    {
        if (_dialogueManager == null || _questManager == null) return;
        if (_dialogueManager.IsDialogueActive) return;

        _playerInRange = false;
        _dialogueManager.UnregisterPromptCandidate(transform);

        // Find the first quest in a relevant state
        foreach (QuestData quest in quests)
        {
            QuestState state = _questManager.GetQuestState(quest.questId);

            switch (state)
            {
                case QuestState.ReadyToComplete:
                    StartCompletionDialogue(quest);
                    return;

                case QuestState.Active:
                    StartInProgressDialogue(quest);
                    return;

                case QuestState.Unavailable:
                case QuestState.Available:
                    if (_questManager.ArePrerequisitesMet(quest))
                    {
                        StartOfferDialogue(quest);
                        return;
                    }
                    break;

                // Completed quests are skipped to look for the next one
                case QuestState.Completed:
                    break;
            }
        }

        // No quest available; show idle dialogue if configured
        if (idleDialogue != null)
            _dialogueManager.StartDialogue(idleDialogue);
    }

    /// <summary>
    /// Called externally (e.g., by the prompt click) to begin interaction.
    /// </summary>
    public void StartDialogue()
    {
        Interact();
    }

    // ───────────────────── Dialogue Flows ─────────────────────

    private void StartOfferDialogue(QuestData quest)
    {
        if (quest.offerDialogue == null)
        {
            // No offer dialogue configured; auto-accept
            _questManager.AcceptQuest(quest);
            return;
        }

        _pendingOfferQuest = quest;
        _dialogueManager.StartDialogue(quest.offerDialogue);
        _dialogueManager.OnDialogueEnded += HandleOfferDialogueEnded;
    }

    private void HandleOfferDialogueEnded()
    {
        _dialogueManager.OnDialogueEnded -= HandleOfferDialogueEnded;

        if (_pendingOfferQuest != null)
        {
            // Accept the quest when the offer dialogue finishes.
            // If you want Accept/Decline branching, use the dialogue tree to lead
            // to a terminal "accepted" node. Declining just ends the dialogue
            // without the node marked for acceptance.
            // For simplicity, completing the offer dialogue = acceptance.
            _questManager.AcceptQuest(_pendingOfferQuest);
            _pendingOfferQuest = null;
        }
    }

    private void StartInProgressDialogue(QuestData quest)
    {
        if (quest.inProgressDialogue != null)
            _dialogueManager.StartDialogue(quest.inProgressDialogue);
    }

    private void StartCompletionDialogue(QuestData quest)
    {
        if (quest.completionDialogue != null)
        {
            _dialogueManager.StartDialogue(quest.completionDialogue);
            _dialogueManager.OnDialogueEnded += () => HandleCompletionDialogueEnded(quest);
        }
        else
        {
            // No completion dialogue; complete immediately
            _questManager.CompleteQuest(quest);
        }
    }

    private void HandleCompletionDialogueEnded(QuestData quest)
    {
        // Note: lambda subscription; only fires once because dialogue ends.
        _questManager.CompleteQuest(quest);
    }
}
