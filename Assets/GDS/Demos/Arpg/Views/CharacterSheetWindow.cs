using GDS.Core;
using GDS.Core.Events;

namespace GDS.Demos.Arpg {

    public class CharacterSheetWindow : WindowView {

        public CharacterSheetWindow(CharacterSheet c, Store store) {
            Init("Character Sheet", () => store.Bus.Publish(new CloseWindow(c)));

            AddToClassList("character-sheet");

            var PhysicalDamage = Dom.Label("stat-text", "");
            var AttacksPerSecond = Dom.Label("stat-text", "");
            var DamagePerSecond = Dom.Label("stat-text", "");
            var ChanceToBlock = Dom.Label("stat-text", "");
            var Armor = Dom.Label("stat-text", "");

            Container.Add(
                Dom.Div("row justify-space-between", Dom.Label("stat-text", "Physical Damage:"), PhysicalDamage),
                Dom.Div("row justify-space-between even", Dom.Label("stat-text", "Attacks per Second:"), AttacksPerSecond),
                Dom.Div("row justify-space-between", Dom.Label("stat-text", "Damage per Second:"), DamagePerSecond),
                Dom.Div("row justify-space-between even", Dom.Label("stat-text", "Chance to Block:"), ChanceToBlock),
                Dom.Div("row justify-space-between", Dom.Label("stat-text", "Armor:"), Armor)
            );

            this.Observe(c.Stats, value => {
                PhysicalDamage.text = value.PhysicalDamage.ToString();
                AttacksPerSecond.text = value.AttacksPerSecond.ToString();
                DamagePerSecond.text = value.dps.ToString();
                ChanceToBlock.text = value.BlockChance + "%";
                Armor.text = value.Armor.ToString();

            });
        }
    }

}