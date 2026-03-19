using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Reactive;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LibLGFs.AvaVisualHelper;
using CSGO_GC_Ext.ViewModels;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CSGO_GC_Ext.Views;

public partial class HomeView : UserControl, ViewHelper.INavigationAnimatable
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(MainView));
    private new HomeViewModel DataContext
    {
        get => (HomeViewModel)(base.DataContext ?? throw new("DataContext is null."));
        set => base.DataContext = value;
    }

    private readonly ViewHelper.SlideAnimation _pageSlideAnimation = new();
    private readonly ViewHelper.SlideAnimation _listBoxSlideAnimation = new();
    private readonly ViewHelper.SlideAnimation _editSlideAnimation = new();

    public HomeView()
    {
        DataContext = new();
        DataContext.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(DataContext.CurrentEditItem))
            {
                var _current_edit = DataContext.CurrentEditItem;
                var _show_edit = _current_edit is not null;
                //Debug.WriteLine($"{DateTime.Now}: {_show_edit}");

                InventoryList.IsHitTestVisible = !_show_edit;
                ItemEditorPresenter.IsHitTestVisible = _show_edit;

                EditItemView? _edit = null;
                if (_show_edit)
                {
                    _edit = new EditItemView(_current_edit!);
                    ItemEditorPresenter.Content = _edit;
                }

                var _inventory_sliding = _listBoxSlideAnimation.Fire(
                    act: !_show_edit ? ViewHelper.SlideAnimation.ActType.In : ViewHelper.SlideAnimation.ActType.Out,
                    reset: false,
                    discard: false // Keep
                );
                var _edit_sliding = _editSlideAnimation.Fire(
                    act: _show_edit ? ViewHelper.SlideAnimation.ActType.In : ViewHelper.SlideAnimation.ActType.Out,
                    reset: false,
                    discard: false // Keep
                );

                // _ = Task.Run(async () =>
                // {
                //     foreach (var animating in _inventory_sliding)
                //         await animating.Item1;
                //     if (_show_edit)
                //     {
                //
                //     }
                // });

                _ = Task.Run(async () =>
                {
                    foreach (var animating in _edit_sliding)
                        await animating.Item1;
                    if (!_show_edit)
                    {
                        // Clear references and views
                        if (_edit is not null && ItemEditorPresenter.Content is not null)
                        {
                            if (object.ReferenceEquals(ItemEditorPresenter.Content, _edit))
                                ItemEditorPresenter.Content = null;
                        }
                    }
                });

                return;
            }
        };

        InitializeComponent();

        _listBoxSlideAnimation.Load(InventoryList);
        _editSlideAnimation.Load(ItemEditorPresenter);
        this.Loaded += (_, _) =>
        {
            DispatchBonjourTitle(run: true);
        };
    }

    public HomeView(Visual testParent) : this()
    {
        this.Arrange(testParent.Bounds);
    }

    ~HomeView()
    {
        DispatchBonjourTitle(false);
    }

    private void LoadSlideInAnimation()
    {
        _pageSlideAnimation.Clear();
        _pageSlideAnimation.Load(this.FindLogiclChildren<Visual>(5, noRecursionVisuals: [typeof(Button)]).Where(v => !object.ReferenceEquals(v, InventoryList)));
    }

    private void OnTestButtonClick(object? sender, RoutedEventArgs e)
    {
        //_ = DataContext.DispatchUpdate();
    }


    private Task RunBonjorTitle(CancellationToken cancellationToken = default)
    {
        return Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!Dispatcher.UIThread.Invoke(() => BonjourRunner.IsVisible))
                    goto try_later;

                var scrollViewer = BonjourRunner.GetVisualDescendants()
                                                .OfType<ScrollViewer>()
                                                .FirstOrDefault() ?? throw new($"ScrollViewer is not existing of {nameof(BonjourRunner)}");

                var stackPanel = BonjourRunner.FindDescendantOfType<StackPanel>() ?? throw new($"Cannot find the ItemsPanel of type StackPanel.");

                double horizonSpacing = 0d;

                if (stackPanel.Children.Count <= 0)
                    goto try_later;

                await Dispatcher.UIThread.Invoke(async () =>
                {
                    var first = (stackPanel.Children[0] as ListBoxItem ?? throw new($"Excepted for a ListBoxItem ({stackPanel.Children[0].GetType()})"));
                    var firstClones = stackPanel.Children.Where(item => (item as ListBoxItem ?? throw new($"Excepted for a ListBoxItem ({item.GetType()})")).Content == first.Content).ToImmutableArray();

                    double loopStart = -1d;

                    // Measure the loop start point
                    {
                        foreach (var child in stackPanel.Children)
                        {
                            if (object.ReferenceEquals(child, firstClones[1]))
                                break;

                            loopStart += child.Margin.Left + child.Bounds.Width + child.Margin.Right + stackPanel.Spacing;
                        }
                    }

                    for (int i = 0; i < stackPanel.Children.Count; i++)
                    {
                        scrollViewer.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;
                        cancellationToken.ThrowIfCancellationRequested();

                        var child = stackPanel.Children[i];
                        var childNext = stackPanel.Children.ElementAtOrDefault(i + 1);

                        if (firstClones.Length < 3)
                            throw new($"The number of duplicate elements must be >= 3!");

                        // Reached the loop tail
                        if (object.ReferenceEquals(child, firstClones[2]))
                        {
                            scrollViewer.Offset = new Vector(loopStart, 0);
                            i = stackPanel.Children.IndexOf(firstClones[1]) - 1;
                            continue;
                        }

                        //var viewLength = stackPanel.Bounds.Width;
                        //var scrollSpeedPixelsPerSecond = BonjourRunner.FontSize * 8;
                        //var animDuration = TimeSpan.FromSeconds(viewLength / scrollSpeedPixelsPerSecond);

                        List<Task> _tasks = [];

                        var scrollingAnimation = new Animation
                        {
                            Duration = TimeSpan.FromSeconds(0.600),
                            Easing = new CircularEaseInOut(),
                            FillMode = FillMode.Forward
                        };
                        //scrollingAnimation.Children.Add(new KeyFrame
                        //{
                        //    Cue = new(0),
                        //    Setters =
                        //    {
                        //        new Setter(ScrollViewer.OffsetProperty, new Vector(0, 0))
                        //    }
                        //});
                        scrollingAnimation.Children.Add(new KeyFrame
                        {
                            Cue = new(1),
                            Setters =
                                {
                                    new Setter(ScrollViewer.OffsetProperty, new Vector(scrollViewer.Offset.X + child.Bounds.Width + horizonSpacing, scrollViewer.Offset.Y))
                                }
                        });
                        horizonSpacing = 0;

                        if (childNext != null)
                        {
                            var extendSize = BonjourRunner.FontSize * 8;
                            var targetSize = (childNext.Bounds.Width + childNext.Margin.Left + childNext.Margin.Right) + extendSize;
                            var sizingAniamtion = new Animation
                            {
                                Duration = scrollingAnimation.Duration,
                                Easing = scrollingAnimation.Easing,
                                FillMode = FillMode.Forward
                            };
                            sizingAniamtion.Children.Add(new KeyFrame
                            {
                                Cue = new(1),
                                Setters =
                                    {
                                        new Setter(StackPanel.WidthProperty, targetSize)
                                    }
                            });

                            // Extend offset fix
                            var __offset = extendSize / 2;
                            var _target = (Setter)scrollingAnimation.Children[0].Setters[0];
                            var _target_v = (_target.Value as Vector?)!.Value;
                            _target.Value = new Vector(_target_v.X - __offset, _target_v.Y);
                            horizonSpacing += __offset;

                            // Play sizingAniamtion
                            _tasks.Add(sizingAniamtion.RunAsync(BonjourRunner, cancellationToken));
                        }
                        // Play scrollingAnimation
                        _tasks.Add(scrollingAnimation.RunAsync(scrollViewer, cancellationToken));

                        horizonSpacing += stackPanel.Spacing + child.Margin.Left + child.Margin.Right;
                        await Task.WhenAll(_tasks);
                        await Task.Delay(250);
                    }
                }, DispatcherPriority.Normal, cancellationToken);

                continue;

            try_later:
                await Task.Delay(1000);
                continue;
            }
        }, cancellationToken);
    }
    private CancellationTokenSource? __lastBonjourRunnerCancellationSource = null;
    private void DispatchBonjourTitle(bool run = true)
    {
        __lastBonjourRunnerCancellationSource?.Cancel();
        __lastBonjourRunnerCancellationSource = null;

        if (run)
        {
            __lastBonjourRunnerCancellationSource = new();
            _ = RunBonjorTitle(__lastBonjourRunnerCancellationSource.Token);
        }
    }

    private void ItemCard_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control control)
            return;

        if (control.FindLogiclChildren<Button>().Where(v => v.Name is "__PART_EDIT_BUTTON").FirstOrDefault() is not Button buttonEdit)
            return;
        if (buttonEdit.Command is not ICommand commandEdit)
            return;
        commandEdit.Execute(control.DataContext);
    }

    private void ListBox_Initialized(object? sender, EventArgs e)
    {
        if (sender is not ListBox listbox)
            throw new ArgumentException(null, nameof(sender));

        listbox.LayoutUpdated += ViewHelper.ListBox_LayoutUpdated;
        listbox.Tapped += ViewHelper.ListBox_Tapped;
    }

    public bool OnNavigating(object? obj = null)
    {
        LoadSlideInAnimation();

        return true;
    }

    bool _first_inited = false;
    public async void OnNavigated(object? _ = null)
    {
        _pageSlideAnimation.Fire(ViewHelper.SlideAnimation.ActType.In, reset: true);

        if (!_first_inited)
        {
            try
            {
                foreach (var item in _pageSlideAnimation.ForEachPlaying(x => x.Item1))
                    if (item is not null) // We are working with out lock, it's not a thread safe operation.
                        await item;
            }
            catch (TaskCanceledException) { }

            _ = DataContext.DispatchLoadInventory(); // ���ǽ��״��Զ����ؿ����������Ϊ���ڽ�����Ӧ�õ�δʵ�������WrapPanel�ᵼ��Ӧ�ó�����淢������
            _first_inited = true;
        }
    }
}