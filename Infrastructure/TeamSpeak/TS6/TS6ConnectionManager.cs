using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.Infrastructure.TeamSpeak.TS6
{
    /// <summary>
    /// Этот класс управляет исключительно подключением к WebSocket-серверу TeamSpeak 6.
    /// Он отвечает за коннект, дисконнект, поддержку жизни соединения (poll) 
    /// и чтение/запись сырых байтов (сообщений) из/в сокет.
    /// Никакой логики парсинга данных тут нет, только работа с сетью.
    /// </summary>
    public class TS6ConnectionManager : IDisposable
    {
        // Стандартный адрес и порт для Remote API в TeamSpeak 6
        private const string WsUrl = "ws://127.0.0.1:5899";

        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cts;
        private readonly System.Threading.Timer _pollTimer;
        
        // Событие, которое генерируется при получении любой текстовой строчки (JSON) из сокета
        public event EventHandler<string>? JsonMessageReceived;
        // Событие, которое генерируется при потере соединения
        public event EventHandler? ConnectionLost;

        public bool IsConnected => _webSocket != null && _webSocket.State == WebSocketState.Open;

        public TS6ConnectionManager()
        {
            // Таймер для периодической проверки состояния сокета
            _pollTimer = new System.Threading.Timer(OnPollTimerTick, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Попытка подключиться к WebSocket серверу TS6.
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                Disconnect(); // Сбрасываем старое соединение, если оно было

                Logger.Info($"Connecting to TS6 Remote WebSocket API at {WsUrl}...", "TS6ConnectionManager");
                _webSocket = new ClientWebSocket();
                _cts = new CancellationTokenSource();

                var uri = new Uri(WsUrl);
                var connectTask = _webSocket.ConnectAsync(uri, _cts.Token);
                var timeoutTask = Task.Delay(1500); // Таймаут 1.5 секунды на подключение

                // Если таймаут сработал быстрее, чем установилось соединение
                if (await Task.WhenAny(connectTask, timeoutTask) != connectTask || _webSocket.State != WebSocketState.Open)
                {
                    Logger.Info("TS6 Connection failed or timed out.", "TS6ConnectionManager");
                    Disconnect();
                    return false;
                }

                Logger.Info("TS6 WebSocket Connected!", "TS6ConnectionManager");
                
                // Запускаем таймер проверки соединения каждые 5 секунд
                _pollTimer.Change(0, 5000);
                
                // Запускаем бесконечный цикл чтения данных в фоне
                _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("TS6 Connection exception", ex, "TS6ConnectionManager");
                Disconnect();
                return false;
            }
        }

        /// <summary>
        /// Корректно закрывает соединение и освобождает ресурсы.
        /// </summary>
        public void Disconnect()
        {
            _cts?.Cancel();
            _pollTimer.Change(Timeout.Infinite, Timeout.Infinite);

            if (_webSocket != null)
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        // Пытаемся послать серверу сообщение о нормальном закрытии
                        _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait(500);
                    }
                    catch { }
                }
                _webSocket.Dispose();
                _webSocket = null;
            }

            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>
        /// Отправляет JSON строку на сервер.
        /// </summary>
        public async Task SendJsonAsync(string json)
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        /// <summary>
        /// Периодическая проверка (каждые 5 сек), чтобы убедиться, что сокет еще жив.
        /// </summary>
        private void OnPollTimerTick(object? state)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                Logger.Info("TS6 connection lost during polling.", "TS6ConnectionManager");
                Disconnect();
                ConnectionLost?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Бесконечный цикл, который читает байты из вебсокета и склеивает их в строки JSON.
        /// </summary>
        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            var buffer = new byte[8192];
            var ms = new MemoryStream();

            try
            {
                while (!token.IsCancellationRequested && _webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    ms.SetLength(0); // Очищаем поток для нового сообщения
                    WebSocketReceiveResult result;
                    do
                    {
                        // Читаем порцию данных
                        result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Logger.Info("TS6 WebSocket closed by remote server", "TS6ConnectionManager");
                            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, token);
                            return; // Выходим из цикла, сервер сам закрыл соединение
                        }
                        // Записываем порцию в память
                        ms.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage); // Если сообщение большое, склеиваем несколько порций

                    // Превращаем байты в строку UTF-8
                    ms.Seek(0, SeekOrigin.Begin);
                    string jsonMessage = Encoding.UTF8.GetString(ms.ToArray());
                    
                    try { File.AppendAllText("ts6_debug.log", jsonMessage + Environment.NewLine); } catch { }

                    // Передаем JSON дальше в обработчик (TS6MessageHandler)
                    JsonMessageReceived?.Invoke(this, jsonMessage);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.Error("TS6 ReceiveLoop exception", ex, "TS6ConnectionManager");
            }
            finally
            {
                // Если мы вышли из цикла (ошибка или закрытие), нужно сообщить системе, что связь потеряна
                Disconnect();
                ConnectionLost?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            Disconnect();
            _pollTimer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
