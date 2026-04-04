using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The type of condition that must be met to offer or complete a quest.
/// </summary>
public enum QuestConditionType
{
    /// <summary>Player must be at or above a certain level.</summary>
    MinLevel,
    /// <summary>Player must have a specific item in inventory.</summary>
    HasItem,
    /// <summary>Player must have killed a specific enemy at least N times.</summary>
    EnemyKilled,
    /// <summary>Another quest must be completed first.</summary>
    QuestCompleted
}

/// <summary>
/// The type of objective the player must fulfill to progress a quest.
/// </summary>
public enum QuestObjectiveType
{
    /// <summary>Kill a certain number of a named enemy.</summary>
    KillEnemy,
    /// <summary>Collect a certain number of a named item.</summary>
    CollectItem,
    /// <summary>Reach a specific player level.</summary>
    ReachLevel,
    /// <summary>Talk to a specific NPC (by GameObject name).</summary>
    TalkToNPC
}

/// <summary>
/// The current lifecycle state of a quest for the player.
/// </summary>
public enum QuestState
{
    Unavailable,
    Available,
    Active,
    ReadyToComplete,
    Completed
}

/// <summary>
/// A single prerequisite condition that gates whether a quest can be offered.
/// </summary>
[Serializable]
public class QuestCondition
{
    [Tooltip("What type of condition this is.")]
    public QuestConditionType conditionType;

    [Tooltip("Required level (for MinLevel).")]
    public int requiredLevel;

    [Tooltip("Item name to check (for HasItem).")]
    public string itemName;

    [Tooltip("Enemy name to check (for EnemyKilled).")]
    public string enemyName;

    [Tooltip("Number of kills required (for EnemyKilled).")]
    public int requiredCount = 1;

    [Tooltip("Quest ID that must be completed (for QuestCompleted).")]
    public string requiredQuestId;
}

/// <summary>
/// A single objective the player must complete as part of a quest.
/// </summary>
[Serializable]
public class QuestObjective
{
    [Tooltip("What the player must do.")]
    public QuestObjectiveType objectiveType;

    [Tooltip("Short description shown in the quest log.")]
    public string description = "Do something";

    [Tooltip("Target name: enemy name, item name, or NPC name depending on type.")]
    public string targetName;

    [Tooltip("How many are required (kills, items, or target level for ReachLevel).")]
    public int requiredAmount = 1;

    /// <summary>
    /// Runtime progress towards this objective. Not serialized in the asset.
    /// </summary>
    [NonSerialized]
    public int currentAmount;

    /// <summary>
    /// Whether this objective is fulfilled.
    /// </summary>
    public bool IsComplete => currentAmount >= requiredAmount;
}

/// <summary>
/// Rewards granted to the player upon quest completion.
/// </summary>
[Serializable]
public class QuestReward
{
    [Tooltip("Experience points awarded.")]
    public int xp;

    [Tooltip("Gold awarded.")]
    public int gold;

    [Tooltip("Items awarded (by name). Leave empty for none.")]
    public List<string> items = new List<string>();
}

/// <summary>
/// ScriptableObject defining a single quest: its prerequisites, objectives, rewards,
/// and dialogue hooks for offer, in-progress, and completion conversations.
/// </summary>
[CreateAssetMenu(fileName = "NewQuest", menuName = "Quest/QuestData")]
public class QuestData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique identifier for this quest.")]
    public string questId;

    [Tooltip("Display name shown in the quest log.")]
    public string questTitle = "New Quest";

    [TextArea(2, 4)]
    [Tooltip("Description shown in the quest log.")]
    public string questDescription = "";

    [Header("Prerequisites")]
    [Tooltip("All conditions must be met before this quest can be offered.")]
    public List<QuestCondition> prerequisites = new List<QuestCondition>();

    [Header("Objectives")]
    [Tooltip("All objectives must be completed to finish the quest.")]
    public List<QuestObjective> objectives = new List<QuestObjective>();

    [Header("Rewards")]
    public QuestReward reward;

    [Header("Dialogue Integration")]
    [Tooltip("Dialogue shown when offering the quest. Last two responses should be Accept / Decline.")]
    public DialogueData offerDialogue;

    [Tooltip("Dialogue shown while quest is in progress.")]
    public DialogueData inProgressDialogue;

    [Tooltip("Dialogue shown when quest is ready to turn in.")]
    public DialogueData completionDialogue;
}
