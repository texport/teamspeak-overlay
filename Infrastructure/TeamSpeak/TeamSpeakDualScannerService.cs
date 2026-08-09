using System;
using System.Threading;
using System.Threading.Tasks;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Domain.Interfaces;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.Infrastructure.TeamSpeak
{
    public class TeamSpeakDualScannerService : ITeamSpeakProvider, IDisposable
    {
        private readonly TeamSpeak3Client _ts3Client;
        private readonly TeamSpeak6Client _ts6Client;
        private readonly ISettingsRepository _settingsService;
        private System.Threading.Timer? _reconnectTimer;

        private ITeamSpeakClient? _activeClient;
        private bool _isDisposed;
        private bool _isConnecting;

        public ITeamSpeakClient? ActiveClient => _activeClient;
        public bool IsConnected => _activeClient != null && _activeClient.IsConnected;

        public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;
        public event EventHandler<TalkStatusEventArgs>? TalkStatusChanged;
        public event EventHandler<ChannelChangedEventArgs>? ChannelChanged;
        public event EventHandler<PokeEventArgs>? PokeReceived;

        public TeamSpeakDualScannerService(ISettingsRepository settingsService)
        {
            _settingsService = settingsService;
            _ts3Client = new TeamSpeak3Client();
            _ts6Client = new TeamSpeak6Client(settingsService);

            // Hook up events
            SubscribeEvents(_ts3Client);
            SubscribeEvents(_ts6Client);
            
            Logger.Info("TeamSpeakDualScannerService created", "TSScanner");
        }
        
        public void StartScanning()
        {
            if (_reconnectTimer == null)
            {
                _reconnectTimer = new System.Threading.Timer(OnReconnectTimerTick, null, 0, 4000);
                Logger.Info("TeamSpeakDualScannerService scanner started", "TSScanner");
            }
        }
        
        public void StopScanning()
        {
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;
            
            _activeClient = null;
            _ts3Client.Dispose();
            _ts6Client.Dispose();
        }

        private void SubscribeEvents(ITeamSpeakClient client)
        {
            client.ConnectionStatusChanged += (s, e) =>
            {
                if (s == _activeClient)
                {
                    Logger.Info($"Active client ConnectionStatusChanged: Connected={e.IsConnected}, Type={e.ClientType}", "TSScanner");
                    ConnectionStatusChanged?.Invoke(this, e);
                }
            };
            client.TalkStatusChanged += (s, e) =>
            {
                if (s == _activeClient)
                {
                    TalkStatusChanged?.Invoke(this, e);
                }
            };
            client.ChannelChanged += (s, e) =>
            {
                if (s == _activeClient)
                {
                    ChannelChanged?.Invoke(this, e);
                }
            };
            client.PokeReceived += (s, e) =>
            {
                if (s == _activeClient)
                {
                    Logger.Info($"Active client PokeReceived: Invoker={e.InvokerName}", "TSScanner");
                    PokeReceived?.Invoke(this, e);
                }
            };
        }

        private async void OnReconnectTimerTick(object? state)
        {
            if (_isConnecting || IsConnected) return;

            _isConnecting = true;
            try
            {
                var settings = _settingsService.Settings;
                Logger.Info($"Scanning for active TeamSpeak client (Mode={settings.TeamSpeakMode})...", "TSScanner");
                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(false, TeamSpeakClientType.None.ToString(), "Connecting to TeamSpeak..."));

                var tasks = new System.Collections.Generic.List<Task<bool>>();
                Task<bool>? ts3Task = null;
                Task<bool>? ts6Task = null;

                if (settings.TeamSpeakMode == TeamSpeakConnectionMode.Auto || settings.TeamSpeakMode == TeamSpeakConnectionMode.TeamSpeak3)
                {
                    ts3Task = _ts3Client.ConnectAsync();
                    tasks.Add(ts3Task);
                }

                if (settings.TeamSpeakMode == TeamSpeakConnectionMode.Auto || settings.TeamSpeakMode == TeamSpeakConnectionMode.TeamSpeak6)
                {
                    ts6Task = _ts6Client.ConnectAsync();
                    tasks.Add(ts6Task);
                }

                while (tasks.Count > 0)
                {
                    var completedTask = await Task.WhenAny(tasks);
                    tasks.Remove(completedTask);

                    if (ts6Task != null && completedTask == ts6Task && _ts6Client.IsConnected)
                    {
                        _activeClient = _ts6Client;
                        Logger.Info("Activated TS6 Remote WebSocket Client!", "TSScanner");
                        ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(true, TeamSpeakClientType.TeamSpeak6.ToString(), "TeamSpeak Remote Client"));
                        _activeClient.ForceNotifyState();
                        return;
                    }

                    if (ts3Task != null && completedTask == ts3Task && _ts3Client.IsConnected)
                    {
                        _activeClient = _ts3Client;
                        Logger.Info("Activated TS3 ClientQuery TCP Client!", "TSScanner");
                        ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(true, TeamSpeakClientType.TeamSpeak3.ToString(), "TS3 ClientQuery"));
                        _activeClient.ForceNotifyState();

                        // Fire a second state notification after socket query responses finish processing
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(400);
                            _activeClient?.ForceNotifyState();
                        });
                        return;
                    }
                }

                // Disconnected state
                _activeClient = null;
                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(false, TeamSpeakClientType.None.ToString(), "Waiting for TeamSpeak..."));
            }
            catch (Exception ex)
            {
                Logger.Error("TSScanner exception in reconnect loop", ex, "TSScanner");
                _activeClient = null;
            }
            finally
            {
                _isConnecting = false;
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                StopScanning();
                _isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}





