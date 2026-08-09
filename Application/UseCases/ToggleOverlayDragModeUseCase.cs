using System;

namespace TeamSpeakOverlay.Application.UseCases
{
    public class ToggleOverlayDragModeUseCase
    {
        public bool IsDragModeEnabled { get; private set; }

        public event EventHandler<bool>? DragModeChanged;

        public void SetDragMode(bool enabled)
        {
            if (IsDragModeEnabled != enabled)
            {
                IsDragModeEnabled = enabled;
                DragModeChanged?.Invoke(this, enabled);
            }
        }

        public void ToggleDragMode()
        {
            SetDragMode(!IsDragModeEnabled);
        }
    }
}
