using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GDS.Core {

#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif

    // Important Note:
    // Why implement IItemContext here but not in GridItemView?
    // Because GridItemView can have an irregular shape and we'd need to implement IItemContext on each of the cells
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

        GridBag bag;
        Observable<Item> ghost;
        bool localPosValid = false;
        Vector2 lastGlobalPos;
        Vector2 lastLocalPos;
        Pos lastGridCoord;
        Pos gridGhostPos;
        Item lastGhostValue;
        Item lastHoveredItem;
        IVisualElementScheduledItem scheduleId;
        const int PollingInterval = 50;

        List<Item> overlapping = new();
        Dictionary<Item, BaseGridItemView> itemViewsDict = new();
        IEnumerable<BaseGridItemView> overlappingItemViews => overlapping.Select(itemViewsDict.GetValueOrDefault);
        BaseGridItemView lastHoveredItemView => lastHoveredItem == null ? null : itemViewsDict.GetValueOrDefault(lastHoveredItem);

        VisualElement slotContainer = Dom.Div("absolute slot-container");
        VisualElement itemContainer = Dom.Div("absolute item-container");
        GridGhostView gridGhostView = new GridGhostView().WithClass("absolute grid-ghost-view");

        Label debugLabel = Dom.Label("debug-label-grid", "debug");
        public Func<BaseGridItemView> CreateItemView = () => new GridItemView();
        public Dictionary<Item, BaseGridItemView> ItemViewsDict => itemViewsDict;

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
            // this.Observe(ghost, OnGhostChanged);
            return this;
        }

        public void SetPreviewCellSize(int value) { previewCellSize = value; RenderPreview(); }
        public void SetPreviewGridWidth(int value) { previewGridWidth = value; RenderPreview(); }
        public void SetPreviewGridHeight(int value) { previewGridHeight = value; RenderPreview(); }
        public void SetShowPreviewItems(bool value) { showPreviewItems = value; RenderPreview(); }

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

            scheduleId = schedule.Execute(OnTick);
            scheduleId.Every(PollingInterval);
            scheduleId.Pause();
        }

        BaseGridItemView CreateGridItemView(GridItem item) {
            // Debug.Log($"should create grid item: {item.Item}, pos: {item.Pos}");
            var view = CreateItemView();
            view.CellSize = CellSize;
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

            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        void UnregisterEvents() {
            if (bag == null) return;
            bag.ItemAdded -= OnItemAdded;
            bag.ItemRemoved -= OnItemRemoved;
            bag.ItemChanged -= OnItemChanged;
            bag.CollectionReset -= OnCollectionReset;

            UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        void OnCollectionReset() {
            itemContainer.Clear();
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

        void OnPointerEnter(PointerEnterEvent e) {
            localPosValid = true;
            scheduleId.Resume();
        }

        void OnPointerLeave(PointerLeaveEvent e) {
            localPosValid = false;
            scheduleId.Pause();
            Cleanup();
        }

        void OnTick() {
            if (localPosValid == false) { Cleanup(); return; }

            var screenPos = Pointer.current.position.ReadValue();
            screenPos.y = Screen.height - screenPos.y;
            var panelPos = RuntimePanelUtils.ScreenToPanel(panel, screenPos);

            lastGlobalPos = panelPos;
            var newLocalPos = lastGlobalPos - worldBound.min;
            lastLocalPos = newLocalPos;
            lastGridCoord = GridMath.ScreenPosToGridPos(lastLocalPos, CellSize, bag.Size);

            UpdateHoveredItem();
            UpdateGhostView();
            UpdateOverlappingItems();
            UpdateDebugView();

        }

        void Cleanup() {
            debugLabel.Hide();
            gridGhostView.Hide();
            if (overlapping.Count > 0) {
                foreach (var itemView in overlappingItemViews) {
                    if (itemView == null) { Debug.LogWarning("item already removed"); continue; }
                    itemView?.RemoveFromClassList("overlapping");
                }
                overlapping.Clear();
            }
            lastHoveredItemView?.RemoveFromClassList("hovered");
            lastHoveredItem = null;
        }

        void UpdateDebugView() {
            debugLabel.Show();
            debugLabel.Translate(lastGridCoord, CellSize);
            debugLabel.text = lastGridCoord.ToString();
        }

        void UpdateGhostView() {
            if (ghost.Value == null) {
                gridGhostView.Hide();
            } else {
                gridGhostPos = bag.CreateGridGhostPos(lastLocalPos, ghost.Value.Size());
                gridGhostView.Build(ghost.Value);
                gridGhostView.Translate(gridGhostPos);
                gridGhostView.Show();
            }
            lastGhostValue = ghost.Value;
        }

        void UpdateHoveredItem() {
            if (localPosValid == false) { lastHoveredItem = null; return; }
            if (lastGridCoord == null) return;
            Item hoveredItem = bag.GetItemAt(lastGridCoord);
            if (lastHoveredItem == hoveredItem) return;

            lastHoveredItemView?.RemoveFromClassList("hovered");
            lastHoveredItem = hoveredItem;
            lastHoveredItemView?.AddToClassList("hovered");
        }

        void UpdateOverlappingItems() {
            foreach (var itemView in overlappingItemViews) {
                if (itemView == null) { Debug.LogWarning("item has been removed"); continue; }
                itemView?.RemoveFromClassList("overlapping");
            }

            if (ghost.Value == null) return;
            var computedShape = GridMath.ComputedShape(bag.Occupancy, ghost.Value.Shape(), gridGhostPos);
            overlapping = bag.GetOverlappingItems(computedShape, gridGhostPos).ToList();
            foreach (var itemView in overlappingItemViews) itemView?.AddToClassList("overlapping");

            gridGhostView.EnableInClassList("illegal", overlapping.Count > 1);
        }

        ///////////////////////////////////////
        // IItemContext, IHoveredItemContext //
        ///////////////////////////////////////
        public Bag Bag => bag;
        public Slot Slot => gridGhostPos == null ? null : bag.GetSlotAt(gridGhostPos);
        public Item Item => lastHoveredItem;
        public Rect WorldBound => lastHoveredItemView?.worldBound ?? Rect.zero;
    }

}