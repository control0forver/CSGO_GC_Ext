using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Reactive;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace CSGO_GC_Ext.Utils.Views;

/// <summary>
/// Virtualizing panel that arranges children in a wrap layout with virtualization support.
/// </summary>
public class _Bad_VirtualizingWrapPanel : VirtualizingPanel, INavigableContainer
{
    #region Dependency Properties

    /// <summary>
    /// Defines the <see cref="ItemSpacing"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<_Bad_VirtualizingWrapPanel, double>(nameof(ItemSpacing));

    /// <summary>
    /// Defines the <see cref="LineSpacing"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LineSpacingProperty =
        AvaloniaProperty.Register<_Bad_VirtualizingWrapPanel, double>(nameof(LineSpacing));

    /// <summary>
    /// Defines the <see cref="Orientation"/> property.
    /// </summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<_Bad_VirtualizingWrapPanel, Orientation>(nameof(Orientation), Orientation.Horizontal);

    /// <summary>
    /// Defines the <see cref="ItemsAlignment"/> property.
    /// </summary>
    public static readonly StyledProperty<WrapPanelItemsAlignment> ItemsAlignmentProperty =
        AvaloniaProperty.Register<_Bad_VirtualizingWrapPanel, WrapPanelItemsAlignment>(nameof(ItemsAlignment), WrapPanelItemsAlignment.Start);

    /// <summary>
    /// Defines the <see cref="ItemWidth"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ItemWidthProperty =
        AvaloniaProperty.Register<_Bad_VirtualizingWrapPanel, double>(nameof(ItemWidth), double.NaN);

    /// <summary>
    /// Defines the <see cref="ItemHeight"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ItemHeightProperty =
        AvaloniaProperty.Register<_Bad_VirtualizingWrapPanel, double>(nameof(ItemHeight), double.NaN);

    /// <summary>
    /// Defines the <see cref="CacheLength"/> property.
    /// </summary>
    public static readonly StyledProperty<double> CacheLengthProperty =
        AvaloniaProperty.Register<_Bad_VirtualizingWrapPanel, double>(nameof(CacheLength), 0.0);

    static _Bad_VirtualizingWrapPanel()
    {
        AffectsMeasure<_Bad_VirtualizingWrapPanel>(
            ItemSpacingProperty,
            LineSpacingProperty,
            OrientationProperty,
            ItemWidthProperty,
            ItemHeightProperty,
            CacheLengthProperty);

        AffectsArrange<_Bad_VirtualizingWrapPanel>(ItemsAlignmentProperty);
    }

