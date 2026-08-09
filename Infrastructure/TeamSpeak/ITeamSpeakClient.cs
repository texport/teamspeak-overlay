using System;
using System.Collections.Generic;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Domain.Interfaces;

namespace TeamSpeakOverlay.Infrastructure.TeamSpeak
{
    public enum TeamSpeakClientType
    {
        None,
        TeamSpeak3,
        TeamSpeak6
    }

    public interface ITeamSpeakClient : IDisposable
    {
        event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;
        event EventHandler<TalkStatusEventArgs> TalkStatusChanged;
        event EventHandler<ChannelChangedEventArgs> ChannelChanged;
        event EventHandler<VoiceStateSnapshot>? VoiceStateUpdated;
        event EventHandler<PokeEventArgs>? PokeReceived;

        bool IsConnected { get; }
        TeamSpeakClientType ClientType { get; }
        VoiceStateSnapshot CurrentSnapshot { get; }
        void ForceNotifyState();
    }
}
