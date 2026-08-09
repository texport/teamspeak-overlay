using System;

namespace TeamSpeakOverlay.Domain.Entities
{
    public class ToastItem
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string IconType { get; set; } = "Poke"; // Poke, Info, Warning
        public DateTime CreatedAt { get; } = DateTime.Now;
    }
}
