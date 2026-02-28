namespace GDS.Core {
    public class GridSlot : Slot {
        public Pos Pos;
        public override string ToString() => $"Pos = {Pos}, Item = {Item}";
    }
}