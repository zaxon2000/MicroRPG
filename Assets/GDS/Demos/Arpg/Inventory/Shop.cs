using System;
using System.Collections.Generic;
using System.Linq;
using GDS.Core;
using GDS.Core.Events;
using UnityEngine;

namespace GDS.Demos.Arpg {

    [Serializable]
    public class Shop : GridBag {

        public List<Arpg_ItemBase> Catalog = new();
        // PlayerGold should be injected by a Store or a MonoBehavior on init/awake
        public Observable<int> PlayerGold;
        public void Refresh() {
            Clear();
            AddRange(Catalog.Select(b => b.CreateItem()));
        }

        public override bool AllowStacking() => false;

        public override Result Add(Item item) {
            var result = base.Add(item);
            if (result is Success) PlayerGold.SetValue(PlayerGold.Value + item.Cost());
            return result.MapTo(new SellItemSuccess(item, null));
        }

        public override Result AddAt(Slot slot, Item item) {
            return Add(item);
        }

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
}
