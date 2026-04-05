using System;
using System.Collections.Generic;
using System.Linq;
using GDS.Core;
using GDS.Core.Events;
using UnityEngine;

namespace GDS.Demos.Backpack {
    [Serializable]
    public class Shop : ListBag {
        public Shop() => Size = 5;
        public List<ShapeItemBase> Catalog;
        public Observable<int> PlayerGold;
        public int RerollCost = 1;

        public void Init(Observable<int> playerGold) {
            PlayerGold = playerGold;
        }

        public void Reroll() {
            Clear();
            AddRange(Catalog.Randomize().Take(Size).Select(b => b.CreateItem()));
        }

        public override bool Accepts(Item item) => false;

        public override Result CanRemove(Item item) {
            var itemCost = item.Cost();
            if (PlayerGold.Value < itemCost) { Debug.Log("Not enough gold".Red()); }
            return PlayerGold.Value < itemCost ? Result.Fail : Result.Success;
        }

        public override Result Remove(Item item) {
            var result = CanRemove(item);
            if (result is Success) result = base.Remove(item);
            if (result is Success) PlayerGold.SetValue(PlayerGold.Value - item.Cost());
            return result.MapTo(new BuyItemSuccess(item));
        }
    }

    public static class ShopExt {
        static System.Random random = new System.Random();
        public static IEnumerable<T> Randomize<T>(this List<T> list) => list.OrderBy(_ => random.Next());

    }

}