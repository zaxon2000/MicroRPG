using System;
using System.Collections.Generic;
using System.Linq;
using GDS.Core;
using GDS.Demos.Backpack;
using UnityEngine;

/// <summary>
/// Singleton that tracks all quest states, listens for gameplay events,
/// and updates objective progress. Other systems query this to determine
/// quest availability and completion.
/// </summary>
public class QuestManager : MonoBehaviour
{
    private static QuestManager _instance;

    /// <summary>
    /// Singleton accessor.
    /// </summary>
    public static QuestManager Instance => _instance;

    /// <summary>
    /// Raised when any quest changes state (offered, accepted, progressed, completed).
    /// </summary>
    public event Action<QuestData, QuestState> OnQuestStateChanged;

    /// <summary>
    /// Raised when an objective makes progress.
    /// </summary>
    public event Action<QuestData, QuestObjective> OnObjectiveProgressed;

    // Runtime quest tracking. Key = questId.
    private readonly Dictionary<string, QuestState> _questStates = new Dictionary<string, QuestState>();

    // Cached kill counts per enemy name (persists across quest boundaries for prerequisite checks).
    private readonly Dictionary<string, int> _killCounts = new Dictionary<string, int>();

    // Active quests with their runtime objective copies.
    private readonly Dictionary<string, List<QuestObjective>> _activeObjectives = new Dictionary<string, List<QuestObjective>>();

    // All registered quest data assets.
    private readonly Dictionary<string, QuestData> _registeredQuests = new Dictionary<string, QuestData>();

    private Player _player;
    private PlayerInventory _inventory;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void OnEnable()
    {
        QuestEvents.OnEnemyKilled += HandleEnemyKilled;
        QuestEvents.OnPlayerLeveledUp += HandlePlayerLeveledUp;
        QuestEvents.OnItemCollected += HandleItemCollected;
        QuestEvents.OnPlayerDamaged += HandlePlayerDamaged;
    }

    private void OnDisable()
    {
        QuestEvents.OnEnemyKilled -= HandleEnemyKilled;
        QuestEvents.OnPlayerLeveledUp -= HandlePlayerLeveledUp;
        QuestEvents.OnItemCollected -= HandleItemCollected;
        QuestEvents.OnPlayerDamaged -= HandlePlayerDamaged;
    }

    private void Start()
    {
        _player = FindFirstObjectByType<Player>();
        if (_player == null)
            Debug.LogWarning("[QuestManager] No Player found in scene.");

        _inventory = FindFirstObjectByType<PlayerInventory>();
        if (_inventory == null)
            Debug.LogWarning("[QuestManager] No PlayerInventory found in scene.");
    }

    // ───────────────────── Registration ─────────────────────

    /// <summary>
    /// Registers a quest so QuestManager is aware of it. Called by NPCQuestGiver on Start.
    /// </summary>
    public void RegisterQuest(QuestData quest)
    {
        if (quest == null || string.IsNullOrEmpty(quest.questId)) return;
        if (_registeredQuests.ContainsKey(quest.questId)) return;

        _registeredQuests[quest.questId] = quest;

        if (!_questStates.ContainsKey(quest.questId))
            _questStates[quest.questId] = QuestState.Unavailable;
    }

    // ───────────────────── State Queries ─────────────────────

    /// <summary>
    /// Returns the current state of a quest.
    /// </summary>
    public QuestState GetQuestState(string questId)
    {
        return _questStates.TryGetValue(questId, out QuestState state) ? state : QuestState.Unavailable;
    }

    /// <summary>
    /// Returns the runtime objectives for an active quest, or null if not active.
    /// </summary>
    public List<QuestObjective> GetActiveObjectives(string questId)
    {
        return _activeObjectives.TryGetValue(questId, out List<QuestObjective> objs) ? objs : null;
    }

    /// <summary>
    /// Returns all quests currently in the Active or ReadyToComplete state.
    /// </summary>
    public List<QuestData> GetTrackedQuests()
    {
        List<QuestData> result = new List<QuestData>();
        foreach (KeyValuePair<string, QuestState> kvp in _questStates)
        {
            if (kvp.Value == QuestState.Active || kvp.Value == QuestState.ReadyToComplete)
            {
                if (_registeredQuests.TryGetValue(kvp.Key, out QuestData data))
                    result.Add(data);
            }
        }
        return result;
    }

    /// <summary>
    /// Returns the total kill count for a given enemy name.
    /// </summary>
    public int GetKillCount(string enemyName)
    {
        return _killCounts.TryGetValue(enemyName, out int count) ? count : 0;
    }

    // ───────────────────── Prerequisite Check ─────────────────────

