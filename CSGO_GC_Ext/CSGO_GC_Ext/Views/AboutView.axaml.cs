using Avalonia;
using Avalonia.Controls;
using CSGO_GC_Ext.Utils.Views;
using CSGO_GC_Ext.ViewModels;
using log4net;

namespace CSGO_GC_Ext.Views;

public partial class AboutView : UserControl, ViewHelper.INavigationAnimatable
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(AboutView));
    private new AboutViewModel DataContext
    {
        get => (AboutViewModel)(base.DataContext ?? throw new("DataContext is null."));
        set => base.DataContext = value;
    }

    private readonly ViewHelper.SlideAnimation _pageSlideInAnimation = new();

    public AboutView()
    {
        if (!Design.IsDesignMode)
        {
            DataContext = new();
        }

        InitializeComponent();
    }

    public AboutView(Visual testParent) : this()
    {
        this.Arrange(testParent.Bounds);
    }

    private void LoadSlideInAnimation()
    {
        _pageSlideInAnimation.Clear();
        _pageSlideInAnimation.Load(this.FindVisualChildren<Visual>(-1));
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