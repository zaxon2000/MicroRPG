using UnityEngine;
using GDS.Core;

namespace GDS.Demos.Arpg {
    [CreateAssetMenu(menuName = "SO/Demos/Arpg/Arpg_ShieldBase")]
    public class Arpg_ShieldBase : Arpg_ArmorBase {
        public int BlockChance;
        public override Item CreateItem() => new Arpg_Shield { Base = this, Name = Name, Armor = Armor.Roll() };
    }

    [System.Serializable]
    public class Arpg_Shield : Arpg_Item {
        public int Armor;
        public int BlockChance => (Base as Arpg_ShieldBase).BlockChance;
    }

}