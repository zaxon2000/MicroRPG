using UnityEngine;
using GDS.Core;

namespace GDS.Demos.Backpack {
    [CreateAssetMenu(menuName = "SO/Demos/Backpack/Backpack_WeaponBase")]
    public class Backpack_WeaponBase : Backpack_ItemBase {
        public IntRange Damage;
        public float Stamina;
        public float Accuracy;
        public float Cooldown;

        public float DPS => (Damage.Min + Damage.Max) / (2 * Cooldown);
        public float SPS => Stamina / Cooldown;

        public override Item CreateItem() => new Backpack_Weapon { Base = this, Name = Name };
    }

    [System.Serializable]
    public class Backpack_Weapon : Backpack_Item { }
}