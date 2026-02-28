using System.Collections.Generic;
using System.Linq;
using GDS.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Demos.Dayz {

#if UNITY_6000_0_OR_NEWER
    [UxmlElement]    
#endif

    public partial class EquipmentView : VisualElement {

#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<EquipmentView> { }
#endif

        public EquipmentView() {
            var uxml = Resources.Load<VisualTreeAsset>("DayzEquipmentView");
            uxml.CloneTree(this);

            equipmentBagView = this.Q<SetBagView>();
            equipmentContainer = this.Q<VisualElement>("EquipmentContainer");
        }

        Equipment Equipment;
        SetBagView equipmentBagView;
        VisualElement equipmentContainer;
        Store Store;

        Dictionary<string, ContainerItemView> containerItemViewsDict;

        public void Init(Equipment equipment, Store store) {
            Equipment = equipment;
            Store = store;

            equipmentBagView.Init(equipment);

            containerItemViewsDict = equipment.Slots.ToDictionary(s => s.Key, s => TryCreateContainerItemView(s));
            var views = containerItemViewsDict.Values.Where(x => x != null);
            foreach (var view in views) { equipmentContainer.Add(view); }
            equipment.ItemChanged += OnItemChanged;
        }

        ContainerItemView TryCreateContainerItemView(SetSlot slot) {
            var index = Equipment.Slots.FindIndex(s => s.Key == slot.Key);
            if (slot.Item is Dayz_ContainerItem c) return new ContainerItemView(c, Store) { Index = index };
            return null;
        }

        void OnItemChanged(SetSlot slot) {
            if (slot.Empty()) {
                // Debug.Log("Should remove container item view");
                var view = containerItemViewsDict.GetValueOrDefault(slot.Key);
                if (view == null) { Debug.LogWarning($"could not find view for key {slot.Key}"); return; }
                equipmentContainer.Remove(view);
                containerItemViewsDict[slot.Key] = null;
            } else {
                // Debug.Log("should add new view to container");
                if (containerItemViewsDict[slot.Key] != null) {
                    equipmentContainer.Remove(containerItemViewsDict[slot.Key]);
                    containerItemViewsDict[slot.Key] = null;
                }
                var view = TryCreateContainerItemView(slot);
                if (view == null) { Debug.LogWarning($"could not create view for key {slot.Key}. Container item required!"); return; }
                equipmentContainer.Add(view);
                containerItemViewsDict[slot.Key] = view;

                equipmentContainer.Sort(ContainerSortFunction);
            }
        }

        int ContainerSortFunction(VisualElement a, VisualElement b) {
            if (a is not ContainerItemView ca) return 0;
            if (b is not ContainerItemView cb) return 0;
            return ca.Index > cb.Index ? 1 : -1;
        }
    }
}