using System;
using System.Collections.Generic;

namespace TeamSpeakOverlay.Domain.Interfaces
{
    public interface ITeamSpeakProvider
    {
        event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;
        event EventHandler<TalkStatusEventArgs> TalkStatusChanged;
        event EventHandler<ChannelChangedEventArgs> ChannelChanged;
        event EventHandler<PokeEventArgs> PokeReceived;

        bool IsConnected { get; }
        
        /// <summary>
        /// Attempts to connect to the TeamSpeak client.
        /// </summary>
        void StartScanning();
        
        /// <summary>
        /// Disconnects from the TeamSpeak client and stops scanning.
        /// </summary>
        void StopScanning();
    }

    public class ConnectionStatusEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string ClientType { get; }
        public string ServerName { get; }

        public ConnectionStatusEventArgs(bool isConnected, string clientType, string serverName = "")
        {
            IsConnected = isConnected;
            ClientType = clientType;
            ServerName = serverName;
        }
    }

    public class TalkStatusEventArgs : EventArgs
    {
        public int ClientId { get; }
        public bool IsTalking { get; }

        public TalkStatusEventArgs(int clientId, bool isTalking)
        {
            ClientId = clientId;
            IsTalking = isTalking;
        }
    }

    public class ChannelChangedEventArgs : EventArgs
    {
        public Entities.ChannelInfo Channel { get; }
        public IEnumerable<Entities.ClientItem> Clients { get; }

        public ChannelChangedEventArgs(Entities.ChannelInfo channel, IEnumerable<Entities.ClientItem> clients)
        {
            Channel = channel;
            Clients = clients;
        }
    }

    public class PokeEventArgs : EventArgs
    {
        public string InvokerName { get; }
        public string Message { get; }

        public PokeEventArgs(string invokerName, string message)
        {
            InvokerName = invokerName;
            Message = message;
        }
    }
}
