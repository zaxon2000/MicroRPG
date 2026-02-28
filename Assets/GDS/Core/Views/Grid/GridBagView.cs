using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Core {

#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif

    public partial class GridBagView : VisualElement, IItemContext, IHoveredItemContext {

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute] public int PreviewCellSize { get => previewCellSize; set => SetPreviewCellSize(value); }
        [UxmlAttribute] public int PreviewGridWidth { get => previewGridWidth; set => SetPreviewGridWidth(value); }
        [UxmlAttribute] public int PreviewGridHeight { get => previewGridHeight; set => SetPreviewGridHeight(value); }
        [UxmlAttribute] public bool ShowPreviewItems { get => showPreviewItems; set => SetShowPreviewItems(value); }
#endif

#if !UNITY_6000_0_OR_NEWER    
        public new class UxmlFactory : UxmlFactory<GridBagView, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits {
            UxmlIntAttributeDescription PreviewCellSize = new() { name = "preview-cell-size", defaultValue = DefaultCellSize };
            UxmlIntAttributeDescription PreviewGridWidth = new() { name = "preview-grid-width", defaultValue = DefaultGridWidth };
            UxmlIntAttributeDescription PreviewGridHeight = new() { name = "preview-grid-height", defaultValue = DefaultGridHeight };
            UxmlBoolAttributeDescription ShowPreviewItems = new() { name = "show-preview-items", defaultValue = DefaultShowPreviewItems };
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
                base.Init(ve, bag, cc);
                ((GridBagView)ve).SetPreviewCellSize(PreviewCellSize.GetValueFromBag(bag, cc));
                ((GridBagView)ve).SetPreviewGridWidth(PreviewGridWidth.GetValueFromBag(bag, cc));
                ((GridBagView)ve).SetPreviewGridHeight(PreviewGridHeight.GetValueFromBag(bag, cc));
                ((GridBagView)ve).SetShowPreviewItems(ShowPreviewItems.GetValueFromBag(bag, cc));
            }
        }

        public int PreviewCellSize { get => previewCellSize; set => SetPreviewCellSize(value); }
        public int PreviewGridWidth { get => previewGridWidth; set => SetPreviewGridWidth(value); }
        public int PreviewGridHeight { get => previewGridHeight; set => SetPreviewGridHeight(value); }
        public bool ShowPreviewItems { get => showPreviewItems; set => SetShowPreviewItems(value); }
