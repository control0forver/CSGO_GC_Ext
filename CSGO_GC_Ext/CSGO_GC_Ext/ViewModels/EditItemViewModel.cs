using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSGO_GC_Ext.Models;
using CSGO_GC_Ext.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CSGO_GC_Ext.ViewModels;

public class SelectionItem(object? data, string displayText, bool isSeparator = false) : IInventoryItemSearcherSearchable
{
    public object? Data { get; private set; } = data;

    public bool IsSeparator { get; private set; } = isSeparator;
    public string DisplayText { get; private set; } = displayText;

    public IEnumerable<string> SearchTokens => Data is string _1 ? [_1, DisplayText] : [DisplayText];
}

public partial class EditItemViewModel : ViewModelBase
{
    public enum Operation
    {
        Cancel = 0,
        Save
    }

    [ObservableProperty]
    public partial IImmutableBrush Background { get; set; } = Design.IsDesignMode ? MainViewModel.AppBackgroundBrush : Brushes.Transparent;

    private readonly InventoryItem _edit;
    private readonly TaskCompletionSource<Operation> _completionSource = new();

    public InventoryItem EditItem => _edit;
    public Task<Operation> Editing => _completionSource.Task;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedItemDefDisplayName))]
    public partial KeyValuePair<object, SelectionItem>? SelectedItemDef { get; set; } = null;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedPaintKitDefDisplayName))]
    public partial KeyValuePair<object, SelectionItem>? SelectedPaintKitDef { get; set; } = null;

    private bool __SelectedItemDefChanged = false;
    public string? SelectedItemDefDisplayName
    {
        get
        {
            return field;
        }
        set
        {
            // TODO: Thread safety
            if (!__SelectedItemDefChanged)
            {
                foreach (var item in _refItemDefs)
                {
                    if (item.Value?.DisplayText == value)
                    {
                        SelectedItemDef = item;
                        break;
                    }
                }
            }
            else
            {
                __SelectedItemDefChanged = false;
            }

            field = value;
        }
    }
    private bool __SelectedPaintKitDefChanged = false;
    public string? SelectedPaintKitDefDisplayName
    {
        get
        {
            return field;
        }
        set
        {
            // TODO: Thread safety
            if (!__SelectedPaintKitDefChanged)
            {
                foreach (var item in _refPaintKitDefs)
                {
                    if (item.Value?.DisplayText == value)
                    {
                        SelectedPaintKitDef = item;
                        break;
                    }
                }
            }
            else
            {
                __SelectedPaintKitDefChanged = false;
            }

            field = value;
        }
    }
    public Func<string?, CancellationToken, Task<IEnumerable<Object>>> AvailableItemDefsFilter { get; }
    public Func<string?, CancellationToken, Task<IEnumerable<Object>>> AvailablePaintKitDefsFilter { get; }

    private readonly IReadOnlyDictionary<object, SelectionItem> _refItemDefs;
    public IReadOnlyDictionary<object, SelectionItem> AvailableItemDefs => _refItemDefs;
    private readonly IReadOnlyDictionary<object, SelectionItem> _refPaintKitDefs;
    public IReadOnlyDictionary<object, SelectionItem> AvailablePaintKitDefs => _refPaintKitDefs;

    public EditItemViewModel(InventoryItem edit, IReadOnlyDictionary<object, SelectionItem> refItemDefs, IReadOnlyDictionary<object, SelectionItem> refPaintKitDefs)
    {
        this.AppBarStyle ??= new();
        this.AppBarStyle.TitleContent = "About";
        this.AppBarStyle.TitleTagContent = "";

        _refItemDefs = refItemDefs;
        _refPaintKitDefs = refPaintKitDefs;
        _edit = edit;

        AvailableItemDefsFilter = Task<IEnumerable<object>> (query, token) =>
        {
            return Task.Run(() => InventoryItemSearcher.Search(query, threshold: 70, AvailableItemDefs.Values).Select(x => x.DisplayText).Cast<object>(), token);
        };
        AvailablePaintKitDefsFilter = Task<IEnumerable<object>> (query, token) =>
        {
            return Task.Run(() => InventoryItemSearcher.Search(query, threshold: 35, AvailablePaintKitDefs.Values).Select(x => x.DisplayText).Cast<object>(), token);
        };

        SelectedItemDef = _refItemDefs.FirstOrDefault(kv => kv.Key.ToString() == edit.DefIndex);
        if (edit.Attributes?.TryGetValue("6", out var _tmp_paint_kit_token) ?? false)
        {
            string _parsed_token = _tmp_paint_kit_token;
            if (double.TryParse(_tmp_paint_kit_token, out var __tmp_parsing))
                _parsed_token = ((int)__tmp_parsing).ToString();

            if (_refPaintKitDefs.TryGetValue(_parsed_token, out var _tmp))
                SelectedPaintKitDef = new(_parsed_token, _tmp);
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SelectedItemDef))
        {
            __SelectedItemDefChanged = true;
            SelectedItemDefDisplayName = SelectedItemDef?.Value.DisplayText;

            if (SelectedItemDef != null)
            {
                if (SelectedItemDef.Value.Key is not string _tmp0)
                    throw new($"{string.Join('.', nameof(SelectedItemDef), nameof(SelectedItemDef.Value.Key))} is not a string.");

                EditItem.DefIndex = _tmp0;
            }
        }

        if (e.PropertyName == nameof(SelectedPaintKitDef))
        {
            __SelectedPaintKitDefChanged = true;
            SelectedPaintKitDefDisplayName = SelectedPaintKitDef?.Value.DisplayText;

            if (SelectedPaintKitDef != null)
            {
                if (SelectedPaintKitDef.Value.Key is not string _tmp1)
                    throw new($"{string.Join('.', nameof(SelectedPaintKitDef), nameof(SelectedPaintKitDef.Value.Key))} is not a string.");

                EditItem.Attributes ??= [];
                EditItem.Attributes["6"] = _tmp1;
            }
        }
    }

    // Save Item Edit
    [RelayCommand]
    private void SaveEditItem()
    {
        if (_edit is not null)
            _ = _completionSource.TrySetResult(Operation.Save);
        else
            _ = _completionSource.TrySetCanceled();
    }

    // Cancel Item Edit
    [RelayCommand]
    private void CancelEditItem()
    {
        if (_edit is not null)
            _ = _completionSource.TrySetResult(Operation.Cancel);
        else
            _ = _completionSource.TrySetCanceled();
    }
}

