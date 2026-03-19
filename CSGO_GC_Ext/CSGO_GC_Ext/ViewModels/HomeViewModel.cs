using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSGO_GC_Ext.Models;
using CSGO_GC_Ext.Utils;
using CSGO_GC_Ext.Utils.Game;
using CSGO_GC_Ext.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CSGO_GC_Ext.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    public class CategoryItem(int id, string name) : IEquatable<CategoryItem>
    {
        public int Index { get; set; } = id;
        public string DisplayName { get; set; } = name;

        public bool Equals(CategoryItem? other)
        {
            if (other is null) return false;
            return Index == other.Index;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as CategoryItem);
        }

        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    [ObservableProperty]
    public partial IImmutableBrush Background { get; set; } = Design.IsDesignMode ? MainViewModel.AppBackgroundBrush : Brushes.Transparent;

    [ObservableProperty]
    public partial List<string> Bonjours { get; set; } = ["你好", "Hello", "Hola", "Bonjour", "こんにちは", "안녕하세요", "Ciao", "Guten Tag", "Olá", "Привет"];

    public List<string> BonjoursTripled => [.. Bonjours, .. Bonjours, .. Bonjours];
    partial void OnBonjoursChanged(List<string> value)
    {
        OnPropertyChanged(nameof(BonjoursTripled));
    }

    public readonly CSGOGameResources CSGOGameResources = new();
    private CSGOGameResources CSGOGameResourcesProvider() => CSGOGameResources;

    // Initialized by DispatchLoadInventory method, but it's no longer to be.
    [ObservableProperty]
    private partial IReadOnlyDictionary<object, SelectionItem>? AvailableItemDefs { get; set; } = null;

    // Initialized by DispatchLoadInventory method, but it's no longer to be.
    [ObservableProperty]
    private partial IReadOnlyDictionary<object, SelectionItem>? AvailablePaintKitDefs { get; set; } = null;


    // Inventory Category Views
    public IEnumerable<CategoryItem> Categories
    {
        get
        {
            _ = this; // CA1822
            var categories = new List<CategoryItem>();

            foreach (var o in Enum.GetValues<InventoryItem.ItemCategories.ItemCategory>())
            {
                categories.Add(new((int)o, InventoryItem.GetCategoryEnumDisplayName(o) ?? o.ToString()));
            }

            return categories;
        }
    }
    public IEnumerable<CategoryItem> SubMenuCategories
    {
        get
        {
            List<CategoryItem> categories = [];

            var t = InventoryItem.MenuCategoryType.GetMenuCategoryType((InventoryItem.ItemCategories.ItemCategory)SelectedCategory.Index);
            if (t is null)
                return categories;

            foreach (var o in Enum.GetValues(t))
            {
                if (o is not Enum e)
                    throw new();
                categories.Add(new((int)o, InventoryItem.GetCategoryEnumDisplayName(e) ?? e.ToString()));
            }

            return categories;
        }
    }

    // Inventory Items
    [ObservableProperty]
    public partial PausableObservableCollection<InventoryItem> Items { get; set; } = [];
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PagedItems), nameof(DisplayPageNumbers), nameof(FilteredItemsCount), nameof(ItemsCount))]
    /// FilteredItems: We post the most update events here.
    ///   WARN: FilteredItems should be used like IReadOnlyList!
    public partial List<InventoryItem> FilteredItems { get; set; } = [];
    public int FilteredItemsCount => FilteredItems.Count;
    public int ItemsCount => Items.Count;
    public int NumberAvaliablePages => (FilteredItems.Count + MaxItemsPerPage - 1) / MaxItemsPerPage;
    private const bool __always_1_least_page = true;
    public IEnumerable<int> DisplayPageNumbers => Enumerable.Range(1, Math.Max(__always_1_least_page ? 1 : 0, NumberAvaliablePages));
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PagedItems), nameof(DisplayPageNumbers))]
    public partial int MaxItemsPerPage { get; set; } = 12; // TODO: We really need a Virtualizing-WrapPanel!
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PagedItems))]
    public partial int CurrentPageIndex { get; set; } = 0;
    public List<InventoryItem>? PagedItems
    {
        get
        {
            //var _chks = FilteredItems.Chunk(MaxItemsPerPage);
            //return _chks.ElementAt(CurrentPageIndex = Math.Clamp(CurrentPageIndex, 0, _chks.Count()));

            if (CurrentPageIndex == -1) // This should only occors when modify CurrentPageIndex
                return null;

            if (FilteredItems.Count <= 0)
                return FilteredItems;

            int start = CurrentPageIndex * MaxItemsPerPage;
            return FilteredItems.Slice(start, Math.Min(MaxItemsPerPage, FilteredItems.Count - start));
        }
    }
    public ObservableCollection<InventoryItem> SelectedItems { get; } = [];

    // Inventory Search Text
    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    // Inventory Category
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShouldShowSubMenu))]
    [NotifyPropertyChangedFor(nameof(SubMenuCategories))]
    public partial CategoryItem SelectedCategory { get; set; }
    private CategoryItem GetDefualtSelectedCategory()
        => Categories.Single(x => x.Index == (int)InventoryItem.ItemCategories.ItemCategory.Everything);

    // Inventory Sub Category Menu
    [ObservableProperty]
    public partial CategoryItem SelectedSubMenuCategory { get; set; }
    private CategoryItem GetDefualtSelectedSubMenuCategory()
        => SubMenuCategories.Single(x => x.Index == (int)InventoryItem.ItemCategories.MenuCategoryAllIndex);

    #region
    //public Enum AccessSelectedSubMenuCategory(params Tuple<InventoryItem.ItemCategories.ItemCategory?, Action<Enum>>[] callbacks)
    //{
    //    var enumType = InventoryItem.MenuCategoryType.GetMenuCategoryType((InventoryItem.ItemCategories.ItemCategory)SelectedCategory.Index);
    //    if (enumType is null)
    //        throw new NotImplementedException();
    //
    //    Enum value = Enum.IsDefined(enumType, SelectedSubMenuCategory)
    //        ? (Enum)Enum.ToObject(enumType, SelectedSubMenuCategory)
    //        : (Enum)Activator.CreateInstance(enumType)!;
    //
    //    callbacks.SingleOrDefault(x => x.Item1 == (InventoryItem.ItemCategories.ItemCategory)SelectedCategory.Index)?.Item2(value);
    //    return value;
    //}
    #endregion

    // Item Edit
    private readonly Lock _currentEditItemLock = new();
    [ObservableProperty]
    public partial EditItemViewModel? CurrentEditItem { get; set; }


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CSGOGameTranslationFile))]
    public partial string CSGOGameTranslation { get; set; } = "schinese";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CSGOGameItemsFile))]
    [NotifyPropertyChangedFor(nameof(CSGOGameItemsCDNFile))]
    [NotifyPropertyChangedFor(nameof(CSGOGameResourceDir)),
        NotifyPropertyChangedFor(nameof(CSGOGameTranslationFile))]
    [NotifyPropertyChangedFor(nameof(CSGOGCDir)),
        NotifyPropertyChangedFor(nameof(CSGOGCInventoryDataFile))]
    public partial string CSGOGameDir { get; set; } = "G:/SteamLibrary/steamapps/common/Counter-Strike Global Offensive"; // "C:/Program Files (x86)/Steam/SteamLibrary/steamapps/common/Counter-Strike Global Offensive";
    public string CSGOGameItemsFile => Path.Combine(CSGOGameDir, "csgo", "scripts", "items", "items_game.txt");
    public string CSGOGameItemsCDNFile => Path.Combine(CSGOGameDir, "csgo", "scripts", "items", "items_game_cdn.txt");
    public string CSGOGameResourceDir => Path.Combine(CSGOGameDir, "csgo", "resource");
    public string CSGOGameTranslationFile => Path.Combine(CSGOGameResourceDir, $"csgo_{CSGOGameTranslation}.txt");
    public string CSGOGCDir => Path.Combine(CSGOGameDir, "csgo_gc");
    public string CSGOGCInventoryDataFile => Path.Combine(CSGOGCDir, "inventory.txt");

    [ObservableProperty]
    public partial bool IsMessageVisible { get; set; }
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;
    private CancellationTokenSource? __setStatusMessageLast = null;
    public void SetStatusMessage(string newValue, int dismissDelay = 2500, bool flash = true)
    {
        if (flash)
            IsMessageVisible = false;

        StatusMessage = string.Empty;
        StatusMessage = newValue;

        IsMessageVisible = true;

        __setStatusMessageLast?.Cancel();
        var cts = new CancellationTokenSource();
        __setStatusMessageLast = cts;
        HideStatusMessageAfterDelay(dismissDelay, false, cts.Token);
    }

    public bool ShouldShowSubMenu => SelectedCategory.Index != (int)InventoryItem.ItemCategories.ItemCategory.Everything;
    [ObservableProperty]
    public partial bool ShouldShowItemsOperation { get; set; } = false;


    public readonly Task LoadingResources; // TODO: Non-Readonly
    public bool CanLoadInventory => LoadingResources.IsCompleted; // TODO: Use IsCompletedSuccessfully

    public HomeViewModel()
    {
        SelectedCategory = GetDefualtSelectedCategory();
        SelectedSubMenuCategory = GetDefualtSelectedSubMenuCategory();

        PropertyChanging += OnPropertyChanging;
        PropertyChanged += OnPropertyChanged;

        SelectedItems.CollectionChanged += OnSelectedItemsChanged;

        // Load Resources.
        LoadingResources = Task.WhenAll(
            UpdateGameItemsAsync(),
            UpdateCSGOTranslaitonsAsync(),
            UpdateGameItemsCDNAsync()
        );
    }

    private void OnSelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ShouldShowItemsOperation = SelectedItems.Count > 0;
    }

    private void OnPropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (e.PropertyName == nameof(Items) && Items is not null)
            Items.CollectionChanged -= OnItemsChanged;
    }

    private async void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchText))
        {
            var dispatch = await DispatchUpdate(delayBegin: 250, runValidation: default);
            if (dispatch.IsCompletedSuccessfully)
                SetStatusMessage(
                    $"已找到 {FilteredItems.Count} 个物品",
                    dismissDelay: 1100
                );
        }

        if (
            (e.PropertyName == nameof(SelectedCategory) && SelectedCategory is not null) ||
            (e.PropertyName == nameof(SelectedSubMenuCategory) && SelectedSubMenuCategory is not null))
        {
            var dispatch = await DispatchUpdate(delayBegin: 0, runValidation: default);
            if (dispatch.IsCompletedSuccessfully)
                SetStatusMessage(
                    $"发现了 {FilteredItems.Count} 个 " +
                    ((SelectedCategory is not null &&
                      SelectedCategory.Index != (int)InventoryItem.ItemCategories.ItemCategory.Everything &&
                      SelectedSubMenuCategory.Index != InventoryItem.ItemCategories.MenuCategoryAllIndex) ?
                        SelectedSubMenuCategory.DisplayName : "物品"),
                    dismissDelay: 1100
                );

            return;
        }

        if (e.PropertyName == nameof(DisplayPageNumbers))
        {
            ValidatePageIndex();
            return;
        }

        if (e.PropertyName == nameof(Items))
        {
            Items.CollectionChanged += OnItemsChanged;
            OnItemsChanged();

            return;
        }

        if (e.PropertyName == nameof(SubMenuCategories))
        {
            SelectedSubMenuCategory = GetDefualtSelectedSubMenuCategory();

            return;
        }
    }

    private async void OnItemsChanged(object? sender = null, NotifyCollectionChangedEventArgs? e = null)
    {
        var dispatch = await DispatchUpdate(delayBegin: 0, runValidation: default);
        if (dispatch.IsCompletedSuccessfully)
            SetStatusMessage($"已加载 {Items.Count} 个物品", dismissDelay: 2500);
    }

    private Task HideStatusMessageAfterDelay(int delay, bool setAfterCancel = false, CancellationToken cancellationToken = default)
    {
        return Task.Run(async () =>
        {
            try { await Task.Delay(delay, cancellationToken); }
            catch (TaskCanceledException)
            {
                if (!setAfterCancel)
                    return;
            }
            IsMessageVisible = false;
        }, cancellationToken);
    }
    //private async Task HideStatusMessageAfterDelay(int delay) => await HideStatusMessageAfterDelay(delay, setAfterCancel: default, orignialCancellationToken: default);

    private async Task BeginEdit(InventoryItem item, int tryOperationTimeout = 0)
    {
        if (!_currentEditItemLock.TryEnter(tryOperationTimeout))
            return;

        EditItemViewModel editing =
            new(item,
                AvailableItemDefs ?? throw new($"{nameof(AvailableItemDefs)} is null!"),
                AvailablePaintKitDefs ?? throw new($"{nameof(AvailablePaintKitDefs)} is null!"));
        CurrentEditItem = editing;

        var progress = editing.Editing;
        var operation = await progress;
        if (!progress.IsCompletedSuccessfully)
            return;

        if (operation is EditItemViewModel.Operation.Save)
        {
            var edit = editing.EditItem;

            var existingItem = Items.FirstOrDefault(i => i.Id == edit.Id);
            if (existingItem != null)
            {
                // Replace existing
                var pauseToken = Items.PauseNotifications();
                try
                {
                    Items.Remove(existingItem);
                    Items.Add(edit);
                }
                finally { pauseToken.Dispose(); }
                await DispatchUpdate(0, runValidation: true);
                SetStatusMessage($"已保存 {existingItem.FullDisplayName}");
            }
            else
            {
                // Add new
                Items.Add(edit);
                SetStatusMessage($"已添加 {edit.FullDisplayName}");
            }
        }
        else if (operation is EditItemViewModel.Operation.Cancel)
        {
            SetStatusMessage("编辑已取消");
        }

        CurrentEditItem = null;
        _currentEditItemLock.Exit();
    }

    private void ValidatePageIndex()
    {
        CurrentPageIndex = Math.Clamp(CurrentPageIndex, 0, NumberAvaliablePages - 1);
    }

    public Task UpdateGameItemsAsync()
    {
        return Task.Run(() =>
        {
            var benchmark = Stopwatch.StartNew();
            CSGOGameResources.GameItems = CSGOTxtHelper.CSGOJsonLikeTxtHelper.Resolve(CSGOGameItemsFile);
            benchmark.Stop();
            Debug.WriteLine($"Loaded All Game Items in {benchmark} ms");
        });
    }
    public Task UpdateCSGOTranslaitonsAsync()
    {
        return Task.Run(() =>
        {
            var benchmark = Stopwatch.StartNew();
            CSGOGameResources.CSGOTranslations = CSGOTxtHelper.CSGOJsonLikeTxtHelper.Resolve(CSGOGameTranslationFile);
            benchmark.Stop();
            Debug.WriteLine($"Loaded CSGO Translation in {benchmark} ms");
        });
    }
    public Task UpdateGameItemsCDNAsync()
    {
        return Task.Run(() =>
        {
            var benchmark = Stopwatch.StartNew();
            CSGOGameResources.GameItemsCDN = CSGOTxtHelper.CSGOItemsGameCdnTxtHelper.Resolve(CSGOGameItemsCDNFile);
            benchmark.Stop();
            Debug.WriteLine($"Loaded All Game Items CDN in {benchmark} ms");
        });
    }

    private IReadOnlyDictionary<object, SelectionItem>? LoadItemDefs()
    {
        var items = CSGOGameResourcesProvider().GameItems?.TryGetSelfRecursiveValue("items_game")?.TryGetSelfRecursiveValue("items");
        if (items is null)
            return null;

        Dictionary<object, SelectionItem>? _result = null;
        foreach (var ikey in items.Keys)
        {
            var item = new InventoryItem(CSGOGameResourcesProvider) { DefIndex = ikey };

            _result ??= [];
            _result.Add(ikey, new SelectionItem(data: ikey, displayText: item.DisplayName));
        }
        return _result;
    }

    private IReadOnlyDictionary<object, SelectionItem>? LoadPaintKitDefs()
    {
        var attrs = CSGOGameResourcesProvider().GameItems?.TryGetSelfRecursiveValue("items_game")?.TryGetSelfRecursiveValue("attributes");
        if (attrs == null)
            return null;

        var paint_kits = CSGOGameResourcesProvider().GameItems?.TryGetSelfRecursiveValue("items_game")?.TryGetSelfRecursiveValue("paint_kits");
        if (paint_kits is null)
            return null;

        var paint_kit_attr_id = attrs.Keys.FirstOrDefault(i => attrs.TryGetSelfRecursiveValue(i)?.TryGetStringValue("attribute_class") == "set_item_texture_prefab");
        if (paint_kit_attr_id is null)
            return null;

        Dictionary<object, SelectionItem>? _result = null;
        foreach (var ikey in paint_kits.Keys)
        {
            var item = new InventoryItem(CSGOGameResourcesProvider) { Attributes = new() { { paint_kit_attr_id, ikey } } };

            _result ??= [];
            _result.Add(ikey, new SelectionItem(data: ikey, displayText: item.DisplaySkinName));
        }
        return _result;
    }

    private DateTime? __lastInventoryFileWriteDate = null;
    public Task<IReadOnlyList<InventoryItem>> LoadInventoryAsync()
    {
        return Task.Run<IReadOnlyList<InventoryItem>>(() =>
        {
            var benchmark = Stopwatch.StartNew();

            if (CSGOGameResources.GameItems is null)
                throw new InvalidOperationException("CSGOGameResources.GameItems is null");

            __lastInventoryFileWriteDate = File.GetLastWriteTime(CSGOGCInventoryDataFile);
            var __inventory_data = CSGOTxtHelper.CSGOJsonLikeTxtHelper.Resolve(CSGOGCInventoryDataFile);
            if (__inventory_data["items"] is not Dictionary<string, object> _inventory_items)
                throw new($"Invalid CSGO GC Inventory Data File");

            List<InventoryItem> result = [];
            foreach (var ikey in _inventory_items.Keys)
            {
                var _item_raw = _inventory_items.TryGetSelfRecursiveValue(ikey);
                if (_item_raw is null)
                {
                    continue; // This may be other kind of item which is not a scope, Skip it.
                }

                int? _id = null; if (int.TryParse(ikey, out var _1)) _id = _1;

                //if (InventoryItem.FromCSGOTxt(_id, _item_raw, CSGOGameResourcesProvider) is not InventoryItem _item)
                //    throw new();
                //var a = _item.ToCSGOTxt().First();
                //if (InventoryItem.FromCSGOTxt(int.Parse(a.Key), (Dictionary<string, object>)a.Value, CSGOGameResourcesProvider) is not InventoryItem _item_2)
                //    throw new();
                //result.Add(_item_2);

                if (InventoryItem.FromCSGOTxt(_id, _item_raw, CSGOGameResourcesProvider) is InventoryItem _item)
                    result.Add(_item);
            }

            benchmark.Stop();
            Debug.WriteLine($"Resolved Inventory from raw in {benchmark} ms");

            Items = new(result);
            return result;
        });
    }
    private Stopwatch? __swSaveInventoryRepeat = null;
    public Task SaveInventoryAsync()
    {
        // Check if the inventory file has been modified externally 
        if (__lastInventoryFileWriteDate is not null && __lastInventoryFileWriteDate != File.GetLastWriteTime(CSGOGCInventoryDataFile))
        {
            if (__swSaveInventoryRepeat is null || __swSaveInventoryRepeat.Elapsed < TimeSpan.FromSeconds(1.5))
            {
                SetStatusMessage("当前库存文件已在外部被修改，请重复以确认覆盖当前库存文件。要放弃修改并读取最新库存，请：使用 重置 按钮", 1300);
                __swSaveInventoryRepeat = Stopwatch.StartNew();
                return Task.CompletedTask;
            }
        }
        __swSaveInventoryRepeat?.Stop();
        __swSaveInventoryRepeat = null;
        __lastInventoryFileWriteDate = File.GetLastWriteTime(CSGOGCInventoryDataFile);

        var items = new List<InventoryItem>(Items);
        return Task.Run(() =>
        {
            var inventory_data = CSGOTxtHelper.CSGOJsonLikeTxtHelper.Resolve(CSGOGCInventoryDataFile);
            if (inventory_data["items"] is not Dictionary<string, object> _inventory_items)
                throw new($"Invalid CSGO GC Inventory Data File");

            var benchmark = Stopwatch.StartNew();

            _inventory_items.Clear();
            foreach (var itemTxt in items.Select(i => i.ToCSGOTxt()))
            {
                var item = itemTxt.First();
                _inventory_items.Add(item.Key, item.Value);
            }

            CSGOTxtHelper.CSGOJsonLikeTxtHelper.Save(inventory_data, CSGOGCInventoryDataFile);

            benchmark.Stop();
            Debug.WriteLine($"Resolve Inventory to raw in {benchmark} ms");

            return;
        });
    }

    private CancellationTokenSource? __tryUpdateLast = null;
    public async Task<Task> DispatchUpdate(int delayBegin = 150, bool runValidation = false)
    {
        /// Validate Ids for each InventoryItem
        Task __func_validate_async(CancellationToken cancellationToken = default)
        {
            if (Items.Count == 0)
                return Task.CompletedTask;

            return Task.Run(() =>
            {
                Stopwatch benchmark = Stopwatch.StartNew();

                const int ID_START = 1; // TODO: 我记不清 csgo_gc 的inventory.txt第一个物品 id 到底是0还是1了
                HashSet<int> _ids = [];
                List<InventoryItem> invalids = [];

                foreach (var item in Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (item.Id is null or < ID_START)
                    {
                        invalids.Add(item);
                    }
                    else if (_ids.Contains(item.Id.Value))
                    {
                        invalids.Add(item);
                    }
                    else
                    {
                        _ids.Add(item.Id.Value);
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();

                int i = ID_START;
                foreach (var item in invalids)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    while (_ids.Contains(i))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        i++;
                    }
                    cancellationToken.ThrowIfCancellationRequested();

                    item.Id = i;
                    _ids.Add(i);
                }
                cancellationToken.ThrowIfCancellationRequested();

                Debug.WriteLine($"Validate Items for {benchmark} ms");
            }, cancellationToken);
        }
        Task __func_apply_filters_async(CancellationToken cancellationToken = default)
        {
            if (Items.Count == 0)
                return Task.CompletedTask;

            return Task.Run(() =>
            {
                var _searched = InventoryItemSearcher.Search(userRawInput: SearchText, items: Items);
                cancellationToken.ThrowIfCancellationRequested();

                List<InventoryItem> _filtered = [];
                foreach (var item in _searched)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Not Categoried
                    if (SelectedCategory?.Index == (int)InventoryItem.ItemCategories.ItemCategory.Everything)
                        goto @true;

                    //if (SelectedSubMenuCategory is null)
                    //    goto @false; // TODO: This should happens in changes of the main catrgory selection only. Otherwise, exceptions should be thrown.

                    // Category Matching
                    if (item.Category == SelectedSubMenuCategory?.Index ||
                        SubMenuCategories.SingleOrDefault(x => x.Index == item.Category) is not null && SelectedSubMenuCategory?.Index == InventoryItem.ItemCategories.MenuCategoryAllIndex)
                        goto @true;

                    //@false:
                    continue;
                @true:
                    _filtered.Add(item);
                    continue;
                }
                cancellationToken.ThrowIfCancellationRequested();

                var _result = _filtered.OrderByDescending(x => x.Id).ToList(); // Like CS:GO
                cancellationToken.ThrowIfCancellationRequested();

                FilteredItems = _result;
            }, cancellationToken);
        }

        __tryUpdateLast?.Cancel();

        var cts = new CancellationTokenSource();
        __tryUpdateLast = cts;

        try
        {
            await Task.Delay(delayBegin, cts.Token);
            if (runValidation)
                await __func_validate_async(cts.Token);
            await __func_apply_filters_async(cts.Token);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            return Task.FromCanceled(cts.Token);
        }
        return Task.CompletedTask;
    }


    public async Task DispatchLoadInventory()
    {
        if (!CanLoadInventory)
            await LoadingResources;

        AvailableItemDefs ??= LoadItemDefs();
        AvailablePaintKitDefs ??= LoadPaintKitDefs();

        _ = await LoadInventoryAsync();
    }

    // Delete Item
    [RelayCommand]
    private void DeleteItem(InventoryItem item)
    {
        if (Items.Remove(item))
        {
            SetStatusMessage($"已删除 {item.FullDisplayName}");
        }
    }

    // Add New Item
    [RelayCommand]
    private async Task AddNewItem()
    {
        await BeginEdit(new(CSGOGameResourcesProvider), tryOperationTimeout: 0);
    }

    // Edit Item
    [RelayCommand]
    private async Task EditItem(InventoryItem item)
    {
        await BeginEdit(item.Clone(_: null), tryOperationTimeout: 0);
    }

    // Save all changes
    [RelayCommand]
    private async Task SaveChanges()
    {
        await SaveInventoryAsync();
        await DispatchLoadInventory();
        SetStatusMessage("所有更改已保存");
    }

    // Discard all changes
    [RelayCommand]
    private async Task ResetInventory()
    {
        //Items = [];
        await DispatchLoadInventory();
        SetStatusMessage("库存已重置");
    }

    // Share current inventory
    [RelayCommand]
    private void ShareInventory()
    {
        throw new NotImplementedException();
    }

    // Add items from sharing
    [RelayCommand]
    private void FromSharing()
    {
        throw new NotImplementedException();
    }

    // Remove items in the selection.
    [RelayCommand]
    private void DeleteSelectedItems()
    {
        var _targets = SelectedItems.ToList();
        SelectedItems.Clear();

        var _pausing = Items.PauseNotifications();
        try
        {
            foreach (var item in _targets)
                Items.Remove(item);
        }
        finally { _pausing.Dispose(); }
        _ = DispatchUpdate();
    }

    // Clone items in the selection.
    [RelayCommand]
    private void CloneSelectedItems()
    {
        var _targets = SelectedItems.ToList();
        SelectedItems.Clear();

        var _pausing = Items.PauseNotifications();
        try
        {
            foreach (var item in _targets)
                Items.Add(item.Clone(_: default));
        }
        finally { _pausing.Dispose(); }
        _ = DispatchUpdate();
    }

    // Add sample inventory items
    [RelayCommand]
    private void AddSampleInventoryItems()
    {
        if (CSGOGameResources.GameItemsCDN is null)
            return;

        List<InventoryItem> _structor = Items is not null ? [.. Items] : [];
        foreach (var item in CSGOGameResources.GameItemsCDN)
        {
            if (item.Key.StartsWith('#'))
                Debugger.Break();

            _structor.Add(new(CSGOGameResourcesProvider)
            {
                DisplaySkinName = item.Key,
                DisplayImageToken = item.Key
            });
        }

        const bool __LOAD_LGF_SAMPLES = true;
        if (__LOAD_LGF_SAMPLES)
        {
            _structor =
            [
                .. _structor,

                new(CSGOGameResourcesProvider)
                {
                    DisplayImageToken = "sporty_gloves_sporty_green",
                    DisplayName = "运动手套",
                    DisplaySkinName = "树篱迷宫",
                },
                new(CSGOGameResourcesProvider)
                {
                    DisplayImageToken = "sporty_gloves_sporty_poison_frog_blue_white",
                    DisplayName = "运动手套",
                    DisplaySkinName = "双栖",
                },
                new(CSGOGameResourcesProvider)
                {
                    DisplayImageToken = "sporty_gloves_sporty_blue_pink",
                    DisplayName = "运动手套",
                    DisplaySkinName = "迈阿密风云",
                },
                new(CSGOGameResourcesProvider)
                {
                    DisplayImageToken = "specialist_gloves_specialist_marble_fade",
                    DisplayName = "专业手套",
                    DisplaySkinName = "渐变大理石",
                },
            ];
        }

        Items = new(_structor);
    }
}

