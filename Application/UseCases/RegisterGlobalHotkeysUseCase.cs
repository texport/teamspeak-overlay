using System;
using TeamSpeakOverlay.Domain.Interfaces;

namespace TeamSpeakOverlay.Application.UseCases
{
    public class RegisterGlobalHotkeysUseCase
    {
        private readonly IHotkeyService _hotkeyService;

        public event EventHandler? ToggleVisibilityRequested;
        public event EventHandler? ToggleDisplayModeRequested;

        public RegisterGlobalHotkeysUseCase(IHotkeyService hotkeyService)
        {
            _hotkeyService = hotkeyService;
            _hotkeyService.HotkeyToggleOverlayVisibilityPressed += (s, e) => ToggleVisibilityRequested?.Invoke(this, EventArgs.Empty);
            _hotkeyService.HotkeyToggleDisplayModePressed += (s, e) => ToggleDisplayModeRequested?.Invoke(this, EventArgs.Empty);
        }

        public void Initialize(IntPtr windowHandle)
        {
            _hotkeyService.Initialize(windowHandle);
        }

        public void SetHotkeysEnabled(bool enabled)
        {
            if (enabled)
            {
                _hotkeyService.RegisterHotkeys();
            }
            else
            {
                _hotkeyService.UnregisterHotkeys();
            }
        }
    }
}
