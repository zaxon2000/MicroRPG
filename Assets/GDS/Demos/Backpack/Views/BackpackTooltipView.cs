using GDS.Core;
using GDS.Demos.Backpack;
using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Examples {

    public class BackpackTooltipView : TooltipView {
        public BackpackTooltipView() {
            var uxml = Resources.Load<VisualTreeAsset>("TooltipView");
            if (uxml == null) { Debug.LogError("Could not find visual tree asset TooltipView in Resources"); return; }

            Clear();
            uxml.CloneTree(this);
            ItemName = this.Q<Label>(nameof(ItemName));
            ItemRarityAndType = this.Q<Label>(nameof(ItemRarityAndType));
            WeaponGroup = this.Q<VisualElement>(nameof(WeaponGroup));
            Damage = this.Q<Label>(nameof(Damage));
            Stamina = this.Q<Label>(nameof(Stamina));
            Accuracy = this.Q<Label>(nameof(Accuracy));
            Cooldown = this.Q<Label>(nameof(Cooldown));

            this.PickIgnoreAll();
        }

        VisualElement WeaponGroup;
        Label Damage, Stamina, Accuracy, Cooldown, ItemRarityAndType;

        public override void Render(IHoveredItemContext context) {
            var item = context.Item.Base as Backpack_ItemBase;
            ClearClassList();
            AddToClassList("tooltip");
            AddToClassList(item.Rarity.ToString());

            ItemName.text = item.Name;
            ItemRarityAndType.text = $"{item.Rarity} {item.ItemType}";
            WeaponGroup.SetVisible(item is Backpack_WeaponBase);
            if (item is Backpack_WeaponBase b) {
                Damage.text = Dps(b);
                Stamina.text = Sps(b);
                Accuracy.text = Acc(b);
                Cooldown.text = $"{b.Cooldown}s";
            }
        }

        string Dps(Backpack_WeaponBase b) => $"{b.Damage.Min}-{b.Damage.Max} ({b.DPS:0.0}/s)";
        string Sps(Backpack_WeaponBase b) => $"{b.Stamina} ({b.SPS:0.0}/s)";
        string Acc(Backpack_WeaponBase b) => $"{b.Accuracy * 100:0}%";
    }

}