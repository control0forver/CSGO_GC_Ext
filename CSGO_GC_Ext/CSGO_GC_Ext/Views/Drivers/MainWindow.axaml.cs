using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CSGO_GC_Ext.Views.Drivers;

public partial class MainWindow : Window
{
    public bool MoveWindow { get; set; } = false;

    public MainWindow()
    {
        InitializeComponent();

        this.Loaded += (_, _) =>
        {
            __OnWindowLayoutUpdated1_last = this.WindowState;
            __OnWindowLayoutUpdated1_ready = true;
            return;
        };
        this.LayoutUpdated += OnWindowLayoutUpdated1;
    }

    bool __OnWindowLayoutUpdated1_ready = false;
    WindowState? __OnWindowLayoutUpdated1_last = null;
    int __OnWindowLayoutUpdated1_last_same_count = 0;
    const int __OnWindowLayoutUpdated1_from_fullscreen_to_normal_count_magic = 3; // Test OS: Windows 10
    int __OnWindowLayoutUpdated1_from_fullscreen_to_normal_count = 0;
    /// <summary>
    /// Window shadow attach for Windows
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnWindowLayoutUpdated1(object? sender, EventArgs e)
    {
        if (!__OnWindowLayoutUpdated1_ready)
            return;

        var currentState = this.WindowState;

        if (__OnWindowLayoutUpdated1_last is not null)
        {
            if (__OnWindowLayoutUpdated1_last == currentState)
            {
                __OnWindowLayoutUpdated1_last_same_count++;
                if (__OnWindowLayoutUpdated1_last_same_count >= 2)
                {
                    return;
                }
            }
            else
                __OnWindowLayoutUpdated1_last_same_count = 0;

            if (__OnWindowLayoutUpdated1_last is WindowState.FullScreen)
            {
                if (__OnWindowLayoutUpdated1_from_fullscreen_to_normal_count < __OnWindowLayoutUpdated1_from_fullscreen_to_normal_count_magic)
                {
                    // Skip
                    __OnWindowLayoutUpdated1_from_fullscreen_to_normal_count++;
                    return;
                }
                else
                {
                    __OnWindowLayoutUpdated1_from_fullscreen_to_normal_count = 0;
                }
            }
        }

        WindowsNative.NotifyWin32BorderUpdate(this, currentState);
        __OnWindowLayoutUpdated1_last = currentState;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (MoveWindow)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;

            this.BeginMoveDrag(e);

            MoveWindow = false;
        }
    }

    internal static class WindowsNative
    {
        #region public helpers

        public static void NotifyWin32BorderUpdate(Window window, WindowState? v = null)
        {
            if (!OperatingSystem.IsWindows())
                return;

            var __hwnd = window.TryGetPlatformHandle()?.Handle;
            if (__hwnd is not nint hwnd)
                return;

            v ??= window.WindowState;

            if (v == WindowState.Maximized)
            {
                var workArea = new WindowsNative.RECT();
                WindowsNative.SystemParametersInfo(0x0030, 0, ref workArea, 0);
                WindowsNative.SetWindowPos(hwnd, IntPtr.Zero,
                    0, 0,
                    workArea.Right - workArea.Left - 1,
                    workArea.Bottom - workArea.Top - 1,
                    WindowsNative.SetWindowPosFlags.SWP_NOZORDER | WindowsNative.SetWindowPosFlags.SWP_NOACTIVATE);
            }
            else
            {
                var margins = new WindowsNative.MARGINS
                {
                    cyBottomHeight = 1,
                    cxRightWidth = 1,
                    cxLeftWidth = 1,
                    cyTopHeight = 1
                };
                Marshal.ThrowExceptionForHR(WindowsNative.DwmExtendFrameIntoClientArea(hwnd, ref margins));
            }
        }

        #endregion

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        
        [Flags]
        private enum SetWindowPosFlags : uint
        {
            SWP_ASYNCWINDOWPOS = 0x4000,
            SWP_DEFERERASE = 0x2000,
            SWP_DRAWFRAME = 0x0020,
            SWP_FRAMECHANGED = 0x0020,
            SWP_HIDEWINDOW = 0x0080,
            SWP_NOACTIVATE = 0x0010,
            SWP_NOCOPYBITS = 0x0100,
            SWP_NOMOVE = 0x0002,
            SWP_NOOWNERZORDER = 0x0200,
            SWP_NOREDRAW = 0x0008,
            SWP_NOREPOSITION = 0x0200,
            SWP_NOSENDCHANGING = 0x0400,
            SWP_NOSIZE = 0x0001,
            SWP_NOZORDER = 0x0004,
            SWP_SHOWWINDOW = 0x0040
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);
        [DllImport("dwmapi.dll")]
        private static extern int DwmIsCompositionEnabled(out bool pfEnabled);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);
    }
}