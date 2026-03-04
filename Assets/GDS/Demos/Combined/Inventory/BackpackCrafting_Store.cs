using GDS.Core;
using GDS.Demos.Backpack;
using GDS.Demos.Basic;
using UnityEngine;
using BackpackBag        = GDS.Demos.Backpack.Backpack;
using BackpackShop       = GDS.Demos.Backpack.Shop;
using BasicCraftingBench = GDS.Demos.Basic.CraftingBench;

namespace GDS.Demos.Combined {

    /// <summary>
    /// Read-only configuration asset for the player's inventory system.
    /// All mutable runtime state lives in <see cref="PlayerInventory"/> on the Player GameObject.
    /// This asset is never modified at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "SO/Demos/Combined/BackpackCrafting_Store")]
    public class BackpackCrafting_Store : Store {

        [Header("Economy")]
        public int RerollCost   = 1;
        public int StartingGold = 50;

        [Header("Inventory")]
        public BackpackBag Backpack = new();
        public Storage     Storage  = new();

        [Header("Shop")]
        public BackpackShop Shop = new();

        [Header("Crafting")]
        public BasicCraftingBench CraftingBench = new();
    }
}
