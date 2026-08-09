using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Domain.Interfaces;
using TeamSpeakOverlay.Infrastructure.TeamSpeak.TS6;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.Infrastructure.TeamSpeak
{
    /// <summary>
    /// Главный класс-фасад для работы с TeamSpeak 6. 
    /// Он реализует интерфейс ITeamSpeakClient, чтобы приложение могло работать с ним точно так же, как и с TS3.
    /// Внутри себя он просто связывает вместе ConnectionManager (сеть), MessageHandler (логика) и StateCache (данные).
    /// </summary>
    public class TeamSpeak6Client : ITeamSpeakClient
    {
        private readonly TS6ConnectionManager _connectionManager;
        private readonly TS6StateCache _stateCache;
        private readonly TS6MessageHandler _messageHandler;
        
        private bool _isDisposed;

        public TeamSpeakClientType ClientType => TeamSpeakClientType.TeamSpeak6;
        public bool IsConnected => _connectionManager.IsConnected;

        // Эти события требуются интерфейсом ITeamSpeakClient. 
        // Через них UI получает обновления о том, кто говорит и кто в канале.
        public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;
        public event EventHandler<TalkStatusEventArgs>? TalkStatusChanged;
        public event EventHandler<ChannelChangedEventArgs>? ChannelChanged;
        public event EventHandler<VoiceStateSnapshot>? VoiceStateUpdated;
        public event EventHandler<PokeEventArgs>? PokeReceived;

        public VoiceStateSnapshot CurrentSnapshot => new VoiceStateSnapshot
        {
            State = IsConnected ? TeamSpeakConnectionState.ConnectedInChannel : TeamSpeakConnectionState.Disconnected,
            ServerName = "TeamSpeak 6"
        };

        public TeamSpeak6Client(ISettingsRepository settingsService)
        {
            // Создаем все внутренние компоненты для работы с TS6
            _connectionManager = new TS6ConnectionManager();
            _stateCache = new TS6StateCache();
            _messageHandler = new TS6MessageHandler(_connectionManager, _stateCache, settingsService);

            // Пробрасываем внутренние события из кэша данных наружу, прямо в UI
            _stateCache.ChannelChanged += (s, e) => ChannelChanged?.Invoke(this, e);
            _stateCache.TalkStatusChanged += (s, e) => TalkStatusChanged?.Invoke(this, e);
            _stateCache.ConnectionStatusChanged += (s, e) => ConnectionStatusChanged?.Invoke(this, e);

            // Если соединение потеряно (например, закрыли программу TeamSpeak), мы очищаем кэш
            // и отправляем сигнал "отключено" в UI
            _connectionManager.ConnectionLost += (s, e) => 
            {
                _stateCache.Clear();
                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(false, ClientType.ToString()));
            };
        }

        /// <summary>
        /// Попытка подключиться к TS6.
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            // Пытаемся открыть вебсокет
            bool success = await _connectionManager.ConnectAsync();
            if (success)
            {
                // Если успешно, сразу отправляем запрос на авторизацию
                _messageHandler.SendAuthRequest();
            }
            return success;
        }

        /// <summary>
        /// Ручное отключение (например, если пользователь нажал кнопку или сменил программу).
        /// </summary>
        public void Disconnect()
        {
            Logger.Info("Disconnect requested by TSScanner.", "TS6ClientFacade");
            _connectionManager.Disconnect();
            _stateCache.Clear();
            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(false, ClientType.ToString()));
        }

        public void ForceNotifyState()
        {
            // TS6 state cache updates automatically via WebSocket messages
        }

        /// <summary>
        /// Очистка памяти при удалении объекта.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                Disconnect();
                _connectionManager.Dispose();
                _isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
