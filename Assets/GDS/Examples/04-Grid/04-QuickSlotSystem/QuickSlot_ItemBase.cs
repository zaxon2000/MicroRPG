using GDS.Core;

namespace GDS.Examples {

    public class QuickSlot_ItemBase : ShapeItemBase {
        public override Item CreateItem() => new QuickSlot_Item { Base = this, Name = Name, };
    }

    public class QuickSlot_Item : ShapeItem { }


    public static class QuickSlot_ItemExt {
        public static Item Clone(this Item item, bool keepId = false) {
            if (item is QuickSlot_Weapon i) return new QuickSlot_Weapon { Base = i.Base, Name = i.Name, Ammo = i.Ammo, Id = i.Id };
            return item.Clone(keepId);
        }

        public static bool CopyFrom(this Item target, Item source) {
            if (target is not QuickSlot_Weapon t || source is not QuickSlot_Weapon s) return false;
            t.Ammo = s.Ammo;
            return true;
        }
    }

}