using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Domain.Interfaces;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.Infrastructure.TeamSpeak.TS6
{
    /// <summary>
    /// Полностью детерминированный кэш состояния TS6.
    /// Любое изменение входящих данных обновляет словарь серверов или каналов
    /// и вызывает единую переоценку состояния (NotifyChannelUpdated), исключая гонки данных.
    /// </summary>
    public class TS6StateCache
    {
        private readonly Dictionary<int, ClientItem> _serverClients = new();
        private readonly Dictionary<string, string> _channels = new();
        private readonly ChannelInfo _currentChannel = new() { Name = "TeamSpeak Channel", ServerName = "TeamSpeak 6" };
        
        private int _selfClientId = 0;
        private string _selfChannelId = string.Empty;

        public event EventHandler<ChannelChangedEventArgs>? ChannelChanged;
        public event EventHandler<TalkStatusEventArgs>? TalkStatusChanged;
        public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;

        public int SelfClientId 
        { 
            get => _selfClientId;
            set 
            { 
                if (_selfClientId != value) 
                {
                    _selfClientId = value;
                    NotifyChannelUpdated();
                }
            }
        }

        public string SelfChannelId => _selfChannelId;

        public void Clear()
        {
            _serverClients.Clear();
            _channels.Clear();
            _selfClientId = 0;
            _selfChannelId = string.Empty;
            _currentChannel.Name = string.Empty;
            NotifyChannelUpdated();
        }

        public void UpdateSelfChannel(string chId)
        {
            if (_selfChannelId != chId)
            {
                Logger.Info($"TS6 Self Moved Channel: {_selfChannelId} -> {chId}.", "TS6StateCache");
                _selfChannelId = chId;
                
                if (_selfClientId != 0 && _serverClients.TryGetValue(_selfClientId, out var selfClient) && int.TryParse(chId, out int parsedChId))
                {
                    selfClient.ChannelId = parsedChId;
                }
            }

            NotifyChannelUpdated();
        }

        public void UpdateChannelItem(JsonElement item)
        {
            string chId = TS6PayloadParser.ExtractChannelId(item);
            string chName = TS6PayloadParser.ExtractChannelNameFromElement(item);

            if (!string.IsNullOrEmpty(chId) && !string.IsNullOrEmpty(chName))
            {
                _channels[chId] = chName;
            }

            NotifyChannelUpdated();
        }

        public void UpdateClient(JsonElement item)
        {
            int clid = TS6PayloadParser.ExtractClientId(item);
            if (clid == 0) return;

            string nick = TS6PayloadParser.ExtractNicknameFromElement(item);
            string chId = TS6PayloadParser.ExtractChannelId(item);

            if (!_serverClients.TryGetValue(clid, out var client))
            {
                client = new ClientItem
                {
                    ClientId = clid,
                    Nickname = !string.IsNullOrEmpty(nick) ? nick : $"User #{clid}"
                };
                _serverClients[clid] = client;
            }

            if (!string.IsNullOrEmpty(nick) && !nick.StartsWith("User #", StringComparison.Ordinal))
            {
                client.Nickname = nick;
            }

            if (!string.IsNullOrEmpty(chId))
            {
                if (int.TryParse(chId, out int parsedChId))
                {
                    client.ChannelId = parsedChId;
                }

                if (clid == _selfClientId)
                {
                    _selfChannelId = chId;
                }
                else if (chId == _selfChannelId && !string.IsNullOrEmpty(_selfChannelId))
                {
                    client.IsTemporary = false;
                }
            }

            // Статусы микрофона / звука
            if (item.TryGetProperty("isTalking", out var talkProp)) client.IsTalking = talkProp.GetBoolean();
            if (item.TryGetProperty("inputMuted", out var muteProp)) client.IsMicMuted = muteProp.GetBoolean();
            else if (item.TryGetProperty("properties", out var propsMute) && propsMute.TryGetProperty("inputMuted", out var imProp)) client.IsMicMuted = imProp.GetBoolean();

            if (item.TryGetProperty("outputMuted", out var deafProp)) client.IsDeafened = deafProp.GetBoolean();
            else if (item.TryGetProperty("properties", out var propsDeaf) && propsDeaf.TryGetProperty("outputMuted", out var omProp)) client.IsDeafened = omProp.GetBoolean();

            NotifyChannelUpdated();
        }

        public void RemoveClient(int clientId)
        {
            if (clientId == _selfClientId)
            {
                _selfChannelId = "0";
                NotifyChannelUpdated();
            }
            else if (clientId != 0 && _serverClients.Remove(clientId))
            {
                Logger.Info($"TS6 Client Left: {clientId}", "TS6StateCache");
                NotifyChannelUpdated();
            }
        }

        public void HandleTalkStatus(int clientId, int status, bool isWhisper = false)
        {
            bool isTalking = status == 1;

            if (clientId != 0)
            {
                if (!_serverClients.TryGetValue(clientId, out var client))
                {
                    if (isTalking)
                    {
                        _ = int.TryParse(_selfChannelId, out int parsedChId);
                        
                        client = new ClientItem { ClientId = clientId, Nickname = $"User #{clientId}", IsTemporary = true, ChannelId = parsedChId };
                        _serverClients[clientId] = client;
                        Logger.Info($"TS6 Temporary Whisperer {clientId} started talking (isWhisper={isWhisper}).", "TS6StateCache");
                    }
                }

                if (client != null)
                {
                    client.IsTalking = isTalking;
                }

                NotifyChannelUpdated();
                TalkStatusChanged?.Invoke(this, new TalkStatusEventArgs(clientId, isTalking));
            }
        }

        /// <summary>
        /// Главный метод детерминированной сборки состояния для UI.
        /// </summary>
        public void NotifyChannelUpdated()
        {
            // Если у нас канал пуст или равен 0 - отсылаем "отключено"
            if (string.IsNullOrEmpty(_selfChannelId) || _selfChannelId == "0")
            {
                _currentChannel.Name = string.Empty;
                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(false, TeamSpeakClientType.TeamSpeak6.ToString()));
                ChannelChanged?.Invoke(this, new ChannelChangedEventArgs(_currentChannel, new List<ClientItem>()));
                return;
            }

            // Указываем имя текущего канала
            if (_channels.TryGetValue(_selfChannelId, out string? chName))
            {
                _currentChannel.Name = chName;
            }
            else
            {
                _currentChannel.Name = $"Channel #{_selfChannelId}";
            }

            // Сообщаем UI, что соединение с голосом есть
            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(true, TeamSpeakClientType.TeamSpeak6.ToString()));

            // 1. Клиенты нашей комнаты (регулярные участники)
            var roomClients = _serverClients.Values
                .Where(c => c.ChannelId.ToString(System.Globalization.CultureInfo.InvariantCulture) == _selfChannelId)
                .Select(c => { c.IsTemporary = false; return c; })
                .OrderBy(c => c.Nickname, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.ClientId);

            // 2. Внешние говорящие (шепот / глобальный ашаут из других комнат)
            var externalWhisperers = _serverClients.Values
                .Where(c => c.ChannelId.ToString(System.Globalization.CultureInfo.InvariantCulture) != _selfChannelId && c.IsTalking)
                .Select(c => { c.IsTemporary = true; return c; });

            var allClients = roomClients.Concat(externalWhisperers).ToList();

            ChannelChanged?.Invoke(this, new ChannelChangedEventArgs(_currentChannel, allClients));
        }
    }
}
