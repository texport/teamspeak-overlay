using System;
using TeamSpeakOverlay.Domain.Entities;

namespace TeamSpeakOverlay.Domain.Interfaces
{
    /// <summary>
    /// Интерфейс автомата состояний TeamSpeak.
    /// </summary>
    public interface ITeamSpeakStateMachine
    {
        TeamSpeakConnectionState CurrentState { get; }
        VoiceStateSnapshot CurrentSnapshot { get; }

        event EventHandler<TeamSpeakConnectionState>? StateChanged;
        event EventHandler<VoiceStateSnapshot>? VoiceStateUpdated;

        void TransitionTo(TeamSpeakConnectionState newState);
        void UpdateVoiceSnapshot(VoiceStateSnapshot snapshot);
    }
}
