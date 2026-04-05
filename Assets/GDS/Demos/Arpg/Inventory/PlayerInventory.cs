namespace GDS.Demos.Arpg {
    [System.Serializable]
    public class PlayerInventory {
        public Equipment Equipment = new();
        public Inventory Inventory = new();
    }
}