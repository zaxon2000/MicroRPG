using UnityEngine;
using GDS.Core;

namespace GDS.Demos.Arpg {
    [CreateAssetMenu(menuName = "SO/Demos/Arpg/Arpg_ArmorBase")]
    public class Arpg_ArmorBase : Arpg_ItemBase {
        public IntRange Armor;
        public override Item CreateItem() => new Arpg_Armor() { Base = this, Name = Name, Armor = Armor.Roll() };
    }

    [System.Serializable]
    public class Arpg_Armor : Arpg_Item {
        public int Armor;
    }



}