#endif

        public const int DefaultCellSize = 64;
        public const int DefaultGridWidth = 3;
        public const int DefaultGridHeight = 3;
        public const bool DefaultShowPreviewItems = true;

        int previewCellSize = DefaultCellSize;
        int previewGridWidth = DefaultGridWidth;
        int previewGridHeight = DefaultGridHeight;
        bool showPreviewItems = DefaultShowPreviewItems;
        int CellSize = DefaultCellSize;

        bool localPosValid = false;
        GridBag bag;
        Observable<Item> ghost;
        Vector3 lastLocalPos;
        Pos lastGridCoord;
        Pos gridGhostPos;
        Item lastHoveredItem;
        List<Item> overlapping = new();
        Dictionary<Item, ItemView> itemViewsDict = new();
        IEnumerable<ItemView> overlappingItemViews => overlapping.Select(itemViewsDict.GetValueOrDefault);
        ItemView lastHoveredItemView => lastHoveredItem == null ? null : itemViewsDict.GetValueOrDefault(lastHoveredItem);

        VisualElement slotContainer = Dom.Div("absolute slot-container");
        VisualElement itemContainer = Dom.Div("absolute item-container");
        GridGhostView gridGhostView = new GridGhostView().WithClass("absolute grid-ghost-view");

        Label debugLabel = Dom.Label("debug-label-grid", "debug");
        public Func<ItemView> CreateItemView = () => new GridItemView();

        public GridBagView() {
            this.Add("grid-bag", slotContainer, itemContainer, gridGhostView, debugLabel);
            // Workaround for Unity 6 reattaching the document when selecting UI Document in hierarchy
            RegisterCallback<DetachFromPanelEvent>(_ => UnregisterEvents());
            RegisterCallback<AttachToPanelEvent>(_ => RegisterEvents());

            if (bag == null) RenderPreview();
        }

        public GridBagView Init(GridBag bag, Observable<Item> ghost, bool hasIrregularShapes = true) {
            this.ghost = ghost;
            if (!bag.Initialized) bag.Init();
            // bag.Init();
            SetBagValue(bag);
            gridGhostView.Init(CellSize, hasIrregularShapes);
            this.Observe(ghost, OnGhostChanged);
            return this;
        }

        public void SetPreviewCellSize(int value) {
            previewCellSize = value;
            RenderPreview();
        }
        public void SetPreviewGridWidth(int value) {
            previewGridWidth = value;
            RenderPreview();
        }
        public void SetPreviewGridHeight(int value) {
            previewGridHeight = value;
            RenderPreview();
        }
        public void SetShowPreviewItems(bool value) {
            showPreviewItems = value;
            RenderPreview();
        }

        void SetBagValue(GridBag value) {
            UnregisterEvents();
            bag = value;
            CellSize = bag.CellSize;
            Build();
            RegisterEvents();
        }

        void Build() {
            var (w, h) = bag.Size;
            this.SetSize(bag.Size, CellSize);
            BuildSlots(w, h, CellSize);
            itemContainer.Clear();
            bag.GridItems.ForEach(item => {
                var itemView = CreateGridItemView(item);
                itemContainer.Add(itemView);
                itemViewsDict.Add(item.Item, itemView);
            });
        }

        ItemView CreateGridItemView(GridItem item) {
            // Debug.Log($"should create grid item: {item.Item}, pos: {item.Pos}");
            var view = CreateItemView();
            if (view is GridItemView gi) gi.CellSize = CellSize;
            if (view is IrregularGridItemView igi) igi.CellSize = CellSize;
            view.Item = item.Item;
            view.AddToClassList("absolute");
            view.Translate(item.Pos.X * CellSize, item.Pos.Y * CellSize);
            return view;
        }

        void BuildSlots(int w, int h, int c) {
            slotContainer.Clear();
            for (var i = 0; i < h; i++)
                for (var j = 0; j < w; j++) {
                    var slotView = new GridSlotView().WithClass("absolute grid-slot").Translate(j * c, i * c);
                    slotView.style.width = slotView.style.height = c;
                    slotContainer.Add(slotView);
                }
        }

        void RenderPreview() {
            this.SetSize(PreviewGridWidth * PreviewCellSize, PreviewGridHeight * PreviewCellSize);
            BuildSlots(PreviewGridWidth, PreviewGridHeight, PreviewCellSize);
            itemContainer.Clear();
            if (ShowPreviewItems == false) return;
            itemContainer.Add(
                Dom.Div("preview-image-backpack").SetSize(PreviewCellSize * 2),
                Dom.Div("preview-image-apple").Translate(PreviewCellSize * 2, 0).SetSize(PreviewCellSize),
                Dom.Div("preview-image-apple").Translate(PreviewCellSize * 2, PreviewCellSize * 1).SetSize(PreviewCellSize)
            );
        }

        void RegisterEvents() {
            if (bag == null) return;
            UnregisterEvents();
            bag.ItemAdded += OnItemAdded;
            bag.ItemRemoved += OnItemRemoved;
            bag.ItemChanged += OnItemChanged;
            bag.CollectionReset += OnCollectionReset;

            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        void UnregisterEvents() {
            if (bag == null) return;
            bag.ItemAdded -= OnItemAdded;
            bag.ItemRemoved -= OnItemRemoved;
            bag.ItemChanged -= OnItemChanged;
            bag.CollectionReset -= OnCollectionReset;

            UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        void OnCollectionReset() {
            itemContainer.Clear();
        }

        void OnGhostChanged(Item item) {
            if (localPosValid == false) return;
            gridGhostView.Build(item);
            UpdateGridGhostView();
            UpdateHoveredItem(lastGridCoord);
        }

        void OnItemAdded(Item item) {
            var gridItem = bag.FindGridItem(item);
            var itemView = CreateGridItemView(gridItem);
            itemContainer.Add(itemView);
            itemViewsDict.Add(item, itemView);
        }

        void OnItemRemoved(Item item) {
            var itemView = itemViewsDict.GetValueOrDefault(item);
            if (itemView == null) { Debug.LogWarning($"OnItemRemoved:: Could not find itemView for item {item}"); return; }
            itemContainer.Remove(itemView);
            itemViewsDict.Remove(item);
        }

        void OnItemChanged(Item item) {
            var itemView = itemViewsDict.GetValueOrDefault(item);
            if (itemView == null) { Debug.Log($"OnItemChanged:: Could not find itemView for item {item}"); return; }
            itemView.Item = item;
        }

        void OnPointerMove(PointerMoveEvent e) {
            localPosValid = true;
            lastLocalPos = e.localPosition;
            UpdateGridGhostView();
            Pos gridCoord = GridMath.ScreenPosToGridPos(e.localPosition, CellSize, bag.Size);
            if (lastGridCoord == gridCoord) return;

            lastGridCoord = gridCoord;
            UpdateHoveredItem(gridCoord);

            debugLabel.Show();
            debugLabel.Translate(gridCoord, CellSize);
            debugLabel.text = gridCoord.ToString();
        }

        void OnPointerLeave(PointerLeaveEvent e) {
            localPosValid = false;
            debugLabel.Hide();
            gridGhostPos = null;
            gridGhostView.Hide();
            foreach (var itemView in overlappingItemViews) itemView.RemoveFromClassList("overlapping");

            if (lastHoveredItem == null) return;
            var lastItemView = itemViewsDict.GetValueOrDefault(lastHoveredItem);
            lastItemView?.RemoveFromClassList("hovered");
            lastHoveredItem = null;

        }

        void UpdateGridGhostView() {
            if (ghost.Value == null) {
                gridGhostPos = null;
                gridGhostView.Hide();
                if (overlapping.Count > 0) {
                    foreach (var itemView in overlappingItemViews) {
                        if (itemView == null) { Debug.LogWarning("item already removed"); continue; }
                        itemView?.RemoveFromClassList("overlapping");
                    }
                    overlapping.Clear();
                }
                return;
            }
            gridGhostPos = bag.CreateGridGhostPos(lastLocalPos, ghost.Value.Size());
            if (gridGhostPos == null) {
                gridGhostView.Hide();
                overlapping.Clear();
                return;
            }

            gridGhostView.Build(ghost.Value);
            gridGhostView.Translate(gridGhostPos);
            gridGhostView.Show();

            foreach (var itemView in overlappingItemViews) {
                if (itemView == null) { Debug.LogWarning("item has been removed"); continue; }
                itemView?.RemoveFromClassList("overlapping");
            }
            var computedShape = GridMath.ComputedShape(bag.Occupancy, ghost.Value.Shape(), gridGhostPos);
            overlapping = bag.GetOverlappingItems(computedShape, gridGhostPos).ToList();
            foreach (var itemView in overlappingItemViews) itemView.AddToClassList("overlapping");

            gridGhostView.EnableInClassList("illegal", overlapping.Count > 1);
        }

        void UpdateHoveredItem(Pos pos) {
            if (pos == null) return;
            Item hoveredItem = bag.GetItemAt(pos);
            if (lastHoveredItem == hoveredItem) return;

            lastHoveredItemView?.RemoveFromClassList("hovered");
            lastHoveredItem = hoveredItem;
            lastHoveredItemView?.AddToClassList("hovered");
        }

        ///////////////////////////////////////
        // IItemContext, IHoveredItemContext //
        ///////////////////////////////////////
        public Bag Bag => bag;
        public Slot Slot => GetSlotContext();
        public Item Item => GetItemContext();
        public Rect WorldBound => GetWorldBound();

        Slot GetSlotContext() {
            if (gridGhostPos == null) return null;
            return bag.GetSlotAt(gridGhostPos);
        }

        Item GetItemContext() {
            return lastHoveredItem;
        }

        Rect GetWorldBound() {
            if (lastHoveredItem == null) return Rect.zero;
            if (lastHoveredItemView == null) return Rect.zero;
            return lastHoveredItemView.worldBound;
        }

    }

    public class GridGhostView : VisualElement {
        int CellSize;
        bool HasIrregularShapes = true;
        Pos lastPos;
        string itemId;
        Direction itemDirection;
        VisualElement cell = Dom.Div("shape-cell");
        public void Init(int cellSize, bool hasIrregularShapes) {
            CellSize = cellSize;
            HasIrregularShapes = hasIrregularShapes;
            if (hasIrregularShapes == false) { Add(cell); }
        }

        public void Build(Item item) {
            if (item == null) { itemId = null; return; }
            if (item.Id == itemId && itemDirection == item.Direction()) return;
            itemId = item.Id;
            itemDirection = item.Direction();
            if (HasIrregularShapes) {
                Clear();
                Add(new ShapeView(item.Shape(), CellSize));
            } else {
                cell.SetSize(item.Size(), CellSize);
            }
        }

        public void Translate(Pos pos) {
            if (pos == lastPos) return;
            lastPos = pos;
            this.Translate(pos, CellSize);
        }
    }

}