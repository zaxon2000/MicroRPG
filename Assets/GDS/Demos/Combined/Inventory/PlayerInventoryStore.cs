using GDS.Core;

namespace GDS.Demos.Combined {

    /// <summary>
    /// Minimal runtime-only Store used solely to carry the EventBus and Ghost observable
    /// required by DragDropManipulator, RotateGhostManipulator, and ShopView.
    /// Owns no inventory data — all bags and state live on <see cref="PlayerInventory"/>.
    /// Created via ScriptableObject.CreateInstance at runtime by PlayerInventory; never saved to disk.
    /// </summary>
    public class PlayerInventoryStore : Store { }

}