    /// <summary>
    /// Checks whether all prerequisites for a quest are currently met.
    /// </summary>
    public bool ArePrerequisitesMet(QuestData quest)
    {
        if (quest == null) return false;
        if (_player == null) return false;

        foreach (QuestCondition cond in quest.prerequisites)
        {
            switch (cond.conditionType)
            {
                case QuestConditionType.MinLevel:
                    if (_player.curLevel < cond.requiredLevel) return false;
                    break;

                case QuestConditionType.HasItem:
                    if (_inventory == null) return false;
                    bool hasItem = _inventory.Backpack.Items
                        .Any(item => item != null &&
                             string.Equals(item.Name, cond.itemName, StringComparison.OrdinalIgnoreCase));
                    if (!hasItem) return false;
                    break;

                case QuestConditionType.EnemyKilled:
                    if (GetKillCount(cond.enemyName) < cond.requiredCount) return false;
                    break;

                case QuestConditionType.QuestCompleted:
                    if (GetQuestState(cond.requiredQuestId) != QuestState.Completed) return false;
                    break;
            }
        }
        return true;
    }

    // ───────────────────── Quest Lifecycle ─────────────────────

    /// <summary>
    /// Accepts a quest, moving it from Available to Active and creating runtime objective copies.
    /// </summary>
    public void AcceptQuest(QuestData quest)
    {
        if (quest == null) return;
        string id = quest.questId;

        QuestState currentState = GetQuestState(id);
        if (currentState != QuestState.Unavailable && currentState != QuestState.Available && currentState != QuestState.Failed)
        {
            Debug.LogWarning($"[QuestManager] Cannot accept quest '{id}' in state {currentState}.");
            return;
        }

        // Create runtime copies of objectives so progress doesn't mutate the asset
        List<QuestObjective> runtimeObjectives = new List<QuestObjective>();
        foreach (QuestObjective src in quest.objectives)
        {
            runtimeObjectives.Add(new QuestObjective
            {
                objectiveType = src.objectiveType,
                description = src.description,
                targetName = src.targetName,
                requiredAmount = src.requiredAmount,
                currentAmount = 0
            });
        }
        _activeObjectives[id] = runtimeObjectives;

        SetQuestState(id, QuestState.Active);
        Debug.Log($"[QuestManager] Quest accepted: {quest.questTitle}");
    }

    /// <summary>
    /// Completes a quest, granting rewards and moving it to Completed.
    /// </summary>
    public void CompleteQuest(QuestData quest)
    {
        if (quest == null) return;
        string id = quest.questId;

        if (GetQuestState(id) != QuestState.ReadyToComplete)
        {
            Debug.LogWarning($"[QuestManager] Cannot complete quest '{id}' in state {GetQuestState(id)}.");
            return;
        }

        // Grant rewards
        if (quest.reward != null && _player != null)
        {
            if (quest.reward.xp > 0)
                _player.AddXp(quest.reward.xp);

            if (quest.reward.gold > 0 && _inventory != null)
                _inventory.PlayerGold.SetValue(_inventory.PlayerGold.Value + quest.reward.gold);

            // Fire one event per item so PlayerInventory (or any other listener) can handle it.
            foreach (Backpack_ItemBase itemBase in quest.reward.items)
            {
                if (itemBase != null)
                    QuestEvents.RaiseQuestItemRewarded(itemBase);
            }
        }

        _activeObjectives.Remove(id);
        SetQuestState(id, QuestState.Completed);
        Debug.Log($"[QuestManager] Quest completed: {quest.questTitle}");
    }

    /// <summary>
    /// Fails an active quest, moving it to the Failed state.
    /// </summary>
    public void FailQuest(QuestData quest)
    {
        if (quest == null) return;
        string id = quest.questId;

        QuestState state = GetQuestState(id);
        if (state != QuestState.Active && state != QuestState.ReadyToComplete)
        {
            Debug.LogWarning($"[QuestManager] Cannot fail quest '{id}' in state {state}.");
            return;
        }

        _activeObjectives.Remove(id);
        SetQuestState(id, QuestState.Failed);
        Debug.Log($"[QuestManager] Quest failed: {quest.questTitle}");
    }

    /// <summary>
    /// Retries a failed quest by re-accepting it with fresh objectives.
    /// Also respawns any deactivated GroundItems that match the quest's CollectItem objectives.
    /// </summary>
    public void RetryQuest(QuestData quest)
    {
        if (quest == null) return;
        string id = quest.questId;

        if (GetQuestState(id) != QuestState.Failed)
        {
            Debug.LogWarning($"[QuestManager] Cannot retry quest '{id}' — it is not in Failed state.");
            return;
        }

        // Respawn ground items matching CollectItem objectives
        RespawnQuestGroundItems(quest);

        // Re-accept the quest
        AcceptQuest(quest);
    }

