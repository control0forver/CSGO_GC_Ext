using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CSGO_GC_Ext.Utils;
using CSGO_GC_Ext.Utils.Game;
using CSGO_GC_Ext.ViewModels;
using log4net;
using Swordfish.NET.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static CSGO_GC_Ext.Utils.Game.CSGOTxtHelper.CSGOJsonLikeTxtHelper;
using ValueType = CSGO_GC_Ext.Utils.Game.CSGOTxtHelper.CSGOJsonLikeTxtHelper.ValueType;

namespace CSGO_GC_Ext.Models;

[TxtProperty(key: null, ValueType.Scope, required: false)] // TODO: this is not significant
public partial class InventoryItem : ObservableObject, IInventoryItemSearcherSearchable, ICloneable
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(InventoryItem));

    public static class ItemCategories
    {
        #region MenuCategoryIndex
        public const int MenuCategoryAllIndex = 0;

        public const int MenuCategoryIndexMelee = 1;
        public const int MenuCategoryIndexPistol = 2;
        public const int MenuCategoryIndexSMG = 3;
        public const int MenuCategoryIndexRifle = 4;
        public const int MenuCategoryIndexHeavy = 5;
        public const int MenuCategoryIndexAgents = 6;
        public const int MenuCategoryIndexGloves = 7;
        public const int MenuCategoryIndexMusicKits = 8;
        public const int MenuCategoryIndexPatches = 9;
        public const int MenuCategoryIndexStickers = 10;
        public const int MenuCategoryIndexGraffiti = 11;
        public const int MenuCategoryIndexWeaponCases = 12;
        //public const int MenuCategoryIndexStickerCapsules = 13;
        public const int MenuCategoryIndexStickerCapsules = MenuCategoryIndexStickers;
        //public const int MenuCategoryIndexGraffitiBoxes = 14;
        public const int MenuCategoryIndexGraffitiBoxes = MenuCategoryIndexGraffiti;
        public const int MenuCategoryIndexSouvenirCases = 15;
        public const int MenuCategoryIndexTools = 16;
        public const int MenuCategoryIndexMedals = 17;
        #endregion

        #region ItemCategory Enums
        public enum ItemCategory : int
        {
            [Display(Name = "Everything"), MenuCategoryType.MenuCategoryType(typeof(EverythingMenuCategory))]
            Everything,
            [Display(Name = "Equipment"), MenuCategoryType.MenuCategoryType(typeof(EquipmentMenuCategory))]
            Equipment,
            [Display(Name = "Stickers, Graffiti, & Patches"), MenuCategoryType.MenuCategoryType(typeof(SGPMenuCategory))]
            SGP, // Stickers, Graffiti, & Patches
            [Display(Name = "Contains & More"), MenuCategoryType.MenuCategoryType(typeof(CMMenuCategory))]
            CM, // Contains & More
            [Display(Name = "Display"), MenuCategoryType.MenuCategoryType(typeof(DisplayMenuCategory))]
            Display,
        }
        public enum EverythingMenuCategory : int
        {
            [Display(Name = "Everything")]
            All = MenuCategoryAllIndex,
        }
        public enum EquipmentMenuCategory : int
        {
            [Display(Name = "All Equipment")]
            All = MenuCategoryAllIndex,
            [Display(Name = "Melee")]
            Melee = MenuCategoryIndexMelee,
            [Display(Name = "Pistol")]
            Pistol = MenuCategoryIndexPistol,
            [Display(Name = "SMG")]
            SMG = MenuCategoryIndexSMG,
            [Display(Name = "Rifle")]
            Rifle = MenuCategoryIndexRifle,
            [Display(Name = "Heavy")]
            Heavy = MenuCategoryIndexHeavy,
            [Display(Name = "Agents")]
            Agents = MenuCategoryIndexAgents,
            [Display(Name = "Gloves")]
            Gloves = MenuCategoryIndexGloves,
            [Display(Name = "Music Kits")]
            MusicKits = MenuCategoryIndexMusicKits,
        }
        public enum SGPMenuCategory : int
        {
            [Display(Name = "All Graphic Art")]
            All = MenuCategoryAllIndex,
            [Display(Name = "Patches")]
            Patches = MenuCategoryIndexPatches,
            [Display(Name = "Stickers")]
            Stickers = MenuCategoryIndexStickers,
            [Display(Name = "Graffiti")]
            Graffiti = MenuCategoryIndexGraffiti,
        }
        public enum CMMenuCategory : int
        {
            [Display(Name = "All")]
            All = MenuCategoryAllIndex,
            [Display(Name = "Weapon Cases")]
            WeaponCases = MenuCategoryIndexWeaponCases,
            [Display(Name = "Sticker Capsules")]
            StickerCapsules = MenuCategoryIndexStickerCapsules,
            [Display(Name = "Graffiti Boxes")]
            GraffitiBoxes = MenuCategoryIndexGraffitiBoxes,
            [Display(Name = "Souvenir Cases")]
            SouvenirCases = MenuCategoryIndexSouvenirCases,
            [Display(Name = "Tools")]
            Tools = MenuCategoryIndexTools,
        }
        public enum DisplayMenuCategory : int
        {
            [Display(Name = "All")]
            All = MenuCategoryAllIndex,
            [Display(Name = "Medals")]
            Medals = MenuCategoryIndexMedals,
            [Display(Name = "Music Kits")]
            MusicKits = MenuCategoryIndexMusicKits,
        }
        #endregion

        public static readonly Type[] MenuCategoryEnumTypes =
            [typeof(EverythingMenuCategory), typeof(EquipmentMenuCategory), typeof(SGPMenuCategory), typeof(CMMenuCategory), typeof(DisplayMenuCategory)];

        // TODO: this is not a 100% accurate way to determine the menu category
        public static readonly ReadOnlyDictionary<string, int> CategoryClassPairs = new Dictionary<string, int>
        {
            {"weapon_knife", MenuCategoryIndexMelee},
            {"weapon_knifegg", MenuCategoryIndexMelee},
            {"weapon_deagle", MenuCategoryIndexPistol},
            {"weapon_elite", MenuCategoryIndexPistol},
            {"weapon_fiveseven", MenuCategoryIndexPistol},
            {"weapon_glock", MenuCategoryIndexPistol},
            {"weapon_hkp2000", MenuCategoryIndexPistol},
            {"weapon_p250", MenuCategoryIndexPistol},
            {"weapon_tec9", MenuCategoryIndexPistol},
            {"weapon_bizon", MenuCategoryIndexSMG},
            {"weapon_mac10", MenuCategoryIndexSMG},
            {"weapon_mp7", MenuCategoryIndexSMG},
            {"weapon_mp9", MenuCategoryIndexSMG},
            {"weapon_p90", MenuCategoryIndexSMG},
            {"weapon_ump45", MenuCategoryIndexSMG},
            {"weapon_ak47", MenuCategoryIndexRifle},
            {"weapon_aug", MenuCategoryIndexRifle},
            {"weapon_famas", MenuCategoryIndexRifle},
            {"weapon_galilar", MenuCategoryIndexRifle},
            {"weapon_m4a1", MenuCategoryIndexRifle},
            {"weapon_sg556", MenuCategoryIndexRifle},
            {"weapon_awp", MenuCategoryIndexRifle},
            {"weapon_g3sg1", MenuCategoryIndexRifle},
            {"weapon_scar20", MenuCategoryIndexRifle},
            {"weapon_ssg08", MenuCategoryIndexRifle},
            {"weapon_mag7", MenuCategoryIndexHeavy},
            {"weapon_nova", MenuCategoryIndexHeavy},
            {"weapon_sawedoff", MenuCategoryIndexHeavy},
            {"weapon_xm1014", MenuCategoryIndexHeavy},
            {"weapon_m249", MenuCategoryIndexHeavy},
            {"weapon_negev", MenuCategoryIndexHeavy},
            // {"weapon_hegrenade", MenuCategoryIndexGrenade},
            // {"weapon_incgrenade", MenuCategoryIndexGrenade},
            // {"weapon_molotov", MenuCategoryIndexGrenade},
            // {"weapon_smokegrenade", MenuCategoryIndexGrenade},
            // {"weapon_flashbang", MenuCategoryIndexGrenade},
            // {"weapon_decoy", MenuCategoryIndexGrenade},
            {"weapon_taser", MenuCategoryIndexTools},
            {"item_defuser", MenuCategoryIndexTools},
            {"item_kevlar", MenuCategoryIndexTools},
            {"item_assaultsuit", MenuCategoryIndexTools},
            {"item_heavyassaultsuit", MenuCategoryIndexTools},
            {"weapon_healthshot", MenuCategoryIndexTools},
            {"weapon_fists", MenuCategoryIndexMelee},
            {"weapon_shield", MenuCategoryIndexTools},
            {"wearable_item", MenuCategoryIndexAgents}, // For agents and gloves
            {"collectible_item", MenuCategoryIndexPatches}, // Most collectibles are patches
            {"supply_crate", MenuCategoryIndexWeaponCases}, // Includes cases and capsules
            {"tool", MenuCategoryIndexTools},
            {"map_token", MenuCategoryIndexMedals},
            {"item_nvgs", MenuCategoryIndexTools},
            {"weapon_c4", MenuCategoryIndexTools},
            // {"weapon_tagrenade", MenuCategoryIndexGrenade},
            // {"weapon_snowball", MenuCategoryIndexGrenade},
            {"weapon_zone_repulsor", MenuCategoryIndexTools},
            {"weapon_breachcharge", MenuCategoryIndexTools},
            {"weapon_bumpmine", MenuCategoryIndexTools},
            {"weapon_tablet", MenuCategoryIndexTools},
            {"weapon_melee", MenuCategoryIndexMelee},
        }.AsReadOnly();

        public static string? TryGetItemClassNameByMenuCategoryIndex(int index)
        {
            foreach (var pair in CategoryClassPairs)
                if (pair.Value == index) return pair.Key;

            return null;
        }

        public static int? TryGetMenuCategoryIndexByItemClassName(string? className)
        {
            if (className == null)
                return MenuCategoryAllIndex;

            if (CategoryClassPairs.TryGetValue(className, out var index))
                return index;

            return null;
        }
    }

    public enum Sorting
    {
        [Display(Name = "Newest")]
        Newest,
        [Display(Name = "Quality")]
        Quality,
        [Display(Name = "Alphabetical")]
        Alphabetical,
        [Display(Name = "Equip Slot")]
        EquipSlot,
        [Display(Name = "Collection")]
        Collection,
        [Display(Name = "Equipped")]
        Equipped,
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisualImage))]
    public partial Func<CSGOGameResources> CSGOGameResourcesProvider { get; set; }

    // [ObservableProperty]
    // public partial int Quantity { get; set; } = 1;

    [ObservableProperty]
    public partial int? Id { get; set; } = null;

    [ObservableProperty]
    [TxtProperty("inventory", ValueType.IntString)]
    public partial int? Inventory { get; set; } = null;

    [ObservableProperty]
    [TxtProperty("def_index", ValueType.String)]
    public partial string DefIndex { get; set; } = "default";

    [ObservableProperty]
    [TxtProperty("level", ValueType.IntString, required: false)]
    public partial int? Level { get; set; } = null;

    [ObservableProperty]
    [TxtProperty("quality", ValueType.IntString, required: false)]
    public partial int? Quality { get; set; } = null;

    [ObservableProperty]
    [TxtProperty("flags", ValueType.IntString, required: false)]
    public partial int? Flags { get; set; } = null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayCustomName), nameof(FullDisplayName))]
    [TxtProperty("origin", ValueType.IntString, required: false)]
    public partial int? Origin { get; set; } = null;

    [ObservableProperty]
    [TxtProperty("custom_name", ValueType.String, required: false)]
    public partial string? CustomName { get; set; } = null;

    [ObservableProperty]
    [TxtProperty("in_use", ValueType.IntString, required: false)]
    public partial int? InUse { get; set; } = null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RarityBrush))]
    [TxtProperty("rarity", ValueType.IntString, required: false)]
    public partial int? Rarity { get; set; } = null;

    [ObservableProperty]
    [TxtProperty("attributes", ValueType.Scope, required: false
    //,scopeKeys:
    //[
    //    "6", // (value_is_additive) class set_item_texture_prefab
    //    "7", // (value_is_additive) class set_item_texture_seed
    //    "8", // (value_is_additive) class set_item_texture_wear
    //]
    )]
    public partial ConcurrentObservableDictionary<string, string>? Attributes { get; set; } = null;

    [ObservableProperty]
    [TxtProperty("equipped_state", ValueType.Scope, required: false)]
    public partial ConcurrentObservableDictionary<string, string>? EquippedState { get; set; } = null;

    public string? DisplayCustomName => CustomName is null ? null : $"\"{CustomName}\"";

    // TODO; ★ 武器名称显示标签
    public string FullDisplayName => DisplayCustomName ?? $"{DisplayName} | {DisplaySkinName}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullDisplayName))]
    public partial string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullDisplayName))]
    public partial string DisplaySkinName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DisplayRarityName { get; set; } = string.Empty;



    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CategoryDisplayName))]
    public partial int Category { get; private set; } = ItemCategories.MenuCategoryAllIndex; // Set when DefIndex is set

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisualImage))]
    public partial string DisplayImageToken { get; set; } = string.Empty;

    // Inventory Searcher Fields
    [ObservableProperty]
    public partial ObservableCollection<string> ItemsTokenSearchFields { get; set; }
    private IReadOnlyList<string>? _cachedSearchTokens = null;
    public IEnumerable<string> SearchTokens
    {
        get
        {
            // Search Tokens cache rebuilds here (if null)
            return _cachedSearchTokens ??= BuildSearchTokensCache();
        }
    }
    protected virtual List<string> BuildSearchTokensCache()
    {
        var _ts = new List<string>();
        _ts.AddRange(Reflection.GetPropertyValues<InventoryItem, string>(this, ItemsTokenSearchFields));

        return _ts;
    }

    private string? _item_name_cached = null;
    private string? _paint_kit_name_cached = null;
    private string BuildDisplayImageToken()
    {
        string?[] collections = [_item_name_cached, _paint_kit_name_cached];
        var _t = string.Join("_", collections.Where(x => x != null));
        return _t;
    }

    public InventoryItem(Func<CSGOGameResources> CSGOGameResourcesProvider)
    {
        this.CSGOGameResourcesProvider = CSGOGameResourcesProvider;

        ItemsTokenSearchFields = [$"{Reflection.SelfPrefix}.{nameof(DisplayName)}", $"{Reflection.SelfPrefix}.{nameof(DisplaySkinName)}"];
    }

    protected override void OnPropertyChanging(PropertyChangingEventArgs e)
    {
        base.OnPropertyChanging(e);

        if (e.PropertyName == nameof(ItemsTokenSearchFields) && ItemsTokenSearchFields != null)
        {
            ItemsTokenSearchFields.CollectionChanged -= OnItemsSearchTokenFinderCollectionChanged;
            return;
        }

        if (e.PropertyName == nameof(Attributes) && Attributes != null)
        {
            Attributes.CollectionChanged -= OnAttributesChanged;
            return;
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (ItemsTokenSearchFields != null && ItemsTokenSearchFields.Any(x => x[Reflection.SelfPrefix.Length..] == e.PropertyName))
            _cachedSearchTokens = null; // Invalidate cached search tokens


        if (e.PropertyName == nameof(ItemsTokenSearchFields))
        {
            ItemsTokenSearchFields!.CollectionChanged += OnItemsSearchTokenFinderCollectionChanged;
            OnItemsSearchTokenFinderCollectionChanged(null, new(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
            return;
        }

        if (e.PropertyName == nameof(Attributes) && Attributes != null)
        {
            Attributes.CollectionChanged += OnAttributesChanged;
            OnAttributesChanged(null, (System.Collections.Specialized.NotifyCollectionChangedEventArgs)new(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
            return;
        }

        if (e.PropertyName == nameof(Rarity))
        {
            if (CSGOGameResourcesProvider is null)
                return;

            var resource = CSGOGameResourcesProvider();
            if (resource.GameItems is null)
            {
                void callback(object? sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName != nameof(resource.GameItems) ||
                        resource.GameItems is null)
                        return;

                    resource.PropertyChanged -= callback;
                    OnPropertyChanged(
                        e: new(nameof(Rarity))
                    );
                }
                resource.PropertyChanged += callback;
                return;
            }
            if (resource.CSGOTranslations is null)
            {
                void callback(object? sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName != nameof(resource.GameItems) ||
                        resource.GameItems is null)
                        return;

                    resource.PropertyChanged -= callback;
                    OnPropertyChanged(
                        e: new(nameof(Rarity))
                    );
                }
                resource.PropertyChanged += callback;
                return;
            }
            var _translation_tokens = resource.CSGOTranslations.TryGetSelfRecursiveValue("lang")?.TryGetSelfRecursiveValue("Tokens");
            if (_translation_tokens is null)
            {
                Log.Error("Cannot get lang.Tokens from csgo translation txt file.");
                return;
            }

            dynamic reader = resource.GameItems["items_game"];
            dynamic readerRarities = reader["rarities"];

            // TODO: Make a cache for this
            foreach (dynamic ikey in readerRarities.Keys)
            {
                dynamic rarity = readerRarities[ikey];
                if (rarity["value"] == Rarity.ToString())
                {
                    var _rarity_localized_display_name = _translation_tokens.TryGetStringValue((string)rarity["loc_key"]) ?? string.Empty;
                    var _rarity_for_weapons_localized_display_name = _translation_tokens.TryGetStringValue((string)rarity["loc_key_weapon"]) ?? string.Empty;
                    var _rarity_for_characters_localized_display_name = _translation_tokens.TryGetStringValue((string)rarity["loc_key_character"]) ?? string.Empty;
                    DisplayRarityName = string.Join("||", _rarity_for_weapons_localized_display_name, _rarity_for_weapons_localized_display_name, _rarity_for_characters_localized_display_name); // TODO: ...
                    return;
                }
            }

            Log.Error($"Cannot get the rarity name from value '{Rarity}' in items_game.rarities. (items_game.txt)");
            return;
        }

        if (e.PropertyName == nameof(DefIndex))
        {
            if (CSGOGameResourcesProvider is null)
                throw new InvalidOperationException($"{nameof(CSGOGameResourcesProvider)} should't be null here.");

            DisplayName = $"{{{DefIndex}}}";
            _item_name_cached = null; // Reset the cache

            var resource = CSGOGameResourcesProvider();
            if (resource.GameItems is not null)
            {
                if (resource.CSGOTranslations is not null)
                {
                    var _translation_tokens = resource.CSGOTranslations.TryGetSelfRecursiveValue("lang")?.TryGetSelfRecursiveValue("Tokens");
                    if (_translation_tokens is null)
                    {
                        Log.Error("Cannot get lang.Tokens from csgo translation txt file.");
                        return;
                    }

                    var _reader = resource.GameItems.TryGetSelfRecursiveValue("items_game");
                    var _reader_game_items = _reader?.TryGetSelfRecursiveValue("items");
                    var _reader_game_prefabs = _reader?.TryGetSelfRecursiveValue("prefabs");
                    if (_reader is null)
                    {
                        Log.Error("Cannot get items_game from items_game.txt.");
                        return;
                    }
                    if (_reader_game_items is null)
                    {
                        Log.Error("Cannot get items_game.items from items_game.txt.");
                        return;
                    }
                    if (_reader_game_prefabs is null)
                    {
                        Log.Error("Cannot get items_game.prefabs from items_game.txt.");
                        return;
                    }

                    // Get the item
                    var _game_item = _reader_game_items.TryGetSelfRecursiveValue(DefIndex);
                    if (_game_item is null)
                    {
                        Log.Error($"Cannot get the game item from index {DefIndex} in items_game.items. (items_game.txt)");
                        return;
                    }

                    // Get the item name (optional)
                    if (_game_item.TryGetStringValue("name") is string __name)
                    {
                        _item_name_cached = __name;
                        DisplayName = $"{{{__name}}}";
                    }
                    else
                    {
                        Log.Error($"Cannot get the 'name' from game item of item (index {DefIndex}). (items_game.txt)");
                    }

                    static object? __get_item_property(
                        Dictionary<string, object> @this,
                        string property_name,
                        string recursion_class_name_reference_property_name = "prefab",
                        Dictionary<string, object>? search_recursion_class = null
                    )
                    {
                        if (@this.TryGetStringValue(property_name) is string _property_value)
                            return _property_value;

                        if (@this.TryGetStringValue(recursion_class_name_reference_property_name) is string _recursion_class_name)
                            if ((search_recursion_class ?? @this).TryGetSelfRecursiveValue(_recursion_class_name) is Dictionary<string, object> _recursion_class)
                                return __get_item_property(
                                    _recursion_class,
                                    property_name, recursion_class_name_reference_property_name, search_recursion_class
                                );

                        return null;
                    }

                    // Get the item_class
                    if (__get_item_property(_game_item, "item_class", search_recursion_class: _reader_game_prefabs)
                            is not string _game_item_item_class ||
                        ItemCategories.TryGetMenuCategoryIndexByItemClassName(_game_item_item_class)
                            is not int _index)
                    {
                        Log.Error($"Cannot get the 'item_class' from game item of item (index {DefIndex}). (items_game.txt)");
                    }
                    else
                        Category = _index;

                    // Get the item_name
                    if (__get_item_property(_game_item, "item_name", search_recursion_class: _reader_game_prefabs)
                        is not string _game_item_item_name)
                    {
                        Log.Error($"Cannot get the 'item_name' from game item of item (index {DefIndex}). (items_game.txt)");
                        return;
                    }

                    if (_game_item_item_name.StartsWith('#') &&
                        _translation_tokens.TryGetStringValue(_game_item_item_name[1..]) is string _display_name)
                        DisplayName = _display_name;
                    else
                        DisplayName = _game_item_item_name; // DispatchUpdate DisplayName
                    DisplayImageToken = BuildDisplayImageToken(); // DispatchUpdate DisplayImageToken
                }
                else
                {
                    void callback(object? sender, PropertyChangedEventArgs e)
                    {
                        if (e.PropertyName != nameof(resource.CSGOTranslations) ||
                            resource.CSGOTranslations is null)
                            return;

                        resource.PropertyChanged -= callback;
                        OnPropertyChanged(
                            e: new(nameof(DefIndex))
                        );
                    }
                    resource.PropertyChanged += callback;
                }
            }
            else
            {
                void callback(object? sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName != nameof(resource.GameItems) ||
                        resource.GameItems is null)
                        return;

                    resource.PropertyChanged -= callback;
                    OnPropertyChanged(
                        e: new(nameof(DefIndex))
                    );
                }
                resource.PropertyChanged += callback;
            }

            return;
        }
    }

    private void OnItemsSearchTokenFinderCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Clear cache
        _cachedSearchTokens = null;
    }

    private void OnAttributesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (Attributes is null)
            throw new($"{nameof(Attributes)} is null.");

        if (e.OldItems is not null)
            foreach (var __removeal in e.OldItems)
            {

            }
        if (e.NewItems is not null || e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            foreach (var __update in
                    (
                        e.NewItems ??
                        Attributes.ToList() // || e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset
                    )
            )
            {
                if (__update is not KeyValuePair<string, string> update)
                    throw new ArgumentException("New item is not a KeyValuePair<string, string>");

                /// Skin texture
                /// NOTICE: Do not read e.kvs.Value, cuz it may be structed by some user callbacks.
                if (update.Key == "6" &&
                    Attributes.TryGetValue("6", out var _attr_set_item_texture_prefab) && _attr_set_item_texture_prefab != null)
                {
                    if (CSGOGameResourcesProvider is null)
                        throw new InvalidOperationException($"{nameof(CSGOGameResourcesProvider)} should't be null here.");

                    _paint_kit_name_cached = null; // Reset the cache

                    var resource = CSGOGameResourcesProvider();
                    if (resource.GameItems is not null)
                    {
                        if (resource.CSGOTranslations is not null)
                        {
                            var _translation_tokens = resource.CSGOTranslations.TryGetSelfRecursiveValue("lang")?.TryGetSelfRecursiveValue("Tokens");
                            if (_translation_tokens is null)
                            {
                                Log.Error("Cannot get lang.Tokens from csgo translation txt file.");
                                return;
                            }

                            var _reader = resource.GameItems.TryGetSelfRecursiveValue("items_game");
                            var _reader_game_items = _reader?.TryGetSelfRecursiveValue("items");
                            var _reader_game_prefabs = _reader?.TryGetSelfRecursiveValue("prefabs");
                            var _reader_game_paint_kits = _reader?.TryGetSelfRecursiveValue("paint_kits");
                            // dynamic _reader = resource.GameItems["items_game"];
                            // dynamic _reader_game_items = resource.GameItems["items"];
                            // dynamic _reader_game_prefabs = resource.GameItems["prefabs"];
                            // // dynamic _reader_game_attributes = resource.GameItems["attributes"];
                            // // dynamic _reader_game_paint_kits_rarity = resource.GameItems["rarities"];
                            if (_reader is null)
                            {
                                Log.Error("Cannot get items_game from items_game.txt.");
                                return;
                            }
                            if (_reader_game_items is null)
                            {
                                Log.Error("Cannot get items_game.items from items_game.txt.");
                                return;
                            }
                            if (_reader_game_prefabs is null)
                            {
                                Log.Error("Cannot get items_game.prefabs from items_game.txt.");
                                return;
                            }
                            if (_reader_game_paint_kits is null)
                            {
                                Log.Error("Cannot get items_game.paint_kits from items_game.txt.");
                                return;
                            }

                            // Get the paint kit
                            double __paint_kit_index = 0d; // default paint kit
                            _ = double.TryParse(_attr_set_item_texture_prefab, out __paint_kit_index);
                            string _paint_kit_index_str = ((int)__paint_kit_index).ToString();
                            var _paint_kit = _reader_game_paint_kits.TryGetSelfRecursiveValue(_paint_kit_index_str);
                            if (_paint_kit is null)
                            {
                                Log.Error($"Cannot get the paint kit from index {_paint_kit_index_str} in items_game.paint_kits. (items_game.txt)");
                                return;
                            }
                            // Get the paint kit name
                            if (_paint_kit.TryGetStringValue("name") is string _paint_kit_name)
                            {
                                _paint_kit_name_cached = _paint_kit_name;
                            }
                            // Get the paint kit description tag
                            if (_paint_kit.TryGetStringValue("description_tag") is not string _paint_kit_description_tag)
                            {
                                Log.Error($"Cannot get the paint kit description tag from item (index {DefIndex}). (items_game.txt)");
                                if (_paint_kit.TryGetStringValue("name") is string __name)
                                    DisplaySkinName = $"{{{__name}}}";
                                return;
                            }

                            if (_paint_kit_description_tag.StartsWith('#') &&
                                _translation_tokens.TryGetStringValue(_paint_kit_description_tag[1..]) is string _display_name)
                                DisplaySkinName = _display_name;
                            else
                                DisplaySkinName = _paint_kit_description_tag; // DispatchUpdate DisplaySkinName
                            DisplayImageToken = BuildDisplayImageToken(); // DispatchUpdate DisplayImageToken
                        }
                        else
                        {
                            void callback(object? sender, PropertyChangedEventArgs e)
                            {
                                if (e.PropertyName != nameof(resource.CSGOTranslations) ||
                                    resource.CSGOTranslations is null)
                                    return;

                                resource.PropertyChanged -= callback;
                                OnAttributesChanged(
                                    sender: null,
                                    e: new(
                                        System.Collections.Specialized.NotifyCollectionChangedAction.Reset,
                                        new KeyValuePair<string, string?>("6", null)
                                    )
                                );
                            }
                            resource.PropertyChanged += callback;
                        }
                    }
                    else
                    {
                        void callback(object? sender, PropertyChangedEventArgs e)
                        {
                            if (e.PropertyName != nameof(resource.GameItems) ||
                                resource.GameItems is null)
                                return;

                            resource.PropertyChanged -= callback;
                            OnAttributesChanged(
                                sender: null,
                                e: new(
                                    System.Collections.Specialized.NotifyCollectionChangedAction.Reset,
                                    new KeyValuePair<string, string?>("6", null)
                                )
                            );
                        }
                        resource.PropertyChanged += callback;
                    }
                }
            }
    }


    protected new virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
    }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisualImage))]
    private partial bool __visualImageNotifier { get; set; } = false;
    public void NotifyVisualImageUpdate()
    {
        __visualImageNotifier = !__visualImageNotifier;
        OnPropertyChanged(nameof(VisualImage));
    }

    private static readonly Bitmap VisualImage_default_image_unknown = new(AssetLoader.Open(new($"avares://{nameof(CSGO_GC_Ext)}/Assets/Images/Any.png")));
    private static readonly ConcurrentDictionary<string, (Lock lck, Bitmap? image)> VisualImage_cache = [];
    public Bitmap? VisualImage
    {
        get
        {
            if (CSGOGameResourcesProvider is null)
                throw new InvalidOperationException($"{nameof(CSGOGameResourcesProvider)} should't be null here.");

            if (string.IsNullOrEmpty(DisplayImageToken))
                return VisualImage_default_image_unknown; // B: Image token is empty

            if (Avalonia.Controls.Design.IsDesignMode)
            {
                return null;
            }

            var resource = CSGOGameResourcesProvider();
            if (resource.GameItemsCDN is null)
            {
                void callback(object? sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName != nameof(resource.GameItemsCDN) ||
                        resource.GameItemsCDN is null)
                        return;

                    resource.PropertyChanged -= callback;
                    NotifyVisualImageUpdate();
                }
                resource.PropertyChanged += callback;
                return VisualImage_default_image_unknown; // B(Callback): CDN is not loaded
            }

            static string __func_get_file_name(string input)
            {
                // Try Uri
                try
                {
                    if (Uri.TryCreate(input, UriKind.Absolute, out Uri? uri) &&
                        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps ||
                         uri.Scheme == Uri.UriSchemeFile))
                    {
                        return Path.GetFileName(uri.LocalPath);
                    }

                }
                catch { }

                // Or File Path
                return Path.GetFileName(input);
            }

            // GetImageFilePath
            if (!resource.GameItemsCDN.TryGetValue(DisplayImageToken, out var imageRemote))
            {
                Console.WriteLine($"[InventoryItem] GetImageFilePath: No matching image remote for {DisplayImageToken}");
                return VisualImage_default_image_unknown; // B: No matching image remote
            }
            var imageFilePath = Path.Combine("cache", __func_get_file_name(imageRemote));

            var __parent_dir = Path.GetDirectoryName(imageFilePath);
            if (__parent_dir is not null && !Directory.Exists(__parent_dir))
                Directory.CreateDirectory(__parent_dir);

            // Load
            var get = VisualImage_cache.GetOrAdd(
                imageFilePath,
                valueFactory: _ => (new(), null) // mark for loading
            );
            if (get.image is null)
            {
                Task.Run(() =>
                {
                    if (get.lck.TryEnter())
                    { // Begin Loading

                        // Get Image File
                        if (!File.Exists(imageFilePath))
                        { // Image dose not exist of local storage
                            try
                            {
                                Debug.WriteLine($"\"{imageFilePath}\" not exists, downloading from \"{imageRemote}\"");
                                using var httpClient = new HttpClient();

                                using var response = httpClient.GetAsync(imageRemote, HttpCompletionOption.ResponseHeadersRead).Result;
                                response.EnsureSuccessStatusCode();

                                using var contentStream = response.Content.ReadAsStreamAsync().Result;
                                using var fileStream = new FileStream(imageFilePath, FileMode.Create);
                                contentStream.CopyToAsync(fileStream).Wait();
                            }
                            catch
                            {
                                goto end; // Clean-Return: Do not try any more, just leave it with 'VisualImage_default_image_unknown'
                            }
                        }

                        // Load Image
                        Bitmap? image = null;
                        try
                        {
                            using var fs = File.OpenRead(imageFilePath);
                            image = new Bitmap(File.OpenRead(imageFilePath));
                        }
                        catch { } // Do not try any more, just leave it with 'VisualImage_default_image_unknown'

                        if (image is not null)
                        {
                            _ = VisualImage_cache.TryUpdate(imageFilePath, (get.lck, image), get);
                            NotifyVisualImageUpdate();
                        }

                    end:
                        get.lck.Exit();
                    }
                    else
                    { // Wait for loading
                        get.lck.Enter();
                        get.lck.Exit();
                        NotifyVisualImageUpdate();
                    }
                });
                return VisualImage_default_image_unknown; // B(Callback): Image dose not existing in memory cache pool.
            }

            return get.image;
        }
    }

    private static readonly IImmutableBrush __DefaultRarityBrush = Brushes.Transparent;
    public IImmutableBrush RarityBrush
    {
        get
        {
            if (CSGOGameResourcesProvider is null || Rarity is null)
                return __DefaultRarityBrush;

            var resource = CSGOGameResourcesProvider();
            if (resource.GameItems is null)
            {
                void callback(object? sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName != nameof(resource.GameItems) ||
                        resource.GameItems is null)
                        return;

                    resource.PropertyChanged -= callback;
                    NotifyVisualImageUpdate();
                }
                resource.PropertyChanged += callback;
                return __DefaultRarityBrush;
            }

            dynamic reader = resource.GameItems["items_game"];
            dynamic readerRarities = reader["rarities"];
            dynamic readerColors = reader["colors"];

            // TODO: Make a cache for this
            foreach (dynamic ikey in readerRarities.Keys)
            {
                dynamic rarity = readerRarities[ikey];
                if (rarity["value"] == Rarity.ToString())
                {
                    dynamic colorName = rarity["color"];
                    dynamic color = readerColors[colorName]["hex_color"];
                    return SolidColorBrush.Parse(color).ToImmutable();
                }
            }

            Log.Error($"Cannot get the rarity from value '{Rarity}' in items_game.rarities. (items_game.txt)");
            return __DefaultRarityBrush;
        }
    }

    public string? CategoryDisplayName =>
        GetCategoryEnumDisplayName(
            (Enum)
            Enum.ToObject(
                FindValidMenuCategoryEnumTypeFromIndex(Category) ?? throw new($"Invalid {nameof(Category)}({Category}) which not exists in {nameof(ItemCategories.MenuCategoryEnumTypes)}"),
                Category
            )
        );

    public object Clone()
    {
        var _v = this.ToCSGOTxt().First();
        if (FromCSGOTxt(int.Parse(_v.Key), (Dictionary<string, object>)_v.Value, this.CSGOGameResourcesProvider) is not InventoryItem _r)
        {
            var _err_msg = "Failed to clone an inventory item.";
            Log.Fatal(_err_msg);
            throw new(_err_msg);
        }

        return _r;
    }

    /// <summary>
    /// Create a deep copy of <see cref="InventoryItem"/>.
    /// </summary>
    /// <returns>A deep copy of <see cref="InventoryItem"/>.</returns>
    public InventoryItem Clone(object? _ = null)
    {
        return (InventoryItem)Clone();
    }

    public static class MenuCategoryType
    {
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
        public class MenuCategoryTypeAttribute(Type menuCategoryType) : Attribute
        {
            public Type MenuCategoryType { get; } = menuCategoryType;
        }

        private static readonly Dictionary<Enum, Type> _cache = [];
        private static readonly List<Type> _cache_built_enum_types = [];

        private static void BuildCache(Type enumType)
        {
            if (_cache_built_enum_types.Contains(enumType))
                return;

            foreach (var value in Enum.GetValues(enumType).Cast<Enum>())
            {
                var field = enumType.GetField(value.ToString());
                var attribute = field?.GetCustomAttribute<MenuCategoryTypeAttribute>();
                var link = attribute?.MenuCategoryType;
                if (link is not null)
                    _cache[value] = link;
            }

            _cache_built_enum_types.Add(enumType);
        }

        public static Type? GetMenuCategoryType(ItemCategories.ItemCategory category)
        {
            BuildCache(category.GetType());
            return _cache.TryGetValue(category, out var type) ? type : null;
        }
    }
    public static Type? FindValidMenuCategoryEnumTypeFromIndex(int index)
    {
        if (index == ItemCategories.MenuCategoryAllIndex)
            return typeof(ItemCategories.EverythingMenuCategory);

        foreach (var e in ItemCategories.MenuCategoryEnumTypes)
        {
            if (Enum.IsDefined(e, index))
                return e;
        }

        return null;
    }
    public static string? GetCategoryEnumDisplayName(Enum @enum)
    {
        return ((@enum.GetType().GetField(@enum.ToString()) ?? throw new($"Cannot enumerate {@enum} from {@enum.GetType()}.")).GetCustomAttribute<DisplayAttribute>() ?? throw new($"Cannot get DisplayAttribute from {@enum}")).Name;
    }

    public static InventoryItem? FromCSGOTxt(int? id, Dictionary<string, object> raw, Func<CSGOGameResources> CSGOGameResourcesProvider)
    {
        //if (raw.TryGetStringValue("def_index") is not string _item_def_index)
        //{
        //    Log.Warn($"Item def_index is empty, discard resolving."); // Ignore invalid items. (Without def_index)
        //    return null;
        //}

        var item = new InventoryItem(CSGOGameResourcesProvider)
        {
            Id = id
        };

        // Get item scopes
        var properties = typeof(InventoryItem).GetProperties();
        foreach (var property in properties)
        {
            var txtPropPreset = property.GetCustomAttribute<TxtPropertyAttribute>();
            if (txtPropPreset is null)
                continue;

            bool __func_resolve()
            {
                if (txtPropPreset.Key is null)
                {
                    if (txtPropPreset.Required)
                        return false;//throw new NotSupportedException("Value set is required, but the key is not provided");
                    else
                        return true;
                }

                switch (txtPropPreset.ValueType)
                {
                    default: throw new NotImplementedException();

                    case ValueType.String:
                        {
                            var _v = raw.TryGetStringValue(txtPropPreset.Key);
                            if (_v is null)
                                return !txtPropPreset.Required;

                            property.SetValue(item, _v);
                            return true;
                        }

                    case ValueType.IntString:
                        {
                            var _v = raw.TryGetStringValue(txtPropPreset.Key);
                            if (_v is null)
                                return !txtPropPreset.Required;
                            if (!int.TryParse(_v, out var _int_v))
                                return !txtPropPreset.Required;

                            property.SetValue(item, _int_v);
                            return true;
                        }

                    case ValueType.Scope:
                        {
                            var _scope_v = raw.TryGetSelfRecursiveValue(txtPropPreset.Key);
                            if (_scope_v is null)
                                return !txtPropPreset.Required;

                            List<KeyValuePair<string, string?>> scopeProps = [];
                            foreach (var scopePropKey
                                in txtPropPreset.ScopePropertiesKeys is null ? _scope_v.Keys : txtPropPreset.ScopePropertiesKeys // Take all values of the scope if no any specific keys are provided.
                            )
                            {
                                if (_scope_v.TryGetStringValue(scopePropKey) is not string _v_str)
                                    continue;

                                scopeProps.Add(new(scopePropKey, _v_str));
                            }

                            if (scopeProps.Count > 0) // Keep null if the scope dose not contain any values.
                            {
                                dynamic scope =
                                    Activator.CreateInstance(property.PropertyType)!; // property.PropertyType will never be a Nullable<T>
                                property.SetValue(item, scope);

                                foreach (var _v in scopeProps)
                                {
                                    scope[_v.Key] = _v.Value; // This is an unsafe code.
                                }
                            }

                            return true;
                        }
                }
                throw new(); // Everything should be done in the switch statement above.
            }

            if (!__func_resolve())
                return null;
        }

        return item;
    }
}

