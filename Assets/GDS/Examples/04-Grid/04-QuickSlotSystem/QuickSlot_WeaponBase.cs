using GDS.Core;
using UnityEngine;

namespace GDS.Examples {
    [CreateAssetMenu(menuName = "SO/Examples/QuickSlot/QuickSlot_WeaponBase")]
    public class QuickSlot_WeaponBase : QuickSlot_ItemBase {
        public override Item CreateItem() => new QuickSlot_Weapon { Base = this, Name = Name, };
    }

    [System.Serializable]
    public class QuickSlot_Weapon : QuickSlot_Item {
        public int Ammo = 10;
    }
}