    /// <summary>
    /// Abandons a quest permanently, removing it without granting rewards.
    /// </summary>
    public void AbandonQuest(QuestData quest)
    {
        if (quest == null) return;
        string id = quest.questId;

        _activeObjectives.Remove(id);
        SetQuestState(id, QuestState.Completed);
        Debug.Log($"[QuestManager] Quest abandoned: {quest.questTitle}");
    }

    /// <summary>
    /// Re-enables any deactivated GroundItem GameObjects whose item name matches
    /// a CollectItem objective in the given quest.
    /// </summary>
    private void RespawnQuestGroundItems(QuestData quest)
    {
        HashSet<string> targetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (QuestObjective obj in quest.objectives)
        {
            if (obj.objectiveType == QuestObjectiveType.CollectItem)
                targetNames.Add(obj.targetName);
        }

        if (targetNames.Count == 0) return;

        // FindObjectsByType with Include finds components on inactive GameObjects
        GroundItem[] allItems = FindObjectsByType<GroundItem>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (GroundItem gi in allItems)
        {
            if (!gi.gameObject.activeSelf && targetNames.Contains(gi.ItemName))
            {
                gi.gameObject.SetActive(true);
                Debug.Log($"[QuestManager] Respawned ground item: {gi.ItemName}");
            }
        }
    }

    private void SetQuestState(string questId, QuestState newState)
    {
        _questStates[questId] = newState;

        if (_registeredQuests.TryGetValue(questId, out QuestData data))
            OnQuestStateChanged?.Invoke(data, newState);
    }

    // ───────────────────── Event Handlers ─────────────────────

    private void HandleEnemyKilled(string enemyName)
    {
        // Increment global kill counter
        if (!_killCounts.ContainsKey(enemyName))
            _killCounts[enemyName] = 0;
        _killCounts[enemyName]++;

        ProgressObjectives(QuestObjectiveType.KillEnemy, enemyName);
    }

    private void HandlePlayerLeveledUp(int newLevel)
    {
        ProgressObjectives(QuestObjectiveType.ReachLevel, "", newLevel);
    }

    private void HandleItemCollected(string itemName)
    {
        ProgressObjectives(QuestObjectiveType.CollectItem, itemName);
    }

    private void HandlePlayerDamaged(float damage)
    {
        // Fail any active or ready-to-complete quests that have failOnDamage enabled
        List<string> toFail = new List<string>();

        foreach (KeyValuePair<string, QuestState> kvp in _questStates)
        {
            if (kvp.Value != QuestState.Active && kvp.Value != QuestState.ReadyToComplete)
                continue;

            if (_registeredQuests.TryGetValue(kvp.Key, out QuestData data) && data.failOnDamage)
                toFail.Add(kvp.Key);
        }

        foreach (string questId in toFail)
        {
            if (_registeredQuests.TryGetValue(questId, out QuestData data))
                FailQuest(data);
        }
    }

    /// <summary>
    /// Notifies the quest manager that the player talked to a specific NPC.
    /// Call this from NPCQuestGiver or other dialogue triggers.
    /// </summary>
    public void NotifyTalkedToNPC(string npcName)
    {
        ProgressObjectives(QuestObjectiveType.TalkToNPC, npcName);
    }

    private void ProgressObjectives(QuestObjectiveType type, string targetName, int overrideAmount = -1)
    {
        // Iterate over a copy of keys in case state changes during iteration
        List<string> activeIds = _activeObjectives.Keys.ToList();

        foreach (string questId in activeIds)
        {
            if (GetQuestState(questId) != QuestState.Active) continue;

            List<QuestObjective> objectives = _activeObjectives[questId];
            bool anyProgressed = false;

            foreach (QuestObjective obj in objectives)
            {
                if (obj.IsComplete) continue;
                if (obj.objectiveType != type) continue;

                bool matches = type switch
                {
                    QuestObjectiveType.ReachLevel => overrideAmount >= obj.requiredAmount,
                    _ => string.Equals(obj.targetName, targetName, StringComparison.OrdinalIgnoreCase)
                };

                if (!matches) continue;

                if (type == QuestObjectiveType.ReachLevel)
                    obj.currentAmount = overrideAmount;
                else
                    obj.currentAmount++;

                anyProgressed = true;

                if (_registeredQuests.TryGetValue(questId, out QuestData data))
                    OnObjectiveProgressed?.Invoke(data, obj);
            }

            // Check if all objectives are now complete
            if (anyProgressed && objectives.TrueForAll(o => o.IsComplete))
            {
                SetQuestState(questId, QuestState.ReadyToComplete);
            }
        }
    }
}
