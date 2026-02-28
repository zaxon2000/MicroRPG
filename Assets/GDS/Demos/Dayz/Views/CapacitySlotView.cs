using System.Collections;
using System.Collections.Generic;
using GDS.Core;
using UnityEngine;

namespace GDS.Demos.Dayz {

    public class CapacitySlotView : SlotView {
        public override void Render() {
            base.Render();

            if (slot.Tags.Count > 0) AddToClassList(slot.Tags[0].name);
        }
    }

}