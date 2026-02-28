using System.Collections.Generic;
using GDS.Core;
using UnityEngine;

namespace GDS.Demos.Dayz {

    [CreateAssetMenu(menuName = "SO/Demos/Dayz/Dayz_ContainerItemBase")]
    public class Dayz_ContainerItemBase : Dayz_ItemBase {
        public List<Dayz_Tag> ListCapacity;
        public Size GridCapacity;

        public override Item CreateItem() => Dayz_ContainerItem.Create(this);
    }

}