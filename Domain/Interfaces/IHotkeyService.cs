using System;

namespace TeamSpeakOverlay.Domain.Interfaces
{
    public interface IHotkeyService : IDisposable
    {
        event EventHandler HotkeyToggleOverlayVisibilityPressed;
        event EventHandler HotkeyToggleDisplayModePressed;

        void Initialize(IntPtr windowHandle);
        void RegisterHotkeys();
        void UnregisterHotkeys();
    }
}
