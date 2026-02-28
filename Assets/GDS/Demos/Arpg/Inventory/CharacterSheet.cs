using GDS.Core;
using UnityEngine;

namespace GDS.Demos.Arpg {

    public class Stats {
        public IntRange PhysicalDamage = new();
        public float AttacksPerSecond;
        public int Armor;
        public int BlockChance;
        public float dps => PhysicalDamage.Avg * AttacksPerSecond;
    }

    public class CharacterSheet {
        public Observable<Stats> Stats = new(new());

        Equipment Equipment;
        public void Init(Equipment equipment) {
            Equipment = equipment;
            equipment.CollectionChanged += OnEquipmentChanged;
            OnEquipmentChanged();
        }

        public void Reset() {
            Stats.SetValue(new());
        }

        void OnEquipmentChanged() {
            Stats stats = new();
            foreach (var item in Equipment.Items) {
                if (item is Arpg_Armor a) { stats.Armor += a.Armor; }
                if (item is Arpg_Shield s) { stats.Armor += s.Armor; stats.BlockChance = s.BlockChance; }
                if (item is Arpg_Weapon w) {
                    stats.PhysicalDamage += w.ItemBase.PhysicalDamage.Clone();
                    stats.AttacksPerSecond = stats.AttacksPerSecond == 0 ? w.ItemBase.AttacksPerSecond : (stats.AttacksPerSecond + w.ItemBase.AttacksPerSecond) / 2;
                }
            }

            Stats.SetValue(stats);
        }
    }
}
