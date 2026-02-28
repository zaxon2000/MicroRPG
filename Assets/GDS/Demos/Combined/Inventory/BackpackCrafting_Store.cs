using UnityEngine;
using UnityEngine.UIElements;
using GDS.Core;
using GDS.Core.Events;
using GDS.Demos.Basic;
using GDS.Demos.Backpack;
using BackpackBag = GDS.Demos.Backpack.Backpack;
using BackpackShop = GDS.Demos.Backpack.Shop;

namespace GDS.Demos.Combined {

    [CreateAssetMenu(menuName = "SO/Demos/Combined/BackpackCrafting_Store")]
    public class BackpackCrafting_Store : Store {

        public BackpackCrafting_Store() {
            Bus.On<PickItem>(OnPickItem);
            Bus.On<PlaceItem>(OnPlaceItem);
            Bus.On<SellCurrenItem>(OnSellCurrentItem);
            Bus.On<RerollShop>(OnRerollShop);
            Shop.Init(PlayerGold);
        }

        public int RerollCost = 1;

        public Observable<int> PlayerGold = new(50);
        public BackpackBag Backpack = new();
        public BackpackShop Shop = new();
        public Storage Storage = new();
        public CraftingBench CraftingBench = new();

        /// <summary>True while the Craft panel is shown; false shows the Shop panel.</summary>
        public Observable<bool> CraftingActive = new(false);

        bool ShouldMove(IPointerEvent e) => e.modifiers.HasFlag(EventModifiers.Control);

        public override void Reset() {
            base.Reset();
            PlayerGold.Reset();
            // Guard against null Catalog which can occur during Unity's asset serialization cycle.
            Shop.Catalog ??= new System.Collections.Generic.List<GDS.Core.ShapeItemBase>();
            Shop.Reset();
            Backpack.Reset();
            Storage.Reset();
            CraftingBench.Reset();
            CraftingActive.Reset();
        }

        /// <summary>Resets player gold to its default value and triggers the sell SFX pipeline.</summary>
        public void ResetPlayerGold() {
            PlayerGold.Reset();
            Bus.Publish(new SellItemSuccess(null, null));
        }

        void OnPickItem(PickItem e) {
            // Ctrl+click from Backpack moves to Storage instead of picking up.
            Result result = true switch {
                _ when e.Bag is BackpackBag && ShouldMove(e.PointerEvent) => BagExt.MoveItem(e.Bag, e.Item, Storage),
                _ => e.Bag.Remove(e.Item)
            };

            // Picking the crafting outcome slot fires CraftItemSuccess so SFX and other listeners respond correctly.
            result = result switch {
                PlaceItemSuccess r when e.Bag is CraftingBench && e.Slot is OutcomeSlot
                    => result.MapTo(new CraftItemSuccess(r.Item)),
                _ => result
            };

            UpdateGhost(result);
            Bus.Publish(result);
        }

        void OnPlaceItem(PlaceItem e) {
            Result result = e.Bag.AddAt(e.Slot, e.Item);
            UpdateGhost(result);
            Bus.Publish(result);
        }

        void OnSellCurrentItem(SellCurrenItem e) {
            LogUtil.LogEvent(e);
            PlayerGold.SetValue(PlayerGold.Value + Ghost.Value.SellValue());
            Bus.Publish(new SellItemSuccess(Ghost.Value, null));
            Ghost.SetValue(null);
        }

        void OnRerollShop(RerollShop e) {
            LogUtil.LogEvent(e);
            if (PlayerGold.Value < RerollCost) { Bus.Publish(Result.Fail); return; }
            PlayerGold.SetValue(PlayerGold.Value - RerollCost);
            Shop.Reroll();
            Bus.Publish(new SellItemSuccess(null, null));
        }
    }
}
