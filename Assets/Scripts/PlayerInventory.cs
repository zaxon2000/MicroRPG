using System.Collections.Generic;
using GDS.Core;
using GDS.Core.Events;
using GDS.Demos.Backpack;
using GDS.Demos.Combined;
using UnityEngine;
using UnityEngine.UIElements;
using BackpackBag       = GDS.Demos.Backpack.Backpack;
using BackpackShop      = GDS.Demos.Backpack.Shop;
using BasicCraftingBench = GDS.Demos.Basic.CraftingBench;
using BasicRecipe        = GDS.Demos.Basic.Recipe;
using CraftItemSuccess   = GDS.Demos.Basic.CraftItemSuccess;

/// <summary>
/// Owns all mutable inventory state for the player.
/// The BackpackCrafting_Store asset is read-only configuration (catalogs, recipes, economy values).
/// All bags, observables, and the event bus are created fresh each Play session with clean lifecycle.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] BackpackCrafting_Store _config;

    const int BackpackCellSize = 27;

    // ── Bags ──────────────────────────────────────────────────────────────────
    public BackpackBag        Backpack       { get; private set; }
    public Storage            Storage        { get; private set; }
    public BackpackShop       Shop           { get; private set; }
    public BasicCraftingBench CraftingBench  { get; private set; }

    // ── Observables ───────────────────────────────────────────────────────────
    public Observable<int>  PlayerGold    { get; private set; }
    public Observable<bool> CraftingActive { get; private set; }

    /// <summary>
    /// Minimal Store shim — holds EventBus and Ghost so GDS manipulators and views
    /// (DragDropManipulator, RotateGhostManipulator, ShopView) receive the types they require.
    /// </summary>
    public PlayerInventoryStore InventoryStore { get; private set; }

    public int RerollCost => _config != null ? _config.RerollCost : 1;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (_config == null) { Debug.LogError("[PlayerInventory] Config asset not assigned."); return; }

        // Observables
        PlayerGold     = new Observable<int>(_config.StartingGold);
        CraftingActive = new Observable<bool>(false);

        // Bags — fresh instances every session, seeded from config where needed
        Backpack      = new BackpackBag { CellSize = BackpackCellSize };
        Storage       = new Storage();
        Shop          = new BackpackShop { Catalog = _config.Shop.Catalog };
        CraftingBench = new BasicCraftingBench { Recipes = new List<BasicRecipe>(_config.CraftingBench.Recipes) };

        // Init
        Backpack.Init();
        Storage.Init();
        Shop.Init(PlayerGold);  // links shop gold check to this player's gold
        Shop.Reset();           // populates shop slots from catalog
        CraftingBench.Init();

        // Runtime Store shim — provides Bus + Ghost to GDS manipulators without mutating any asset
        InventoryStore = ScriptableObject.CreateInstance<PlayerInventoryStore>();

        // Register all event handlers on the runtime bus
        InventoryStore.Bus.On<PickItem>(OnPickItem);
        InventoryStore.Bus.On<PlaceItem>(OnPlaceItem);
        InventoryStore.Bus.On<SellCurrenItem>(OnSellCurrentItem);
        InventoryStore.Bus.On<RerollShop>(OnRerollShop);
    }

    void OnDestroy()
    {
        if (InventoryStore != null)
            Destroy(InventoryStore);
    }

    /// <summary>
    /// Connects this inventory's bags to the BackpackCrafting_Controller UI.
    /// Called in Start() so all Awake() calls (including the controller's) have completed first.
    /// </summary>
    void Start()
    {
        var controller = FindFirstObjectByType<GDS.Demos.Combined.BackpackCrafting_Controller>();
        if (controller == null) { Debug.LogError("[PlayerInventory] BackpackCrafting_Controller not found in scene."); return; }

        controller.Initialize(
            InventoryStore,
            Backpack,
            Storage,
            Shop,
            CraftingBench,
            PlayerGold,
            CraftingActive,
            ResetPlayerGold);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Resets player gold to its starting value and notifies listeners.</summary>
    public void ResetPlayerGold()
    {
        PlayerGold.Reset();
        InventoryStore.Bus.Publish(new SellItemSuccess(null, null));
    }

    // ── Event Handlers ────────────────────────────────────────────────────────

    bool ShouldMove(IPointerEvent e) => e.modifiers.HasFlag(EventModifiers.Control);

    void OnPickItem(PickItem e)
    {
        // Ctrl+click from Backpack moves the item to Storage instead of picking it up.
        Result result = true switch {
            _ when e.Bag is BackpackBag && ShouldMove(e.PointerEvent) => BagExt.MoveItem(e.Bag, e.Item, Storage),
            _ => e.Bag.Remove(e.Item)
        };

        // Picking from the crafting outcome slot fires CraftItemSuccess for SFX / other listeners.
        result = result switch {
            PlaceItemSuccess r when e.Bag is BasicCraftingBench && e.Slot is GDS.Demos.Basic.OutcomeSlot
                => result.MapTo(new CraftItemSuccess(r.Item)),
            _ => result
        };

        UpdateGhost(result);
        InventoryStore.Bus.Publish(result);
    }

    void OnPlaceItem(PlaceItem e)
    {
        Result result = e.Bag.AddAt(e.Slot, e.Item);
        UpdateGhost(result);
        InventoryStore.Bus.Publish(result);
    }

    void OnSellCurrentItem(SellCurrenItem e)
    {
        PlayerGold.SetValue(PlayerGold.Value + InventoryStore.Ghost.Value.SellValue());
        InventoryStore.Bus.Publish(new SellItemSuccess(InventoryStore.Ghost.Value, null));
        InventoryStore.Ghost.SetValue(null);
    }

    void OnRerollShop(RerollShop e)
    {
        if (PlayerGold.Value < RerollCost) { InventoryStore.Bus.Publish(Result.Fail); return; }
        PlayerGold.SetValue(PlayerGold.Value - RerollCost);
        Shop.Reroll();
        InventoryStore.Bus.Publish(new SellItemSuccess(null, null));
    }

    void UpdateGhost(Result result)
    {
        if (result is PlaceItemSuccess r)       InventoryStore.Ghost.SetValue(r.Replaced);
        else if (result is PickItemSuccess r1)   InventoryStore.Ghost.SetValue(r1.Item);
    }
}
