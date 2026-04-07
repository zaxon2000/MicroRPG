using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to an NPC to enable proximity-based dialogue interaction.
/// Shows the "E" prompt when the player is in range and starts dialogue on input.
/// </summary>
public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue")]
    [Tooltip("The dialogue data asset for this NPC.")]
    [SerializeField] private DialogueData dialogueData;

    [Header("Interaction")]
    [Tooltip("Distance at which the interact prompt appears.")]
    [SerializeField] private float interactionRange = 2f;

    [Tooltip("Key used to start the dialogue.")]
    [SerializeField] private Key interactKey = Key.E;

    private Transform _playerTransform;
    private bool _playerInRange;
    private DialogueManager _dialogueManager;

    private void Start()
    {
        // Cache player reference
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
            _playerTransform = player.transform;
        else
            Debug.LogWarning("[NPCDialogue] No Player found in scene.");

        _dialogueManager = DialogueManager.Instance;
        if (_dialogueManager == null)
            Debug.LogWarning("[NPCDialogue] No DialogueManager found. Add one to the scene.");
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

        // Key input is now handled centrally by DialogueManager to prevent
        // race conditions when multiple NPCs are in range simultaneously.
    }

    /// <summary>
    /// Starts the dialogue conversation for this NPC.
    /// </summary>
    public void StartDialogue()
    {
        if (_dialogueManager == null || dialogueData == null) return;
        if (_dialogueManager.IsDialogueActive) return;

        _playerInRange = false;
        _dialogueManager.UnregisterPromptCandidate(transform);
        _dialogueManager.StartDialogue(dialogueData);
    }
}
