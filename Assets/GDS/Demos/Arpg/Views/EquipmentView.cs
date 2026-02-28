using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using GDS.Core;

namespace GDS.Demos.Arpg {

#if UNITY_6000_0_OR_NEWER
[UxmlElement]
#endif

    public partial class EquipmentView : VisualElement {

#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<EquipmentView> { }
#endif

        public EquipmentView() {
            var uxml = Resources.Load<VisualTreeAsset>("EquipmentView");
            uxml.CloneTree(this);

            List<string> list = new() { "Weapon1", "Weapon2", "Helmet", "Body", "Boots", "Gloves", "RingLeft", "RingRight", };
            slotViewsDict = list.ToDictionary(k => k, k => this.Q<SlotView>(k));
        }

        Equipment bag;
        Dictionary<string, SlotView> slotViewsDict;

        public void Init(Equipment equipment) {
            bag = equipment;
            equipment.ItemChanged += OnItemChanged;
            foreach (var s in bag.Slots) {
                slotViewsDict[s.Key].Init(bag, s);
            }
        }

        void OnItemChanged(SetSlot slot) {
            slotViewsDict[slot.Key].Render();
        }
    }
}