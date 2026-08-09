using System;
using System.Text.Json;
using System.Threading.Tasks;
using TeamSpeakOverlay.Domain.Interfaces;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.Infrastructure.TeamSpeak.TS6
{
    /// <summary>
    /// Этот класс отвечает за получение "сырого" JSON от сервера TS6, 
    /// определение типа сообщения и маршрутизацию данных в TS6StateCache.
    /// По сути, это мозг-переводчик между WebSocket и кэшем состояний.
    /// </summary>
    public class TS6MessageHandler
    {
        private readonly TS6ConnectionManager _connection;
        private readonly TS6StateCache _state;
        private readonly ISettingsRepository _settings;

        public TS6MessageHandler(TS6ConnectionManager connection, TS6StateCache state, ISettingsRepository settings)
        {
            _connection = connection;
            _state = state;
            _settings = settings;

            // Подписываемся на событие получения нового JSON-сообщения от вебсокета
            _connection.JsonMessageReceived += OnJsonMessageReceived;
        }

        /// <summary>
        /// Главный метод-обработчик входящих JSON сообщений.
        /// </summary>
        private void OnJsonMessageReceived(object? sender, string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string msgType = string.Empty;

                // Извлекаем тип сообщения, если он есть в корне JSON
                if (root.TryGetProperty("type", out var typeProp))
                {
                    msgType = typeProp.GetString() ?? string.Empty;
                }

                // В некоторых ответах (например, на авторизацию) TS6 не присылает поле type.
                // В таких случаях мы смотрим внутрь payload, чтобы "угадать" тип сообщения.
                if (string.IsNullOrEmpty(msgType) && root.TryGetProperty("payload", out var payload))
                {
                    if (payload.TryGetProperty("apiKey", out _) || payload.TryGetProperty("connections", out _))
                    {
                        msgType = "auth";
                    }
                    else if (payload.TryGetProperty("clientSelfId", out _))
                    {
                        msgType = "clientSelfDetailGet";
                    }
                }

                // Передаем обработку конкретным методам в зависимости от типа
                if (!string.IsNullOrEmpty(msgType))
                {
                    ProcessMessageByType(msgType, root);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TS6 MessageHandler exception", ex, "TS6MessageHandler");
            }
        }

        private void ProcessMessageByType(string msgType, JsonElement root)
        {
            if (msgType == "auth")
            {
                HandleAuth(root); // Авторизация прошла успешно
            }
            else if (msgType == "connectStatusChanged")
            {
                HandleConnectStatusChanged(root); // Статус соединения и наш свой ID (clientId)
            }
            else if (msgType == "clientSelfPropertyUpdated" || msgType == "clientSelfDetailGet")
            {
                HandleClientSelfDetailGet(root); // Запрос/обновление информации о самом себе
            }
            else if (msgType == "talkStatusChanged")
            {
                HandleTalkStatusChanged(root); // Кто-то начал или перестал говорить
            }
            else if (msgType == "clientDisconnect" || msgType == "clientLeftView")
            {
                HandleClientDisconnect(root); // Клиент вышел с сервера или из нашего поля зрения
            }
            else if (msgType == "clientList" || msgType == "clients" || msgType == "clientPropertiesUpdated" || msgType == "clientPropertiesGet" || msgType == "clientEnterView" || msgType == "clientMoved")
            {
                ParseClientsFromPayload(root); // Любые обновления данных о клиентах (перемещения, изменение ника, муты)
            }
            else if (msgType == "channelPropertiesUpdated" || msgType == "channelList" || msgType == "channels" || msgType == "channelCreated")
            {
                ParseChannelFromPayload(root); // Обновления данных о каналах
            }
        }

        /// <summary>
        /// Обрабатывает успешную авторизацию в TS6.
        /// Здесь мы сохраняем apiKey, получаем наш собственный ID (SelfClientId), 
        /// а также вычитываем изначальные списки каналов и клиентов (initial state).
        /// </summary>
        private void HandleAuth(JsonElement root)
        {
            if (root.TryGetProperty("status", out var statusProp))
            {
                int code = 0;
                if (statusProp.ValueKind == JsonValueKind.Object && statusProp.TryGetProperty("code", out var codeProp))
                {
                    code = codeProp.GetInt32();
                }
                else if (statusProp.ValueKind == JsonValueKind.Number)
                {
                    code = statusProp.GetInt32();
                }

                if (code != 0)
                {
                    Logger.Warn($"TS6 Auth rejected with status code {code}. Clearing saved TS6ApiKey so TS6 will prompt for access again...", "TS6MessageHandler");
                    _settings.Settings.TS6ApiKey = string.Empty;
                    _settings.Save();
                    return;
                }
            }

            string newApiKey = string.Empty;
            if (root.TryGetProperty("payload", out var payload))
            {
                // Пытаемся достать apiKey, он может быть в разных местах в зависимости от ответа
                if (payload.TryGetProperty("content", out var content) && content.TryGetProperty("apiKey", out var kProp))
                {
                    newApiKey = kProp.GetString() ?? string.Empty;
                }
                else if (payload.TryGetProperty("apiKey", out var kProp2))
                {
                    newApiKey = kProp2.GetString() ?? string.Empty;
                }
            }

            // Если нам выдали новый API ключ, сохраняем его в настройки на будущее
            if (!string.IsNullOrEmpty(newApiKey) && _settings.Settings.TS6ApiKey != newApiKey)
            {
                Logger.Info($"TS6 Received new API Key: {newApiKey}", "TS6MessageHandler");
                _settings.Settings.TS6ApiKey = newApiKey;
                _settings.Save();
            }

            // Парсинг огромного массива connections, который приходит при авторизации (содержит всё текущее состояние)
            if (root.TryGetProperty("payload", out var authPayload))
            {
                if (authPayload.TryGetProperty("connections", out var connectionsProp) && connectionsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var conn in connectionsProp.EnumerateArray())
                    {
                        // 1. Ищем свой собственный ID
                        int selfClid = TS6PayloadParser.ExtractClientId(conn);
                        if (selfClid != 0)
                        {
                            _state.SelfClientId = selfClid;
                            Logger.Info($"TS6 Extracted Self ClientId: {_state.SelfClientId}", "TS6MessageHandler");
                        }

                        // 2. Вычитываем все каналы (корневые и вложенные)
                        if (conn.TryGetProperty("channelInfos", out var channelsObj) && channelsObj.ValueKind == JsonValueKind.Object)
                        {
                            if (channelsObj.TryGetProperty("rootChannels", out var rootArr) && rootArr.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var chItem in rootArr.EnumerateArray()) _state.UpdateChannelItem(chItem);
                            }
                            if (channelsObj.TryGetProperty("subChannels", out var subObj) && subObj.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var prop in subObj.EnumerateObject())
                                {
                                    if (prop.Value.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var chItem in prop.Value.EnumerateArray()) _state.UpdateChannelItem(chItem);
                                    }
                                }
                            }
                        }
                        else if (conn.TryGetProperty("channelInfos", out var channelsArr) && channelsArr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var chItem in channelsArr.EnumerateArray()) _state.UpdateChannelItem(chItem);
                        }
                        else if (conn.TryGetProperty("channels", out var directChannelsArr) && directChannelsArr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var chItem in directChannelsArr.EnumerateArray()) _state.UpdateChannelItem(chItem);
                        }
                        else if (conn.TryGetProperty("channelList", out var directChannelListArr) && directChannelListArr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var chItem in directChannelListArr.EnumerateArray()) _state.UpdateChannelItem(chItem);
                        }

                        // 3. Вычитываем всех клиентов
                        if (conn.TryGetProperty("clientInfos", out var clientsArr) && clientsArr.ValueKind == JsonValueKind.Array)
                        {
                            // Сначала ищем себя, чтобы понять, в каком мы канале
                            foreach (var clItem in clientsArr.EnumerateArray())
                            {
                                int clid = TS6PayloadParser.ExtractClientId(clItem);
                                if (clid == _state.SelfClientId)
                                {
                                    string chId = TS6PayloadParser.ExtractChannelId(clItem);
                                    if (!string.IsNullOrEmpty(chId))
                                    {
                                        _state.UpdateSelfChannel(chId);
                                    }
                                    break;
                                }
                            }

                            // Теперь загружаем всех остальных
                            foreach (var clItem in clientsArr.EnumerateArray()) _state.UpdateClient(clItem);
                        }
                    }
                    _state.NotifyChannelUpdated();
                }
            }

            // После успешной авторизации подписываемся на обновления сервера, каналов и клиентов
            _ = SubscribeToEventsAsync();
        }

        private static readonly JsonSerializerOptions CamelCaseOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        /// <summary>
        /// Отправляет запросы в TS6 на подписку (чтобы получать уведомления, когда кто-то говорит или перемещается).
        /// </summary>
        private async Task SubscribeToEventsAsync()
        {
            await _connection.SendJsonAsync(JsonSerializer.Serialize(new { type = "subscribe", payload = new { eventName = "channel" } }, CamelCaseOptions));
            await _connection.SendJsonAsync(JsonSerializer.Serialize(new { type = "subscribe", payload = new { eventName = "client" } }, CamelCaseOptions));
            await _connection.SendJsonAsync(JsonSerializer.Serialize(new { type = "subscribe", payload = new { eventName = "server" } }, CamelCaseOptions));
        }

        private void HandleConnectStatusChanged(JsonElement root)
        {
            if (!root.TryGetProperty("payload", out var payload)) return;

            if (payload.TryGetProperty("info", out var infoObj))
            {
                int selfClid = TS6PayloadParser.ExtractClientId(infoObj);
                if (selfClid != 0)
                {
                    Logger.Info($"TS6 connectStatusChanged: SelfClientId={selfClid}", "TS6MessageHandler");
                    _state.SelfClientId = selfClid;
                }
            }

            _state.NotifyChannelUpdated();
        }

        private void HandleClientSelfDetailGet(JsonElement root)
        {
            if (root.TryGetProperty("payload", out var payload))
            {
                int clid = TS6PayloadParser.ExtractClientId(payload);

                if (clid != 0)
                {
                    _state.SelfClientId = clid;
                }

                string chId = TS6PayloadParser.ExtractChannelId(payload);
                if (!string.IsNullOrEmpty(chId))
                {
                    _state.UpdateSelfChannel(chId);
                }
            }
        }

        private void HandleTalkStatusChanged(JsonElement root)
        {
            if (root.TryGetProperty("payload", out var payload))
            {
                int clientId = TS6PayloadParser.ExtractClientId(payload);
                int status = 0;
                if (payload.TryGetProperty("status", out var sProp)) status = sProp.GetInt32();
                bool isWhisper = false;
                if (payload.TryGetProperty("isWhisper", out var wProp)) isWhisper = wProp.GetBoolean();

                _state.HandleTalkStatus(clientId, status, isWhisper);
            }
        }

        private void HandleClientDisconnect(JsonElement root)
        {
            if (root.TryGetProperty("payload", out var payload))
            {
                int clid = TS6PayloadParser.ExtractClientId(payload);
                _state.RemoveClient(clid);
            }
        }

        /// <summary>
        /// Универсальный парсер массивов клиентов. 
        /// Обрабатывает перемещения по комнатам, смену никнеймов и статусы микрофонов/наушников.
        /// </summary>
        private void ParseClientsFromPayload(JsonElement root)
        {
            if (!root.TryGetProperty("payload", out var payload)) return;

            if (payload.ValueKind == JsonValueKind.Array)
            {
                // ПЕРВЫЙ ПРОХОД: находим себя и обновляем свой ID канала на случай, если мы переместились
                foreach (var item in payload.EnumerateArray())
                {
                    int clid = TS6PayloadParser.ExtractClientId(item);
                    if (clid == _state.SelfClientId)
                    {
                        _state.UpdateClient(item);
                        break;
                    }
                }

                // ВТОРОЙ ПРОХОД: обрабатываем всех остальных
                foreach (var item in payload.EnumerateArray())
                {
                    int clid = TS6PayloadParser.ExtractClientId(item);
                    if (clid != _state.SelfClientId)
                    {
                        _state.UpdateClient(item);
                    }
                }
            }
            else if (payload.ValueKind == JsonValueKind.Object)
            {
                _state.UpdateClient(payload);
            }

            _state.NotifyChannelUpdated();
        }

        private void ParseChannelFromPayload(JsonElement root)
        {
            if (!root.TryGetProperty("payload", out var payload)) return;

            if (payload.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in payload.EnumerateArray())
                {
                    _state.UpdateChannelItem(item);
                }
            }
            else if (payload.ValueKind == JsonValueKind.Object)
            {
                _state.UpdateChannelItem(payload);
            }

            _state.NotifyChannelUpdated();
        }

        /// <summary>
        /// Отправляет первоначальный запрос авторизации в TS6.
        /// Вызывается снаружи (из TeamSpeak6Client) сразу после успешного коннекта к WebSocket.
        /// </summary>
        public void SendAuthRequest()
        {
            string apiKey = _settings.Settings.TS6ApiKey ?? string.Empty;
            var authPayload = new
            {
                type = "auth",
                payload = new
                {
                    identifier = "TeamSpeakOverlay",
                    version = "1.0.0",
                    name = "Overlay",
                    description = "TeamSpeak 6 Overlay Tool",
                    content = new
                    {
                        apiKey = apiKey
                    }
                }
            };

            _ = _connection.SendJsonAsync(JsonSerializer.Serialize(authPayload, CamelCaseOptions));
        }
    }
}