public static class InventoryItemExtensions
{
    public static Dictionary<string, object> ToCSGOTxt(this InventoryItem item)
    {
        if (item.Id is not int _item_id)
            throw new InvalidOperationException($"Item is not valid, make sure to Invoke the {string.Join('.', nameof(HomeViewModel), nameof(HomeViewModel.DispatchUpdate), "[__func_validate_async]")} Method before.");

        Dictionary<string, object> txt = [];
        Dictionary<string, object> raw = [];
        txt[_item_id.ToString()] = raw;

        // Get item properties
        var properties = typeof(InventoryItem).GetProperties();
        foreach (var property in properties)
        {
            var txtPropPreset = property.GetCustomAttribute<TxtPropertyAttribute>();
            if (txtPropPreset is null || txtPropPreset.Key is null)
                continue;

            var value = property.GetValue(item);
            if (value is null && txtPropPreset.Required)
                throw new InvalidOperationException($"Required property {property.Name} cannot be null");

            if (value is null)
                continue;

            switch (txtPropPreset.ValueType)
            {
                case ValueType.String:
                    if (value is string stringValue)
                        raw[txtPropPreset.Key] = stringValue;
                    break;

                case ValueType.IntString:
                    if (value is int intValue)
                        raw[txtPropPreset.Key] = intValue.ToString();
                    break;

                case ValueType.Scope:
                    // Handle ConcurrentObservableDictionary<string, string?>
                    if (value is ConcurrentObservableDictionary<string, string?> concurrentDict)
                    {
                        Dictionary<string, object> scopeDict = [];
                        foreach (var kvp in concurrentDict)
                        {
                            if (kvp.Value != null)
                            {
                                scopeDict[kvp.Key] = kvp.Value;
                            }
                        }
                        if (scopeDict.Count > 0)
                            raw[txtPropPreset.Key] = scopeDict;
                    }
                    // Handle IDictionary
                    else if (value is System.Collections.IDictionary dictValue)
                    {
                        Dictionary<string, object> scopeDict = [];
                        foreach (var key in dictValue.Keys)
                        {
                            if (key is string strKey && dictValue[key] is string strValue)
                            {
                                scopeDict[strKey] = strValue;
                            }
                        }
                        if (scopeDict.Count > 0)
                            raw[txtPropPreset.Key] = scopeDict;
                    }
                    break;
            }
        }

        return txt;
    }
}
