using System;

/// <summary>
/// Central static event hub for quest-related gameplay events.
/// Other systems (Enemy, Player, Inventory) publish events here;
/// QuestManager subscribes to track objective progress.
/// </summary>
public static class QuestEvents
{
    /// <summary>
    /// Fired when an enemy is killed. Parameter is the enemy's GameObject name.
    /// </summary>
    public static event Action<string> OnEnemyKilled;

    /// <summary>
    /// Fired when the player levels up. Parameter is the new level.
    /// </summary>
    public static event Action<int> OnPlayerLeveledUp;

    /// <summary>
    /// Fired when the player picks up or acquires an item. Parameter is the item name.
    /// </summary>
    public static event Action<string> OnItemCollected;

    /// <summary>
    /// Raises the enemy killed event.
    /// </summary>
    public static void RaiseEnemyKilled(string enemyName)
    {
        OnEnemyKilled?.Invoke(enemyName);
    }

    /// <summary>
    /// Raises the player leveled up event.
    /// </summary>
    public static void RaisePlayerLeveledUp(int newLevel)
    {
        OnPlayerLeveledUp?.Invoke(newLevel);
    }

    /// <summary>
    /// Raises the item collected event.
    /// </summary>
    public static void RaiseItemCollected(string itemName)
    {
        OnItemCollected?.Invoke(itemName);
    }
}
