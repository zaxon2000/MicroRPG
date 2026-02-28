using UnityEngine;
using GDS.Core;

namespace GDS.Demos.Arpg {
    [CreateAssetMenu(menuName = "SO/Demos/Arpg/Arpg_ItemBase")]
    public class Arpg_ItemBase : ShapeItemBase { }

    [System.Serializable]
    public class Arpg_Item : ShapeItem {
        public Rarity Rarity;
    }

    public enum Rarity { NoRarity, Common, Magic, Rare, Unique }

    public static class ArpgItemExt {
        public static string NameWithStack(this Item item) => item.Stackable ? $"{item.Name} ({item.StackSize})" : item.Name;
        public static Rarity Rarity(this Item item) => item is Arpg_Item i ? i.Rarity : Arpg.Rarity.NoRarity;
        public static int Cost(this Item item) => 10;
    }

}