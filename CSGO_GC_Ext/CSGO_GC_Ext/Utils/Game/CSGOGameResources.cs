using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace CSGO_GC_Ext.Utils;

public partial class CSGOGameResources : ObservableObject
{
    [ObservableProperty]
    public partial Dictionary<string, object>? GameItems { get; set; } = null;
    [ObservableProperty]
    public partial Dictionary<string, string>? GameItemsCDN { get; set; } = null;
    [ObservableProperty]
    public partial Dictionary<string, object>? CSGOTranslations { get; set; } = null;
}
