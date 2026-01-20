using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using WinUIWindow = Microsoft.UI.Xaml.Window;

namespace Timeline_Note_Taker.Platforms.Windows;

public class GlobalHotkeyService
{
    private const int HOTKEY_ID = 1;
    private IntPtr _windowHandle;
    private Action? _hotkeyCallback;
    private WndProcDelegate? _newWndProc;
    private IntPtr _oldWndProc;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const int GWL_WNDPROC = -4;

    public void RegisterHotkey(WinUIWindow window, Action callback)
    {
        _hotkeyCallback = callback;
        
        // Get the window handle
        var windowNative = WinRT.Interop.WindowNative.GetWindowHandle(window);
        _windowHandle = windowNative;

        // Register Win+Shift+N hotkey
        var registered = NativeMethods.RegisterHotKey(
            _windowHandle,
            HOTKEY_ID,
            NativeMethods.MOD_WIN | NativeMethods.MOD_SHIFT,
            NativeMethods.VK_N
        );

        if (!registered)
        {
            System.Diagnostics.Debug.WriteLine("Failed to register hotkey Win+Shift+N");
            return;
        }

        // Hook window procedure to handle hotkey messages
        _newWndProc = new WndProcDelegate(WndProc);
        _oldWndProc = SetWindowLongPtr(_windowHandle, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
        
        System.Diagnostics.Debug.WriteLine("Hotkey registered and window proc hooked!");
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            System.Diagnostics.Debug.WriteLine("Hotkey pressed!");
            _hotkeyCallback?.Invoke();
            return IntPtr.Zero;
        }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    public void UnregisterHotkey()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(_windowHandle, HOTKEY_ID);
        }
    }
}
