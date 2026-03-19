using Avalonia.Controls.Documents;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CSGO_GC_Ext.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    public partial class AppBarProperties : ObservableObject
    {
        [ObservableProperty]
        public partial bool Show { get; set; } = true;

        [ObservableProperty]
        public partial bool ShowWindowButtons { get; set; } = true;

        [ObservableProperty]
        public partial bool ShowMenuButton { get; set; } = true;

        [ObservableProperty]
        public partial bool ShowTitle { get; set; } = true;


        [ObservableProperty]
        public partial object? TitleContent { get; set; } = null;

        [ObservableProperty]
        public partial object? TitleTagContent { get; set; } = null;

        // TODO: Menu Buttons Custom
    }

    [ObservableProperty]
    public partial AppBarProperties? AppBarStyle { get; set; } = null;
}