    /// <summary>
    /// Gets or sets the spacing between items.
    /// </summary>
    public double ItemSpacing
    {
        get => GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets the spacing between lines.
    /// </summary>
    public double LineSpacing
    {
        get => GetValue(LineSpacingProperty);
        set => SetValue(LineSpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets the orientation in which child controls will be laid out.
    /// </summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>
    /// Gets or sets the alignment of items in the WrapPanel.
    /// </summary>
    public WrapPanelItemsAlignment ItemsAlignment
    {
        get => GetValue(ItemsAlignmentProperty);
        set => SetValue(ItemsAlignmentProperty, value);
    }

    /// <summary>
    /// Gets or sets the width of all items in the WrapPanel.
    /// </summary>
    public double ItemWidth
    {
        get => GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the height of all items in the WrapPanel.
    /// </summary>
    public double ItemHeight
    {
        get => GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the cache length for virtualization.
    /// </summary>
    public double CacheLength
    {
        get => GetValue(CacheLengthProperty);
        set => SetValue(CacheLengthProperty, value);
    }

    #endregion

    #region Private Fields

    private readonly Action<Control, int> _recycleElement;
    private readonly Action<Control> _recycleElementOnItemRemoved;
    private readonly Action<Control, int, int> _updateElementIndex;

    private bool _isInLayout;
    private bool _isWaitingForViewportUpdate;
    private Rect _viewport;
    private Rect _extendedViewport;
    private double _bufferFactor;
    private IScrollAnchorProvider? _scrollAnchorProvider;

    // Realized elements management
    private RealizedWrapElements? _realizedElements;
    private RealizedWrapElements? _measureElements;

    // Special elements
    private int _scrollToIndex = -1;
    private Control? _scrollToElement;
    private int _focusedIndex = -1;
    private Control? _focusedElement;
    private int _realizingIndex = -1;
    private Control? _realizingElement;

    // Recycling
    private Dictionary<object, Stack<Control>>? _recyclePool;
    private static readonly AttachedProperty<object?> RecycleKeyProperty =
        AvaloniaProperty.RegisterAttached<_Bad_VirtualizingWrapPanel, Control, object?>("RecycleKey");
    private static readonly object s_itemIsItsOwnContainer = new object();

    // Layout estimation
    private double _lastEstimatedElementWidth = 50;
    private double _lastEstimatedElementHeight = 50;
    private List<LineInfo> _lineLayout = new List<LineInfo>();

    // Cache for frequently accessed values
    private Size _lastAvailableSize;
    private int _lastItemCount;
    private bool _lineLayoutValid = false;

    #endregion

    #region Constructor

    public _Bad_VirtualizingWrapPanel()
    {
        _recycleElement = RecycleElement;
        _recycleElementOnItemRemoved = RecycleElementOnItemRemoved;
        _updateElementIndex = UpdateElementIndex;

        _bufferFactor = Math.Max(0, CacheLength);
        EffectiveViewportChanged += OnEffectiveViewportChanged;
    }

    #endregion

    #region Override Methods

    protected override Size MeasureOverride(Size availableSize)
    {
        var items = Items;

        if (items.Count == 0)
            return default;

        var orientation = Orientation;

        // If we're bringing an item into view, ignore any layout passes until we receive a new
        // effective viewport.
        if (_isWaitingForViewportUpdate)
            return EstimateDesiredSize(orientation, items.Count);

        _isInLayout = true;

        try
        {
            _realizedElements?.ValidateStartPosition(Orientation);
            _realizedElements ??= new RealizedWrapElements();
            _measureElements ??= new RealizedWrapElements();

            // Estimate element size if needed
            EstimateElementSize();

            // DispatchUpdate line layout information
            UpdateLineLayout(availableSize, items.Count);

            // Calculate which items should be visible based on the viewport
            var viewport = CalculateMeasureViewport(orientation, items, availableSize);

            // If the viewport is disjunct then we can recycle everything.
            if (viewport.viewportIsDisjunct)
                _realizedElements.RecycleAllElements(_recycleElement);

            // Do the measure, creating/recycling elements as necessary to fill the viewport.
            RealizeElements(items, availableSize, ref viewport);

            // Now swap the measureElements and realizedElements collection.
            (_measureElements, _realizedElements) = (_realizedElements, _measureElements);
            _measureElements.ResetForReuse();

            // If there is a focused element is outside the visible viewport, ensure it's measured.
            _focusedElement?.Measure(availableSize);

            return CalculateDesiredSize(orientation, items.Count, viewport, availableSize);
        }
        finally
        {
            _isInLayout = false;
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_realizedElements is null)
            return default;

        _isInLayout = true;

        try
        {
            var orientation = Orientation;

            // Arrange all realized elements
            for (var i = 0; i < _realizedElements.Count; ++i)
            {
                var e = _realizedElements.Elements[i];
                var position = _realizedElements.Positions[i];

                if (e is not null)
                {
                    e.Arrange(new Rect(position, e.DesiredSize));

                    if (_viewport.Intersects(new Rect(position, e.DesiredSize)))
                        _scrollAnchorProvider?.RegisterAnchorCandidate(e);
                }
            }

            // Ensure that the focused element is in the correct position.
            if (_focusedElement is not null && _focusedIndex >= 0)
            {
                var position = CalculateElementPosition(_focusedIndex, finalSize);
                _focusedElement.Arrange(new Rect(position, _focusedElement.DesiredSize));
            }

            return finalSize;
        }
        finally
        {
            _isInLayout = false;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _scrollAnchorProvider = this.FindAncestorOfType<IScrollAnchorProvider>();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _scrollAnchorProvider = null;
    }

    protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
    {
        InvalidateMeasure();

        if (_realizedElements is null)
            return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                _realizedElements.ItemsInserted(e.NewStartingIndex, e.NewItems!.Count, _updateElementIndex);
                break;
            case NotifyCollectionChangedAction.Remove:
                _realizedElements.ItemsRemoved(e.OldStartingIndex, e.OldItems!.Count, _updateElementIndex, _recycleElementOnItemRemoved);
                break;
            case NotifyCollectionChangedAction.Replace:
                _realizedElements.ItemsReplaced(e.OldStartingIndex, e.OldItems!.Count, _recycleElementOnItemRemoved);
                break;
            case NotifyCollectionChangedAction.Move:
                if (e.OldStartingIndex < 0)
                {
                    goto case NotifyCollectionChangedAction.Reset;
                }

                _realizedElements.ItemsRemoved(e.OldStartingIndex, e.OldItems!.Count, _updateElementIndex, _recycleElementOnItemRemoved);
                var insertIndex = e.NewStartingIndex;

                if (e.NewStartingIndex > e.OldStartingIndex)
                {
                    insertIndex -= e.OldItems.Count - 1;
                }

                _realizedElements.ItemsInserted(insertIndex, e.NewItems!.Count, _updateElementIndex);
                break;
            case NotifyCollectionChangedAction.Reset:
                _realizedElements.ItemsReset(_recycleElementOnItemRemoved);
                break;
        }
    }

    protected override Control? ScrollIntoView(int index)
    {
        var items = Items;

        if (_isInLayout || index < 0 || index >= items.Count || _realizedElements is null || !IsEffectivelyVisible)
            return null;

        if (GetRealizedElement(index) is Control element)
        {
            element.BringIntoView();
            return element;
        }
        else if (this.GetVisualRoot() is ILayoutRoot root)
        {
            object? GetLayoutManagerViaReflection(ILayoutRoot root)
            {
                try
                {
                    var property = typeof(ILayoutRoot).GetProperty("LayoutManager",
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    return property?.GetValue(root);
                }
                catch
                {
                    return null;
                }
            }
            void ExecuteLayoutPassViaReflection(object layoutManager)
            {
                try
                {
                    var method = layoutManager.GetType().GetMethod("ExecuteLayoutPass",
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    method?.Invoke(layoutManager, null);
                }
                catch
                {
                    // 处理异常
                }
            }

            var _layout_mgr = GetLayoutManagerViaReflection(root);
            if (_layout_mgr is null)
                throw new($"LayoutManager not exists in {nameof(root)}({root.GetType()})");

            // Create and measure the element to be brought into view
            var scrollToElement = GetOrCreateElement(items, index);
            scrollToElement.Measure(Size.Infinity);

            // Get the expected position of the element
            var position = CalculateElementPosition(index, DesiredSize);
            scrollToElement.Arrange(new Rect(position, scrollToElement.DesiredSize));

            // Store the element and index for the layout pass
            _scrollToElement = scrollToElement;
            _scrollToIndex = index;

            // Force a layout pass if needed
            if (!Bounds.Contains(new Rect(position, scrollToElement.DesiredSize)) &&
                !_viewport.Contains(new Rect(position, scrollToElement.DesiredSize)))
            {
                _isWaitingForViewportUpdate = true;
                ExecuteLayoutPassViaReflection(_layout_mgr);
                _isWaitingForViewportUpdate = false;
            }

            // Bring the item into view
            scrollToElement.BringIntoView();
            _isWaitingForViewportUpdate = !_viewport.Contains(new Rect(position, scrollToElement.DesiredSize));
            ExecuteLayoutPassViaReflection(_layout_mgr);

            if (_isWaitingForViewportUpdate)
            {
                _isWaitingForViewportUpdate = false;
                InvalidateMeasure();
                ExecuteLayoutPassViaReflection(_layout_mgr);
            }

            scrollToElement.BringIntoView();
            _scrollToElement = null;
            _scrollToIndex = -1;
            return scrollToElement;
        }

        return null;
    }

    protected override Control? ContainerFromIndex(int index)
    {
        if (index < 0 || index >= Items.Count)
            return null;
        if (_scrollToIndex == index)
            return _scrollToElement;
        if (_focusedIndex == index)
            return _focusedElement;
        if (index == _realizingIndex)
            return _realizingElement;
        if (GetRealizedElement(index) is { } realized)
            return realized;
        if (Items[index] is Control c && c.GetValue(RecycleKeyProperty) == s_itemIsItsOwnContainer)
            return c;
        return null;
    }

    protected override int IndexFromContainer(Control container)
    {
        if (container == _scrollToElement)
            return _scrollToIndex;
        if (container == _focusedElement)
            return _focusedIndex;
        if (container == _realizingElement)
            return _realizingIndex;
        return _realizedElements?.GetIndex(container) ?? -1;
    }

    protected override IEnumerable<Control>? GetRealizedContainers()
    {
        return _realizedElements?.Elements?.Where(x => x is not null).Cast<Control>();
    }

    protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        var count = Items.Count;
        var fromControl = from as Control;

        if (count == 0 ||
            fromControl is null && direction is not NavigationDirection.First and not NavigationDirection.Last)
            return null;

        var fromIndex = fromControl != null ? IndexFromContainer(fromControl) : -1;
        var toIndex = fromIndex;

        switch (direction)
        {
            case NavigationDirection.First:
                toIndex = 0;
                break;
            case NavigationDirection.Last:
                toIndex = count - 1;
                break;
            case NavigationDirection.Next:
                ++toIndex;
                break;
            case NavigationDirection.Previous:
                --toIndex;
                break;
            case NavigationDirection.Left:
            case NavigationDirection.Right:
            case NavigationDirection.Up:
            case NavigationDirection.Down:
                // For wrap panel, we need more complex navigation logic
                // This is a simplified implementation
                toIndex = GetNextIndexInDirection(direction, fromIndex, wrap);
                break;
            default:
                return null;
        }

        if (fromIndex == toIndex)
            return from;

        return ScrollIntoView(toIndex);
    }

    IInputElement? INavigableContainer.GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        return GetControl(direction, from, wrap);
    }

    #endregion

    #region Private Methods

    private MeasureViewport CalculateMeasureViewport(Orientation orientation, IReadOnlyList<object?> items, Size availableSize)
    {
        Debug.Assert(_realizedElements is not null);

        // Use the extended viewport for calculations
        var viewport = _extendedViewport;

        // Get the viewport in the orientation direction
        var viewportStart = orientation == Orientation.Horizontal ? viewport.Y : viewport.X;
        var viewportEnd = orientation == Orientation.Horizontal ? viewport.Bottom : viewport.Right;

        // Estimate which items should be visible based on the viewport
        int anchorIndex;
        Point anchorPosition;

        if (_scrollToIndex >= 0 && _scrollToElement is not null)
        {
            anchorIndex = _scrollToIndex;
            anchorPosition = _scrollToElement.Bounds.Position;
        }
        else
        {
            GetOrEstimateAnchorElementForViewport(
                viewportStart,
                viewportEnd,
                items.Count,
                availableSize,
                out anchorIndex,
                out anchorPosition);
        }

        // Check if the anchor element is not within the currently realized elements
        var disjunct = anchorIndex < _realizedElements.FirstIndex ||
                       anchorIndex > _realizedElements.LastIndex;

        return new MeasureViewport
        {
            anchorIndex = anchorIndex,
            anchorPosition = anchorPosition,
            viewportStart = viewportStart,
            viewportEnd = viewportEnd,
            viewportIsDisjunct = disjunct,
        };
    }

    private void GetOrEstimateAnchorElementForViewport(
        double viewportStart,
        double viewportEnd,
        int itemCount,
        Size availableSize,
        out int index,
        out Point position)
    {
        // We have no elements, or we're at the start of the viewport
        if (itemCount <= 0 || MathUtilities.IsZero(viewportStart))
        {
            index = 0;
            position = new Point(0, 0);
            return;
        }

        // Estimate which line contains the viewport start
        double currentPos = 0;
        int currentLine = 0;

        while (currentPos < viewportStart && currentLine < _lineLayout.Count)
        {
            currentPos += _lineLayout[currentLine].Size + LineSpacing;
            currentLine++;
        }

        if (currentLine >= _lineLayout.Count)
        {
            currentLine = _lineLayout.Count - 1;
            if (currentLine < 0) currentLine = 0;
        }

        // Estimate the item at the start of the viewport
        var lineInfo = _lineLayout[currentLine];
        var itemsPerLine = lineInfo.ItemCount;
        var startIndex = currentLine * itemsPerLine;

        index = Math.Min(startIndex, itemCount - 1);
        position = CalculateElementPosition(index, availableSize);
    }

    private Point CalculateElementPosition(int index, Size availableSize)
    {
        // Calculate the position of an element based on its index
        // This is a simplified implementation - a real implementation would need
        // to calculate the exact position based on the wrap layout

        var orientation = Orientation;
        var itemWidth = double.IsNaN(ItemWidth) ? _lastEstimatedElementWidth : ItemWidth;
        var itemHeight = double.IsNaN(ItemHeight) ? _lastEstimatedElementHeight : ItemHeight;

        if (orientation == Orientation.Horizontal)
        {
            var itemsPerRow = Math.Max(1, (int)Math.Floor(availableSize.Width / (itemWidth + ItemSpacing)));
            var row = index / itemsPerRow;
            var col = index % itemsPerRow;

            return new Point(col * (itemWidth + ItemSpacing), row * (itemHeight + LineSpacing));
        }
        else
        {
            var itemsPerColumn = Math.Max(1, (int)Math.Floor(availableSize.Height / (itemHeight + ItemSpacing)));
            var col = index / itemsPerColumn;
            var row = index % itemsPerColumn;

            return new Point(col * (itemWidth + LineSpacing), row * (itemHeight + ItemSpacing));
        }
    }

    private void RealizeElements(
        IReadOnlyList<object?> items,
        Size availableSize,
        ref MeasureViewport viewport)
    {
        Debug.Assert(_measureElements is not null);
        Debug.Assert(_realizedElements is not null);
        Debug.Assert(items.Count > 0);

        var orientation = Orientation;
        var index = viewport.anchorIndex;
        var position = viewport.anchorPosition;

        // If the anchor element is at the beginning of, or before, the start of the viewport
        // then we can recycle all elements before it
        if (position.Y <= viewport.viewportStart || position.X <= viewport.viewportStart)
            _realizedElements.RecycleElementsBefore(viewport.anchorIndex, _recycleElement);

        // Cache some frequently used values
        var itemWidth = double.IsNaN(ItemWidth) ? _lastEstimatedElementWidth : ItemWidth;
        var itemHeight = double.IsNaN(ItemHeight) ? _lastEstimatedElementHeight : ItemHeight;

        // Start at the anchor element and move forwards, realizing elements
        do
        {
            _realizingIndex = index;
            var e = GetOrCreateElement(items, index);
            _realizingElement = e;

            e.Measure(availableSize);

            _measureElements!.Add(index, e, position);

            // Calculate next position using cached values
            position = CalculateNextPosition(position, e.DesiredSize, availableSize, index, itemWidth, itemHeight);

            ++index;
            _realizingIndex = -1;
            _realizingElement = null;
        } while ((orientation == Orientation.Horizontal ? position.Y : position.X) < viewport.viewportEnd &&
                 index < items.Count);

        // Store the last index for the desired size calculation
        viewport.lastIndex = index - 1;

        // We can now recycle elements after the last element
        _realizedElements.RecycleElementsAfter(viewport.lastIndex, _recycleElement);

        // Next move backwards from the anchor element, realizing elements
        index = viewport.anchorIndex - 1;
        position = viewport.anchorPosition;

        while ((orientation == Orientation.Horizontal ? position.Y : position.X) > viewport.viewportStart &&
               index >= 0)
        {
            var e = GetOrCreateElement(items, index);
            e.Measure(availableSize);

            _measureElements!.Add(index, e, position);

            // Calculate previous position using cached values
            position = CalculatePreviousPosition(position, e.DesiredSize, availableSize, index, itemWidth, itemHeight);

            --index;
        }

        // We can now recycle elements before the first element
        _realizedElements.RecycleElementsBefore(index + 1, _recycleElement);
    }

    private Point CalculateNextPosition(Point currentPosition, Size elementSize, Size availableSize, int index, double itemWidth, double itemHeight)
    {
        var orientation = Orientation;

        if (orientation == Orientation.Horizontal)
        {
            var nextX = currentPosition.X + itemWidth + ItemSpacing;

            // Check if we need to wrap to the next line
            if (nextX + itemWidth > availableSize.Width)
            {
                return new Point(0, currentPosition.Y + itemHeight + LineSpacing);
            }

            return new Point(nextX, currentPosition.Y);
        }
        else
        {
            var nextY = currentPosition.Y + itemHeight + ItemSpacing;

            // Check if we need to wrap to the next column
            if (nextY + itemHeight > availableSize.Height)
            {
                return new Point(currentPosition.X + itemWidth + LineSpacing, 0);
            }

            return new Point(currentPosition.X, nextY);
        }
    }

    private Point CalculatePreviousPosition(Point currentPosition, Size elementSize, Size availableSize, int index, double itemWidth, double itemHeight)
    {
        var orientation = Orientation;

        if (orientation == Orientation.Horizontal)
        {
            var prevX = currentPosition.X - itemWidth - ItemSpacing;

            // Check if we need to wrap to the previous line
            if (prevX < 0)
            {
                var itemsPerRow = Math.Max(1, (int)Math.Floor(availableSize.Width / (itemWidth + ItemSpacing)));
                var row = (index - 1) / itemsPerRow;
                return new Point((itemsPerRow - 1) * (itemWidth + ItemSpacing), row * (itemHeight + LineSpacing));
            }

            return new Point(prevX, currentPosition.Y);
        }
        else
        {
            var prevY = currentPosition.Y - itemHeight - ItemSpacing;

            // Check if we need to wrap to the previous column
            if (prevY < 0)
            {
                var itemsPerColumn = Math.Max(1, (int)Math.Floor(availableSize.Height / (itemHeight + ItemSpacing)));
                var col = (index - 1) / itemsPerColumn;
                return new Point(col * (itemWidth + LineSpacing), (itemsPerColumn - 1) * (itemHeight + LineSpacing));
            }

            return new Point(currentPosition.X, prevY);
        }
    }

    private Size CalculateDesiredSize(Orientation orientation, int itemCount, in MeasureViewport viewport, Size availableSize)
    {
        // Calculate the total desired size based on the layout
        double totalWidth = 0;
        double totalHeight = 0;

        if (orientation == Orientation.Horizontal)
        {
            var itemWidth = double.IsNaN(ItemWidth) ? _lastEstimatedElementWidth : ItemWidth;
            var itemHeight = double.IsNaN(ItemHeight) ? _lastEstimatedElementHeight : ItemHeight;

            var itemsPerRow = Math.Max(1, (int)Math.Floor(availableSize.Width / (itemWidth + ItemSpacing)));
            var rows = (int)Math.Ceiling((double)itemCount / itemsPerRow);

            totalWidth = availableSize.Width;
            totalHeight = rows * (itemHeight + LineSpacing) - LineSpacing;
        }
        else
        {
            var itemWidth = double.IsNaN(ItemWidth) ? _lastEstimatedElementWidth : ItemWidth;
            var itemHeight = double.IsNaN(ItemHeight) ? _lastEstimatedElementHeight : ItemHeight;

            var itemsPerColumn = Math.Max(1, (int)Math.Floor(availableSize.Height / (itemHeight + ItemSpacing)));
            var columns = (int)Math.Ceiling((double)itemCount / itemsPerColumn);

            totalWidth = columns * (itemWidth + LineSpacing) - LineSpacing;
            totalHeight = availableSize.Height;
        }

        return new Size(totalWidth, totalHeight);
    }

    private Size EstimateDesiredSize(Orientation orientation, int itemCount)
    {
        // Estimate desired size when we're waiting for a viewport update
        if (_scrollToIndex >= 0 && _scrollToElement is not null)
        {
            // We have an element to scroll to, so we can estimate the desired size
            var position = _scrollToElement.Bounds.Position;
            var size = _scrollToElement.DesiredSize;

            if (orientation == Orientation.Horizontal)
            {
                var remaining = itemCount - _scrollToIndex - 1;
                var itemsPerRow = Math.Max(1, (int)Math.Floor(DesiredSize.Width / (size.Width + ItemSpacing)));
                var remainingRows = (int)Math.Ceiling((double)remaining / itemsPerRow);

                return new Size(DesiredSize.Width, position.Y + size.Height + remainingRows * (size.Height + LineSpacing));
            }
            else
            {
                var remaining = itemCount - _scrollToIndex - 1;
                var itemsPerColumn = Math.Max(1, (int)Math.Floor(DesiredSize.Height / (size.Height + ItemSpacing)));
                var remainingColumns = (int)Math.Ceiling((double)remaining / itemsPerColumn);

                return new Size(position.X + size.Width + remainingColumns * (size.Width + LineSpacing), DesiredSize.Height);
            }
        }

        return DesiredSize;
    }

    private void EstimateElementSize()
    {
        if (_realizedElements is null)
            return;

        // Only recalculate if we have a significant number of measured elements
        // This prevents frequent recalculations that can cause stuttering
        const int minElementsForEstimate = 5;
        int measuredElements = 0;
        var totalWidth = 0.0;
        var totalHeight = 0.0;

        // Average the desired size of the realized, measured elements
        foreach (var element in _realizedElements.Elements)
        {
            if (element is null || !element.IsMeasureValid)
                continue;

            totalWidth += element.DesiredSize.Width;
            totalHeight += element.DesiredSize.Height;
            measuredElements++;
        }

        // Only update estimates if we have enough measured elements
        // This prevents excessive recalculations during scrolling
        if (measuredElements >= minElementsForEstimate && measuredElements > 0)
        {
            _lastEstimatedElementWidth = totalWidth / measuredElements;
            _lastEstimatedElementHeight = totalHeight / measuredElements;
        }
    }

    private void UpdateLineLayout(Size availableSize, int itemCount)
    {
        // Only recalculate line layout if necessary
        // This prevents unnecessary recalculations during scrolling
        if (itemCount <= 0)
        {
            _lineLayout.Clear();
            _lineLayoutValid = false;
            return;
        }

        // Check if we can reuse the existing line layout
        if (_lineLayoutValid &&
            _lastItemCount == itemCount &&
            Math.Abs(_lastAvailableSize.Width - availableSize.Width) < 0.1 &&
            Math.Abs(_lastAvailableSize.Height - availableSize.Height) < 0.1)
        {
            // Line layout is still valid, no need to recalculate
            return;
        }

        var orientation = Orientation;
        var itemWidth = double.IsNaN(ItemWidth) ? _lastEstimatedElementWidth : ItemWidth;
        var itemHeight = double.IsNaN(ItemHeight) ? _lastEstimatedElementHeight : ItemHeight;

        _lineLayout.Clear();

        if (orientation == Orientation.Horizontal)
        {
            // Horizontal orientation: items flow left-to-right, then wrap to next line
            if (availableSize.Width > 0)
            {
                var itemsPerLine = Math.Max(1, (int)Math.Floor((availableSize.Width + ItemSpacing) / (itemWidth + ItemSpacing)));
                var lineCount = (int)Math.Ceiling((double)itemCount / itemsPerLine);

                // Pre-allocate list capacity to avoid repeated allocations
                if (_lineLayout.Capacity < lineCount)
                    _lineLayout.Capacity = lineCount;

                for (int i = 0; i < lineCount; i++)
                {
                    var itemsInLine = Math.Min(itemsPerLine, itemCount - i * itemsPerLine);
                    _lineLayout.Add(new LineInfo
                    {
                        ItemCount = itemsInLine,
                        Size = itemHeight
                    });
                }
            }
            else
            {
                // If we don't have a width, assume one item per line
                _lineLayout.Capacity = Math.Max(_lineLayout.Capacity, itemCount);
                for (int i = 0; i < itemCount; i++)
                {
                    _lineLayout.Add(new LineInfo
                    {
                        ItemCount = 1,
                        Size = itemHeight
                    });
                }
            }
        }
        else
        {
            // Vertical orientation: items flow top-to-bottom, then wrap to next column
            if (availableSize.Height > 0)
            {
                var itemsPerColumn = Math.Max(1, (int)Math.Floor((availableSize.Height + ItemSpacing) / (itemHeight + ItemSpacing)));
                var columnCount = (int)Math.Ceiling((double)itemCount / itemsPerColumn);

                // Pre-allocate list capacity to avoid repeated allocations
                if (_lineLayout.Capacity < columnCount)
                    _lineLayout.Capacity = columnCount;

                for (int i = 0; i < columnCount; i++)
                {
                    var itemsInColumn = Math.Min(itemsPerColumn, itemCount - i * itemsPerColumn);
                    _lineLayout.Add(new LineInfo
                    {
                        ItemCount = itemsInColumn,
                        Size = itemWidth
                    });
                }
            }
            else
            {
                // If we don't have a height, assume one item per column
                _lineLayout.Capacity = Math.Max(_lineLayout.Capacity, itemCount);
                for (int i = 0; i < itemCount; i++)
                {
                    _lineLayout.Add(new LineInfo
                    {
                        ItemCount = 1,
                        Size = itemWidth
                    });
                }
            }
        }

        // DispatchUpdate cache
        _lastAvailableSize = availableSize;
        _lastItemCount = itemCount;
        _lineLayoutValid = true;
    }

    private Control GetOrCreateElement(IReadOnlyList<object?> items, int index)
    {
        Debug.Assert(ItemContainerGenerator is not null);

        if ((GetRealizedElement(index) ??
             GetRealizedElement(index, ref _focusedIndex, ref _focusedElement) ??
             GetRealizedElement(index, ref _scrollToIndex, ref _scrollToElement)) is { } realized)
            return realized;

        var item = items[index];
        var generator = ItemContainerGenerator!;

        if (generator.NeedsContainer(item, index, out var recycleKey))
        {
            return GetRecycledElement(item, index, recycleKey) ??
                   CreateElement(item, index, recycleKey);
        }
        else
        {
            return GetItemAsOwnContainer(item, index);
        }
    }

    private Control? GetRealizedElement(int index)
    {
        return _realizedElements?.GetElement(index);
    }

    private static Control? GetRealizedElement(
        int index,
        ref int specialIndex,
        ref Control? specialElement)
    {
        if (specialIndex == index)
        {
            Debug.Assert(specialElement is not null);
            var result = specialElement;
            specialIndex = -1;
            specialElement = null;
            return result;
        }

        return null;
    }

    private Control GetItemAsOwnContainer(object? item, int index)
    {
        Debug.Assert(ItemContainerGenerator is not null);

        var controlItem = (Control)item!;
        var generator = ItemContainerGenerator!;

        // Check if the item already has the correct settings to avoid unnecessary operations
        var hasRecycleKey = controlItem.IsSet(RecycleKeyProperty);
        var recycleKeyValue = controlItem.GetValue(RecycleKeyProperty);

        if (!hasRecycleKey || recycleKeyValue != s_itemIsItsOwnContainer)
        {
            generator.PrepareItemContainer(controlItem, controlItem, index);
            if (!Children.Contains(controlItem))
            {
                AddInternalChild(controlItem);
            }
            controlItem.SetValue(RecycleKeyProperty, s_itemIsItsOwnContainer);
            generator.ItemContainerPrepared(controlItem, item, index);
        }

        // Only update visibility if needed
        if (!controlItem.IsVisible)
        {
            controlItem.SetCurrentValue(IsVisibleProperty, true);
        }

        return controlItem;
    }

    private Control? GetRecycledElement(object? item, int index, object? recycleKey)
    {
        Debug.Assert(ItemContainerGenerator is not null);

        if (recycleKey is null)
            return null;

        var generator = ItemContainerGenerator!;

        if (_recyclePool?.TryGetValue(recycleKey, out var recyclePool) == true && recyclePool.Count > 0)
        {
            var recycled = recyclePool.Pop();

            // Only update visibility if needed
            if (!recycled.IsVisible)
            {
                recycled.SetCurrentValue(IsVisibleProperty, true);
            }

            generator.PrepareItemContainer(recycled, item, index);
            generator.ItemContainerPrepared(recycled, item, index);
            return recycled;
        }

        return null;
    }

    private Control CreateElement(object? item, int index, object? recycleKey)
    {
        Debug.Assert(ItemContainerGenerator is not null);

        var generator = ItemContainerGenerator!;
        var container = generator.CreateContainer(item, index, recycleKey);

        container.SetValue(RecycleKeyProperty, recycleKey);
        generator.PrepareItemContainer(container, item, index);

        // Only add to children if not already present
        if (!Children.Contains(container))
        {
            AddInternalChild(container);
        }

        generator.ItemContainerPrepared(container, item, index);

        return container;
    }

    private void RecycleElement(Control element, int index)
    {
        Debug.Assert(ItemsControl is not null);
        Debug.Assert(ItemContainerGenerator is not null);

        _scrollAnchorProvider?.UnregisterAnchorCandidate(element);

        var recycleKey = element.GetValue(RecycleKeyProperty);

        if (recycleKey is null)
        {
            // Only remove if the element is actually a child
            if (Children.Contains(element))
            {
                RemoveInternalChild(element);
            }
        }
        else if (recycleKey == s_itemIsItsOwnContainer)
        {
            // Only update visibility if needed
            if (element.IsVisible)
            {
                element.SetCurrentValue(IsVisibleProperty, false);
            }
        }
        else if (KeyboardNavigation.GetTabOnceActiveElement(ItemsControl) == element)
        {
            _focusedElement = element;
            _focusedIndex = index;
        }
        else
        {
            ItemContainerGenerator!.ClearItemContainer(element);
            PushToRecyclePool(recycleKey, element);

            // Only update visibility if needed
            if (element.IsVisible)
            {
                element.SetCurrentValue(IsVisibleProperty, false);
            }
        }
    }

    private void RecycleElementOnItemRemoved(Control element)
    {
        Debug.Assert(ItemContainerGenerator is not null);

        _scrollAnchorProvider?.UnregisterAnchorCandidate(element);

        var recycleKey = element.GetValue(RecycleKeyProperty);

        if (recycleKey is null || recycleKey == s_itemIsItsOwnContainer)
        {
            // Only remove if the element is actually a child
            if (Children.Contains(element))
            {
                RemoveInternalChild(element);
            }
        }
        else
        {
            ItemContainerGenerator!.ClearItemContainer(element);
            PushToRecyclePool(recycleKey, element);

            // Only update visibility if needed
            if (element.IsVisible)
            {
                element.SetCurrentValue(IsVisibleProperty, false);
            }
        }
    }

    private void PushToRecyclePool(object recycleKey, Control element)
    {
        _recyclePool ??= new Dictionary<object, Stack<Control>>();

        if (!_recyclePool.TryGetValue(recycleKey, out var pool))
        {
            pool = new Stack<Control>();
            _recyclePool.Add(recycleKey, pool);
        }

        pool.Push(element);
    }

    private void UpdateElementIndex(Control element, int oldIndex, int newIndex)
    {
        Debug.Assert(ItemContainerGenerator is not null);
        ItemContainerGenerator.ItemContainerIndexChanged(element, oldIndex, newIndex);
    }

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        var orientation = Orientation;
        var oldViewportStart = orientation == Orientation.Horizontal ? _viewport.Y : _viewport.X;
        var oldViewportEnd = orientation == Orientation.Horizontal ? _viewport.Bottom : _viewport.Right;
        var oldExtendedViewportStart = orientation == Orientation.Horizontal ? _extendedViewport.Y : _extendedViewport.X;
        var oldExtendedViewportEnd = orientation == Orientation.Horizontal ? _extendedViewport.Bottom : _extendedViewport.Right;

        // DispatchUpdate current viewport
        _viewport = e.EffectiveViewport.Intersect(new Rect(Bounds.Size));
        _isWaitingForViewportUpdate = false;

        // Calculate buffer sizes based on viewport dimensions
        var viewportSize = orientation == Orientation.Horizontal ? _viewport.Height : _viewport.Width;
        var bufferSize = viewportSize * _bufferFactor;

        // Calculate extended viewport with relative buffers
        var extendedViewportStart = orientation == Orientation.Horizontal ?
            Math.Max(0, _viewport.Y - bufferSize) :
            Math.Max(0, _viewport.X - bufferSize);

        var extendedViewportEnd = orientation == Orientation.Horizontal ?
            Math.Min(Bounds.Height, _viewport.Bottom + bufferSize) :
            Math.Min(Bounds.Width, _viewport.Right + bufferSize);

        Rect extendedViewport;
        if (orientation == Orientation.Horizontal)
        {
            extendedViewport = new Rect(
                _viewport.X,
                extendedViewportStart,
                _viewport.Width,
                extendedViewportEnd - extendedViewportStart);
        }
        else
        {
            extendedViewport = new Rect(
                extendedViewportStart,
                _viewport.Y,
                extendedViewportEnd - extendedViewportStart,
                _viewport.Height);
        }

        // Determine if we need a new measure
        var newViewportStart = orientation == Orientation.Horizontal ? _viewport.Y : _viewport.X;
        var newViewportEnd = orientation == Orientation.Horizontal ? _viewport.Bottom : _viewport.Right;
        var newExtendedViewportStart = orientation == Orientation.Horizontal ? extendedViewport.Y : extendedViewport.X;
        var newExtendedViewportEnd = orientation == Orientation.Horizontal ? extendedViewport.Bottom : extendedViewport.Right;

        // Use a threshold to prevent unnecessary measures for small changes
        const double threshold = 1.0;
        var needsMeasure =
            Math.Abs(oldViewportStart - newViewportStart) > threshold ||
            Math.Abs(oldViewportEnd - newViewportEnd) > threshold ||
            newViewportStart < oldExtendedViewportStart ||
            newViewportEnd > oldExtendedViewportEnd;

        if (needsMeasure)
        {
            _extendedViewport = extendedViewport;
            InvalidateMeasure();
        }
    }

    private int GetNextIndexInDirection(NavigationDirection direction, int currentIndex, bool wrap)
    {
        // Simplified implementation - in a real scenario, this would need to
        // account for the actual layout of items
        switch (direction)
        {
            case NavigationDirection.Left:
                return Math.Max(0, currentIndex - 1);
            case NavigationDirection.Right:
                return Math.Min(Items.Count - 1, currentIndex + 1);
            case NavigationDirection.Up:
                // This would need to calculate the item above based on the layout
                return Math.Max(0, currentIndex - 1);
            case NavigationDirection.Down:
                // This would need to calculate the item below based on the layout
                return Math.Min(Items.Count - 1, currentIndex + 1);
            default:
                return currentIndex;
        }
    }

    #endregion

    #region Helper Classes

    private struct MeasureViewport
    {
        public int anchorIndex;
        public Point anchorPosition;
        public double viewportStart;
        public double viewportEnd;
        public int lastIndex;
        public bool viewportIsDisjunct;
    }

    private class LineInfo
    {
        public int ItemCount { get; set; }
        public double Size { get; set; }
    }

    private class RealizedWrapElements
    {
        private readonly List<int> _indices = new List<int>();
        private readonly List<Control?> _elements = new List<Control?>();
        private readonly List<Point> _positions = new List<Point>();

        // Add a dictionary for faster index lookups
        private readonly Dictionary<int, int> _indexLookup = new Dictionary<int, int>();

        public int Count => _indices.Count;
        public IReadOnlyList<Control?> Elements => _elements;
        public IReadOnlyList<Point> Positions => _positions;
        public int FirstIndex => _indices.Count > 0 ? _indices[0] : -1;
        public int LastIndex => _indices.Count > 0 ? _indices[^1] : -1;

        public void Add(int index, Control element, Point position)
        {
            _indices.Add(index);
            _elements.Add(element);
            _positions.Add(position);
            _indexLookup[index] = _indices.Count - 1;
        }

        public Control? GetElement(int index)
        {
            if (_indexLookup.TryGetValue(index, out var listIndex))
            {
                return _elements[listIndex];
            }
            return null;
        }

        public int GetIndex(Control element)
        {
            var i = _elements.IndexOf(element);
            return i >= 0 ? _indices[i] : -1;
        }

        public Point GetPosition(int index)
        {
            if (_indexLookup.TryGetValue(index, out var listIndex))
            {
                return _positions[listIndex];
            }
            return new Point(double.NaN, double.NaN);
        }

        public void ResetForReuse()
        {
            _indices.Clear();
            _elements.Clear();
            _positions.Clear();
            _indexLookup.Clear();
        }

        public void RecycleAllElements(Action<Control, int> recycleAction)
        {
            for (var i = 0; i < _elements.Count; i++)
            {
                if (_elements[i] is { } element)
                {
                    recycleAction(element, _indices[i]);
                }
            }
            ResetForReuse();
        }

        public void RecycleElementsBefore(int index, Action<Control, int> recycleAction)
        {
            int removeCount = 0;
            while (removeCount < _indices.Count && _indices[removeCount] < index)
            {
                if (_elements[removeCount] is { } element)
                {
                    recycleAction(element, _indices[removeCount]);
                }
                removeCount++;
            }

            if (removeCount > 0)
            {
                // Remove range from lists
                _indices.RemoveRange(0, removeCount);
                _elements.RemoveRange(0, removeCount);
                _positions.RemoveRange(0, removeCount);

                // DispatchUpdate lookup dictionary
                _indexLookup.Clear();
                for (int i = 0; i < _indices.Count; i++)
                {
                    _indexLookup[_indices[i]] = i;
                }
            }
        }

        public void RecycleElementsAfter(int index, Action<Control, int> recycleAction)
        {
            var removeCount = 0;
            var startIndex = _indices.Count - 1;

            for (int i = startIndex; i >= 0; i--)
            {
                if (_indices[i] > index)
                {
                    if (_elements[i] is { } element)
                    {
                        recycleAction(element, _indices[i]);
                    }
                    removeCount++;
                }
                else
                {
                    break;
                }
            }

            if (removeCount > 0)
            {
                // Remove range from lists
                var removeIndex = _indices.Count - removeCount;
                _indices.RemoveRange(removeIndex, removeCount);
                _elements.RemoveRange(removeIndex, removeCount);
                _positions.RemoveRange(removeIndex, removeCount);

                // DispatchUpdate lookup dictionary
                _indexLookup.Clear();
                for (int i = 0; i < _indices.Count; i++)
                {
                    _indexLookup[_indices[i]] = i;
                }
            }
        }

        public void ItemsInserted(int index, int count, Action<Control, int, int> updateAction)
        {
            for (var i = 0; i < _indices.Count; i++)
            {
                if (_indices[i] >= index)
                {
                    _indices[i] += count;
                    if (_elements[i] is { } element)
                    {
                        updateAction(element, _indices[i] - count, _indices[i]);
                    }
                }
            }

            // DispatchUpdate lookup dictionary
            _indexLookup.Clear();
            for (int i = 0; i < _indices.Count; i++)
            {
                _indexLookup[_indices[i]] = i;
            }
        }

        public void ItemsRemoved(int index, int count, Action<Control, int, int> updateAction, Action<Control> recycleAction)
        {
            var i = 0;
            while (i < _indices.Count)
            {
                if (_indices[i] >= index + count)
                {
                    _indices[i] -= count;
                    if (_elements[i] is { } element)
                    {
                        updateAction(element, _indices[i] + count, _indices[i]);
                    }
                    i++;
                }
                else if (_indices[i] >= index)
                {
                    if (_elements[i] is { } element)
                    {
                        recycleAction(element);
                    }
                    _indices.RemoveAt(i);
                    _elements.RemoveAt(i);
                    _positions.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            // DispatchUpdate lookup dictionary
            _indexLookup.Clear();
            for (int j = 0; j < _indices.Count; j++)
            {
                _indexLookup[_indices[j]] = j;
            }
        }

        public void ItemsReplaced(int index, int count, Action<Control> recycleAction)
        {
            for (var i = 0; i < _indices.Count; i++)
            {
                if (_indices[i] >= index && _indices[i] < index + count)
                {
                    if (_elements[i] is { } element)
                    {
                        recycleAction(element);
                    }
                    _elements[i] = null;
                }
            }
        }

        public void ItemsReset(Action<Control> recycleAction)
        {
            foreach (var element in _elements)
            {
                if (element is not null)
                {
                    recycleAction(element);
                }
            }
            ResetForReuse();
        }

        public void ValidateStartPosition(Orientation orientation)
        {
            // This would validate that our start position is correct
            // In a real implementation, this would ensure our layout is consistent
        }
    }

    #endregion
}

//public class VirtualizingWrapPanel : WrapPanel, IVirtualizingPanel, IScrollUnit
//{
//    private Size _availableSpace;
//    private double _takenSpace;
//    private int _canBeRemoved;
//    private double _averageItemSize;
//    private int _averageCount;
//    private double _pixelOffset;
//    private double _crossAxisOffset;
//    private bool _forceRemeasure;
//    private List<double> _lineLengths = new List<double>();
//    private double _takenLineSpace;

//    bool IVirtualizingPanel.IsFull
//    {
//        get
//        {
//            return Orientation == Orientation.Vertical ?
//                _takenSpace >= _availableSpace.Width :
//                _takenSpace >= _availableSpace.Height;
//        }
//    }

//    IVirtualizingController IVirtualizingPanel.Controller { get; set; }
//    int IVirtualizingPanel.OverflowCount => _canBeRemoved;
//    Orientation IVirtualizingPanel.ScrollDirection => Orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
//    double IVirtualizingPanel.AverageItemSize => _averageItemSize;

//    double IVirtualizingPanel.PixelOverflow
//    {
//        get
//        {
//            var bounds = Orientation == Orientation.Vertical ?
//                _availableSpace.Width : _availableSpace.Height;
//            return Math.Max(0, _takenSpace - bounds);
//        }
//    }

//    double IVirtualizingPanel.PixelOffset
//    {
//        get { return _pixelOffset; }
//        set
//        {
//            if (_pixelOffset != value)
//            {
//                _pixelOffset = value;
//                InvalidateArrange();
//            }
//        }
//    }

//    double IVirtualizingPanel.CrossAxisOffset
//    {
//        get { return _crossAxisOffset; }
//        set
//        {
//            if (_crossAxisOffset != value)
//            {
//                _crossAxisOffset = value;
//                InvalidateArrange();
//            }
//        }
//    }

//    private IVirtualizingController Controller => ((IVirtualizingPanel)this).Controller;

//    public int ScrollUnit => _lineLengths.Count == 0 ? 1 : _averageCount / _lineLengths.Count;

//    void IVirtualizingPanel.ForceInvalidateMeasure()
//    {
//        InvalidateMeasure();
//        _forceRemeasure = true;
//    }

//    protected override Size MeasureOverride(Size availableSize)
//    {
//        if (availableSize.Width == double.PositiveInfinity)
//        {
//            availableSize = new Size(5, availableSize.Height);
//        }

//        if (_forceRemeasure || availableSize != ((ILayoutable)this).PreviousMeasure)
//        {
//            _forceRemeasure = false;
//            _availableSpace = availableSize;
//            Controller?.UpdateControls();
//        }

//        return base.MeasureOverride(availableSize);
//    }

//    protected override Size ArrangeOverride(Size finalSize)
//    {
//        _availableSpace = finalSize;
//        _canBeRemoved = 0;
//        _takenSpace = 0;
//        _takenLineSpace = 0;
//        _lineLengths.Clear();
//        _averageItemSize = 0;
//        _averageCount = 0;
//        var result = Arrange(finalSize);
//        _takenSpace += _pixelOffset;
//        Controller?.UpdateControls();
//        return result;
//    }

//    protected Size Arrange(Size finalSize)
//    {
//        double accumulatedV = 0;
//        var uvFinalSize = CreateUVSize(finalSize);
//        var lineSize = CreateUVSize();
//        int firstChildInLineindex = 0;
//        for (int index = 0; index < Children.Count; index++)
//        {
//            var child = Children[index];
//            var childSize = CreateUVSize(child.DesiredSize);
//            if (lineSize.U + childSize.U <= uvFinalSize.U) // same line
//            {
//                lineSize.U += childSize.U;
//                lineSize.V = Math.Max(lineSize.V, childSize.V);
//                _takenLineSpace += childSize.U;
//            }
//            else // moving to next line
//            {
//                var controlsInLine = GetContolsBetween(firstChildInLineindex, index);
//                ArrangeLine(accumulatedV, lineSize.V, controlsInLine);
//                accumulatedV += lineSize.V;
//                lineSize = childSize;
//                firstChildInLineindex = index;
//                _takenLineSpace = childSize.U;
//            }
//        }

//        if (firstChildInLineindex < Children.Count)
//        {
//            var controlsInLine = GetContolsBetween(firstChildInLineindex, Children.Count);
//            ArrangeLine(accumulatedV, lineSize.V, controlsInLine);
//        }
//        return finalSize;
//    }
//    private IEnumerable<IControl> GetContolsBetween(int first, int last)
//    {
//        return Children.Skip(first).Take(last - first);
//    }

//    private void ArrangeLine(double v, double lineV, IEnumerable<IControl> contols)
//    {
//        double u = 0;
//        bool isHorizontal = (this.Orientation == Orientation.Horizontal);
//        foreach (var child in contols)
//        {
//            var childSize = CreateUVSize(child.DesiredSize);
//            var x = isHorizontal ? u : v;
//            var y = isHorizontal ? v : u;
//            var width = isHorizontal ? childSize.U : lineV;
//            var height = isHorizontal ? lineV : childSize.U;

//            var rect = new Rect(
//               x - _crossAxisOffset,
//               y - _pixelOffset,
//               width,
//               height);
//            child.Arrange(rect);
//            u += childSize.U;
//            AddToAverageItemSize(childSize.V);

//            if (rect.Bottom >= _takenSpace)
//            {
//                _takenSpace = rect.Bottom;
//            }
//            if (rect.Y >= _availableSpace.Height)
//            {
//                ++_canBeRemoved;
//            }
//        }
//        _lineLengths.Add(u);
//    }
//    protected override void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
//    {
//        base.ChildrenChanged(sender, e);

//        switch (e.Action)
//        {
//            case NotifyCollectionChangedAction.Add:
//                foreach (IControl control in e.NewItems)
//                {
//                    UpdateAdd(control);
//                }
//                break;
//            case NotifyCollectionChangedAction.Remove:
//                foreach (IControl control in e.OldItems)
//                {
//                    UpdateRemove(control);
//                }
//                break;
//        }
//    }


//    private void UpdateAdd(IControl child)
//    {
//        var bounds = Bounds;
//        var gap = 0;

//        child.Measure(_availableSpace);
//        //     ++_averageCount;
//        var height = child.DesiredSize.Height;
//        var width = child.DesiredSize.Width;

//        if (Orientation == Orientation.Horizontal)
//        {
//            if (_takenLineSpace + width > _availableSpace.Width)
//            {
//                _takenSpace += height + gap;
//                _takenLineSpace = width;
//                _lineLengths.Add(width);
//            }
//            else
//            {
//                _takenLineSpace += width;
//                if (_lineLengths.Count == 0) _lineLengths.Add(0);
//                _lineLengths[_lineLengths.Count - 1] += width;
//            }
//            AddToAverageItemSize(height);
//        }
//        else
//        {
//            if (_takenLineSpace + height > _availableSpace.Height)
//            {
//                _takenSpace += width + gap;
//                _takenLineSpace = height;
//            }
//            else
//            {
//                _takenLineSpace += height;
//            }
//            AddToAverageItemSize(width);
//        }
//    }

//    private void UpdateRemove(IControl child)
//    {
//        var bounds = Bounds;
//        var gap = 0;

//        var height = child.DesiredSize.Height;
//        var width = child.DesiredSize.Width;

//        if (Orientation == Orientation.Horizontal)
//        {
//            if (_takenLineSpace - width <= 0)
//            {
//                _takenSpace -= height + gap;
//                _lineLengths.RemoveAt(_lineLengths.Count - 1);
//                _takenLineSpace = _lineLengths.Count > 0 ? _lineLengths.Last() : 0;
//            }
//            else
//            {
//                _takenLineSpace -= width;
//            }
//            AddToAverageItemSize(height);
//        }
//        else
//        {
//            if (_takenLineSpace - height <= 0)
//            {
//                _takenSpace -= width + gap;
//                _takenLineSpace = _availableSpace.Height;
//            }
//            else
//            {
//                _takenLineSpace -= height;
//            }
//            AddToAverageItemSize(width);
//        }

//        if (_canBeRemoved > 0)
//        {
//            --_canBeRemoved;
//        }
//    }

//    private void AddToAverageItemSize(double value)
//    {
//        ++_averageCount;
//        _averageItemSize += (value - _averageItemSize) / _averageCount;
//    }

//    private void RemoveFromAverageItemSize(double value)
//    {
//        _averageItemSize = ((_averageItemSize * _averageCount) - value) / (_averageCount - 1);
//        --_averageCount;
//    }

//    private UVSize CreateUVSize(Size size) => new UVSize(Orientation, size);

//    private UVSize CreateUVSize() => new UVSize(Orientation);

//    /// <summary>
//    /// Used to not not write sepearate code for horizontal and vertical orientation.
//    /// U is direction in line. (x if orientation is horizontal)
//    /// V is direction of lines. (y if orientation is horizonral)
//    /// </summary>
//    [DebuggerDisplay("U = {U} V = {V}")]
//    private struct UVSize
//    {
//        private readonly Orientation _orientation;

//        internal double U;

//        internal double V;

//        private UVSize(Orientation orientation, double width, double height)
//        {
//            U = V = 0d;
//            _orientation = orientation;
//            Width = width;
//            Height = height;
//        }

//        internal UVSize(Orientation orientation, Size size)
//            : this(orientation, size.Width, size.Height)
//        {
//        }

//        internal UVSize(Orientation orientation)
//        {
//            U = V = 0d;
//            _orientation = orientation;
//        }

//        private double Width
//        {
//            get { return (_orientation == Orientation.Horizontal ? U : V); }
//            set
//            {
//                if (_orientation == Orientation.Horizontal)
//                {
//                    U = value;
//                }
//                else
//                {
//                    V = value;
//                }
//            }
//        }

//        private double Height
//        {
//            get { return (_orientation == Orientation.Horizontal ? V : U); }
//            set
//            {
//                if (_orientation == Orientation.Horizontal)
//                {
//                    V = value;
//                }
//                else
//                {
//                    U = value;
//                }
//            }
//        }

//        public Size ToSize()
//        {
//            return new Size(Width, Height);
//        }
//    }
//}
