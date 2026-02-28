using UnityEngine.UIElements;
using UnityEngine;
using GDS.Core;

namespace GDS.Demos.Arpg {

    public class ArpgTooltipView : TooltipView {
        public ArpgTooltipView() {
            Clear();
            ClearClassList();
            var uxml = Resources.Load<VisualTreeAsset>("ArpgTooltipView");
            uxml.CloneTree(this);
            Root = this.Q<VisualElement>("Root");
            WeaponGroup = this.Q<VisualElement>(nameof(WeaponGroup));
            ArmorGroup = this.Q<VisualElement>(nameof(ArmorGroup));
            ShieldGroup = this.Q<VisualElement>(nameof(ShieldGroup));

            ItemName = this.Q<Label>("Name");
            PhysicalDamage = this.Q<Label>(nameof(PhysicalDamage));
            AttacksPerSecond = this.Q<Label>(nameof(AttacksPerSecond));
            ChanceToBlock = this.Q<Label>(nameof(ChanceToBlock));
            Armor = this.Q<Label>(nameof(Armor));
            Root.ClearClassList();
        }

        Label PhysicalDamage, AttacksPerSecond, ChanceToBlock, Armor;
        VisualElement Root, WeaponGroup, ArmorGroup, ShieldGroup;

        public override void Render(IHoveredItemContext context) {
            var item = context.Item;
            ItemName.text = ItemNameText(item);

            var rarity = item.Rarity().ToString();
            ClearClassList();
            AddToClassList("tooltip");
            AddToClassList(rarity);

            WeaponGroup.Hide();
            ArmorGroup.Hide();
            ShieldGroup.Hide();

            if (item.Base is Arpg_WeaponBase wb) {
                PhysicalDamage.text = wb.PhysicalDamage.ToString();
                AttacksPerSecond.text = wb.AttacksPerSecond.ToString();
                WeaponGroup.Show();
            }

            if (item is Arpg_Armor a) {
                Armor.text = a.Armor.ToString();
                ArmorGroup.Show();
            }

            if (item is Arpg_Shield s) {
                ChanceToBlock.text = s.BlockChance + "%";
                Armor.text = s.Armor.ToString();
                ArmorGroup.Show();
                ShieldGroup.Show();
            }

        }

        string ItemNameText(Item item) => item.Rarity() switch {
            Rarity.NoRarity => item.NameWithStack(),
            Rarity.Common => item.NameWithStack(),
            _ => $"{item.Rarity()} {item.NameWithStack()}"
        };
    }

}