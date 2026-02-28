using UnityEngine;
using GDS.Core;

namespace GDS.Demos.Arpg {
    [CreateAssetMenu(menuName = "SO/Demos/Arpg/Arpg_WeaponBase")]
    public class Arpg_WeaponBase : Arpg_ItemBase {
        public float AttacksPerSecond = 1f;
        public IntRange PhysicalDamage;
        public float dps => PhysicalDamage.Avg * AttacksPerSecond;

        // void OnValidate() {
        //     // if (PhysicalDamage.Max < PhysicalDamage.Min) PhysicalDamage.Max = PhysicalDamage.Min;
        //     // Debug.Log($"name: {name}, dps: {dps}");
        // }

        public override Item CreateItem() => new Arpg_Weapon { Base = this, Name = Name };
    }

    [System.Serializable]
    public class Arpg_Weapon : Arpg_Item {
        public Arpg_WeaponBase ItemBase => Base as Arpg_WeaponBase;

    }



}