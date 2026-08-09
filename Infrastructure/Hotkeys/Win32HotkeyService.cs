using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using TeamSpeakOverlay.Domain.Interfaces;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.Infrastructure.Hotkeys
{
    public class Win32HotkeyService : IHotkeyService
    {
        private const int WM_HOTKEY = 0x0312;

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        private const int HOTKEY_ID_VISIBILITY = 9001;
        private const int HOTKEY_ID_DISPLAYMODE = 9002;

        private const uint VK_O = 0x4F;
        private const uint VK_M = 0x4D;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private IntPtr _hWnd;
        private HwndSource? _hwndSource;
        private bool _isRegistered;

        public event EventHandler? HotkeyToggleOverlayVisibilityPressed;
        public event EventHandler? HotkeyToggleDisplayModePressed;

        public void Initialize(IntPtr windowHandle)
        {
            if (_hWnd != IntPtr.Zero) return;

            _hWnd = windowHandle;
            _hwndSource = HwndSource.FromHwnd(_hWnd);
            _hwndSource?.AddHook(HwndHook);

            RegisterHotkeys();
        }

        public void RegisterHotkeys()
        {
            if (_hWnd == IntPtr.Zero || _isRegistered) return;

            // Ctrl + Shift + O -> Toggle Visibility
            bool okVis = RegisterHotKey(_hWnd, HOTKEY_ID_VISIBILITY, MOD_CONTROL | MOD_SHIFT, VK_O);
            // Ctrl + Shift + M -> Toggle Speaking Only Mode
            bool okMode = RegisterHotKey(_hWnd, HOTKEY_ID_DISPLAYMODE, MOD_CONTROL | MOD_SHIFT, VK_M);

            _isRegistered = true;
            Logger.Info($"Registered Global Hotkeys: Ctrl+Shift+O ({okVis}), Ctrl+Shift+M ({okMode})", "Win32HotkeyService");
        }

        public void UnregisterHotkeys()
        {
            if (_hWnd == IntPtr.Zero || !_isRegistered) return;

            UnregisterHotKey(_hWnd, HOTKEY_ID_VISIBILITY);
            UnregisterHotKey(_hWnd, HOTKEY_ID_DISPLAYMODE);

            _isRegistered = false;
            Logger.Info("Unregistered Global Hotkeys", "Win32HotkeyService");
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_ID_VISIBILITY)
                {
                    HotkeyToggleOverlayVisibilityPressed?.Invoke(this, EventArgs.Empty);
                    handled = true;
                }
                else if (id == HOTKEY_ID_DISPLAYMODE)
                {
                    HotkeyToggleDisplayModePressed?.Invoke(this, EventArgs.Empty);
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterHotkeys();
            _hwndSource?.RemoveHook(HwndHook);
            GC.SuppressFinalize(this);
        }
    }
}
