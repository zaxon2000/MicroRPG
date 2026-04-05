using UnityEngine;
using GDS.Core;
using GDS.Core.Events;
using UnityEngine.UIElements;
using System;

namespace GDS.Demos.Backpack {

    [CreateAssetMenu(menuName = "SO/Demos/Backpack/Backpack_Store")]
    public class Backpack_Store : Store {
        public Backpack_Store() {
            Bus.On<PickItem>(OnPickItem);
            Bus.On<PlaceItem>(OnPlaceItem);

            Bus.On<SellCurrenItem>(OnSellCurrentItem);
            Bus.On<RerollShop>(OnRerollShop);

            Shop.Init(PlayerGold);
        }

        public int RerollCost = 1;

        public Observable<int> PlayerGold = new(50);

        public Backpack Backpack = new();
        public Shop Shop = new();
        public Storage Storage = new();

        bool ShouldMove(IPointerEvent e) => e.modifiers.HasFlag(EventModifiers.Control);


        public override void Reset() {
            base.Reset();
            PlayerGold.Reset();

            Backpack.Clear();
            Storage.Clear();
            Shop.Reroll();
        }

        public void ResetPlayerGold() {
            PlayerGold.Reset();
            Bus.Publish(new SellItemSuccess(null, null));
        }

        void OnPickItem(PickItem e) {
            Result result = true switch {
                _ when e.Bag is Backpack && ShouldMove(e.PointerEvent) => BagExt.MoveItem(e.Bag, e.Item, Storage),
                _ => e.Bag.Remove(e.Item)
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