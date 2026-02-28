using System;
using GDS.Core;
using UnityEngine;

namespace GDS.Demos.Backpack {

    [CreateAssetMenu(menuName = "SO/Demos/Backpack/Backpack_ItemBase")]
    public class Backpack_ItemBase : ShapeItemBase {
        public Rarity Rarity;
        public ItemType ItemType;
        public int Cost;

        public override Item CreateItem() => new Backpack_Item { Base = this, Name = Name };
    }

    [System.Serializable]
    public class Backpack_Item : ShapeItem { }
    public enum ItemType { Food, Weapon, Gloves, Armor, Pet }
    public enum Rarity { Common, Magic, Rare, Unique, Epic, Legendary }


    public static class ItemExt {
        public static int Cost(this Item item) => item.Base is Backpack_ItemBase b ? b.Cost : 0;
        public static int SellValue(this Item item) => (int)Math.Ceiling(item.Cost() / 2f);
    }

}