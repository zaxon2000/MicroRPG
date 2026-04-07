using GDS.Demos.Backpack;

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
    /// Fired when the player picks up or acquires an item. Parameter is the item's display name.
    /// </summary>
    public static event Action<string> OnItemCollected;

    /// <summary>
    /// Fired by QuestManager when a quest completes and an item reward should be granted.
    /// PlayerInventory subscribes and adds the item directly to the backpack.
    /// </summary>
    public static event Action<Backpack_ItemBase> OnQuestItemRewarded;

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

    /// <summary>
    /// Raises the quest item rewarded event. Called by QuestManager once per reward item
    /// on quest completion. PlayerInventory handles adding it to the backpack.
    /// </summary>
    public static void RaiseQuestItemRewarded(Backpack_ItemBase itemBase)
    {
        OnQuestItemRewarded?.Invoke(itemBase);
    }

    /// <summary>
    /// Fired when the player takes damage. Parameter is the amount of damage taken.
    /// </summary>
    public static event Action<float> OnPlayerDamaged;

    /// <summary>
    /// Raises the player damaged event.
    /// </summary>
    public static void RaisePlayerDamaged(float damage)
    {
        OnPlayerDamaged?.Invoke(damage);
    }
}
