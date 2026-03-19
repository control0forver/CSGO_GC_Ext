using Avalonia;
using Avalonia.Controls;
using CSGO_GC_Ext.Utils.Views;
using CSGO_GC_Ext.ViewModels;
using log4net;
using System.Threading.Tasks;

namespace CSGO_GC_Ext.Views;

public partial class SettingsView : StackPanel, ViewHelper.INavigationAnimatable
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(SettingsView));
    private new SettingsViewModel DataContext
    {
        get => (SettingsViewModel)(base.DataContext ?? throw new("DataContext is null."));
        set => base.DataContext = value;
    }

    private readonly ViewHelper.SlideAnimation _pageSlideInAnimation = new();

    public SettingsView()
    {
        DataContext = new();

        InitializeComponent();
    }

    public SettingsView(Visual testParent) : this()
    {
        this.Arrange(testParent.Bounds);
    }

    private void LoadSlideInAnimation()
    {
        _pageSlideInAnimation.Clear();
        _pageSlideInAnimation.Load(this.FindVisualChildren<Visual>(-1, [typeof(ContentControl)]));
    }

    public bool OnNavigating(object? _ = null)
    {
        LoadSlideInAnimation();

        return true;
    }

    public void OnNavigated(object? _ = null)
    {
        _pageSlideInAnimation.Fire(ViewHelper.SlideAnimation.ActType.In, reset: true);
    }
}