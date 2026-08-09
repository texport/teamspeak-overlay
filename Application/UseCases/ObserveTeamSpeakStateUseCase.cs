using System;
using System.Collections.Generic;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Domain.Interfaces;

namespace TeamSpeakOverlay.Application.UseCases
{
    /// <summary>
    /// UseCase для отслеживания состояния голосовой связи TeamSpeak.
    /// Передает чистые Snapshot модели в Presentation слой.
    /// </summary>
    public class ObserveTeamSpeakStateUseCase
    {
        private readonly ITeamSpeakProvider _tsProvider;

        public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;
        public event EventHandler<TalkStatusEventArgs>? TalkStatusChanged;
        public event EventHandler<ChannelChangedEventArgs>? ChannelChanged;
        public event EventHandler<VoiceStateSnapshot>? VoiceStateUpdated;
        public event EventHandler<PokeEventArgs>? PokeReceived;

        public ObserveTeamSpeakStateUseCase(ITeamSpeakProvider tsProvider)
        {
            _tsProvider = tsProvider;
            
            _tsProvider.ConnectionStatusChanged += (s, e) => ConnectionStatusChanged?.Invoke(this, e);
            _tsProvider.TalkStatusChanged += (s, e) => TalkStatusChanged?.Invoke(this, e);
            _tsProvider.ChannelChanged += (s, e) => ChannelChanged?.Invoke(this, e);
            _tsProvider.PokeReceived += (s, e) => PokeReceived?.Invoke(this, e);
        }

        public void Execute()
        {
            _tsProvider.StartScanning();
        }

        public void Stop()
        {
            _tsProvider.StopScanning();
        }
    }
}
