using System.Collections.Generic;

namespace TeamSpeakOverlay.Domain.Entities
{
    /// <summary>
    /// Неизменяемый снимок (Snapshot) голосового состояния для UI.
    /// Передается от State Machine в Presentation слой, исключая гонки данных.
    /// </summary>
    public record VoiceStateSnapshot
    {
        public TeamSpeakConnectionState State { get; init; } = TeamSpeakConnectionState.Disconnected;
        public ChannelInfo CurrentChannel { get; init; } = new();
        public IReadOnlyList<ClientItem> Clients { get; init; } = new List<ClientItem>();
        public IReadOnlyList<ClientItem> WhisperClients { get; init; } = new List<ClientItem>();
        public ClientItem? SelfClient { get; init; }
        public string ServerName { get; init; } = string.Empty;

        public static VoiceStateSnapshot Empty => new();
    }
}
