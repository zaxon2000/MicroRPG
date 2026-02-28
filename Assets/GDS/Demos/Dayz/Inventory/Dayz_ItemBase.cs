using GDS.Core;
using UnityEngine;

namespace GDS.Demos.Dayz {

    [CreateAssetMenu(menuName = "SO/Demos/Dayz/Dayz_ItemBase", fileName = "Dayz_Item")]
    public class Dayz_ItemBase : ShapeItemBase {
        public float Weight = 1f;
    }

    [System.Serializable]
    public class Dayz_Item : ShapeItem { }

}