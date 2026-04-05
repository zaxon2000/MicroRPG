using GDS.Core.Events;
using GDS.Demos.Backpack;
using UnityEngine;
using GdsItem = GDS.Core.Item;

/// <summary>
/// Represents a pickable item lying on the ground.
/// Requires a sibling Interactable component on the same GameObject.
/// Registers itself on Interactable.onInteract at runtime — no Inspector wiring needed.
/// </summary>
[RequireComponent(typeof(Interactable))]
public class GroundItem : MonoBehaviour
{
    [SerializeField] Backpack_ItemBase _itemBase;

    PlayerInventory _inventory;

    void Awake()
    {
        _inventory = FindFirstObjectByType<PlayerInventory>();

        if (_inventory == null)
            Debug.LogWarning("[GroundItem] PlayerInventory not found in scene.");

        // Register pickup on the sibling Interactable — no Inspector wiring required.
        GetComponent<Interactable>().onInteract.AddListener(Pickup);
    }

    /// <summary>Adds this item to the player's backpack and removes it from the world.</summary>
    public void Pickup()
    {
        if (_inventory == null || _itemBase == null) return;

        GdsItem item   = _itemBase.CreateItem();
        Result  result = _inventory.Backpack.Add(item);

        if (result is Fail)
        {
            Debug.LogWarning($"[GroundItem] Could not pick up '{_itemBase.Name}' — backpack may be full.");
            return;
        }

        QuestEvents.RaiseItemCollected(_itemBase.Name);
        Destroy(gameObject);
    }
}
