namespace TeamSpeakOverlay.Domain.Entities
{
    /// <summary>
    /// Состояния подключения клиента TeamSpeak в формате конечного автомата (State Machine).
    /// </summary>
    public enum TeamSpeakConnectionState
    {
        /// <summary> Отключен или не находится на голосовом сервере </summary>
        Disconnected,

        /// <summary> Устанавливает WebSocket/Query соединение </summary>
        Connecting,

        /// <summary> Соединение открыто, проходит авторизацию по API ключу </summary>
        Authenticating,

        /// <summary> Авторизован и находится в голосовом канале </summary>
        ConnectedInChannel,

        /// <summary> Соединение разорвано, пытается переподключиться </summary>
        Reconnecting,

        /// <summary> Ошибка соединения или авторизации </summary>
        Error
    }
}
