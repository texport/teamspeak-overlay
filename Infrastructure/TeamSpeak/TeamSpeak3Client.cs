using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Domain.Interfaces;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.Infrastructure.TeamSpeak
{
    public class TeamSpeak3Client : ITeamSpeakClient
    {
        private const string Host = "127.0.0.1";
        private const int Port = 25639;

        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        private readonly Dictionary<int, ClientItem> _channelClients = new();
        private ChannelInfo _currentChannel = new();
        private bool _isDisposed;

        public TeamSpeakClientType ClientType => TeamSpeakClientType.TeamSpeak3;
        public bool IsConnected => _tcpClient != null && _tcpClient.Connected;

        public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;
        public event EventHandler<TalkStatusEventArgs>? TalkStatusChanged;
        public event EventHandler<ChannelChangedEventArgs>? ChannelChanged;
        public event EventHandler<VoiceStateSnapshot>? VoiceStateUpdated;
        public event EventHandler<PokeEventArgs>? PokeReceived;

        public VoiceStateSnapshot CurrentSnapshot => new VoiceStateSnapshot
        {
            State = IsConnected ? TeamSpeakConnectionState.ConnectedInChannel : TeamSpeakConnectionState.Disconnected,
            CurrentChannel = _currentChannel,
            Clients = _channelClients.Values.ToList(),
            ServerName = "TeamSpeak 3"
        };

        public async Task<bool> ConnectAsync()
        {
            try
            {
                DisconnectInternal();

                Logger.Info($"Connecting to TS3 ClientQuery at {Host}:{Port}...", "TS3Client");
                _tcpClient = new TcpClient();
                var connectTask = _tcpClient.ConnectAsync(Host, Port);
                var timeoutTask = Task.Delay(2000);

                if (await Task.WhenAny(connectTask, timeoutTask) != connectTask || !_tcpClient.Connected)
                {
                    Logger.Warn($"TS3 ClientQuery connection timeout or failed on {Host}:{Port}", "TS3Client");
                    DisconnectInternal();
                    return false;
                }

                Logger.Info("TS3 ClientQuery TCP socket connected!", "TS3Client");
                _stream = _tcpClient.GetStream();
                _cts = new CancellationTokenSource();

                // Start reader loop
                _ = Task.Run(() => ReadLoopAsync(_cts.Token));

                // Send setup commands & authenticate if API Key exists
                await Task.Delay(300);

                string apiKey = TryGetTs3ApiKey();
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    Logger.Info($"Authenticating with TS3 ClientQuery API Key...", "TS3Client");
                    await SendCommandAsync($"auth apikey={apiKey}");
                    await Task.Delay(200);
                }

                await SendCommandAsync("use schandlerid=1");
                await Task.Delay(100);
                await SendCommandAsync("clientnotifyregister schandlerid=1 event=any");
                await SendCommandAsync("clientnotifyregister schandlerid=1 event=talkstatuschange");
                await Task.Delay(100);
                await RefreshStateAsync();

                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(true, ClientType.ToString(), _currentChannel.ServerName));
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"TS3 Connect exception", ex, "TS3Client");
                DisconnectInternal();
                return false;
            }
        }

        public static string TryGetTs3ApiKey()
        {
            try
            {
                var settings = AppSettings.Load();
                if (!string.IsNullOrWhiteSpace(settings.TS3ApiKey))
                {
                    return settings.TS3ApiKey.Trim();
                }

                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string iniPath = Path.Combine(appData, "TS3Client", "clientquery.ini");
                if (File.Exists(iniPath))
                {
                    foreach (var line in File.ReadAllLines(iniPath))
                    {
                        if (line.StartsWith("api_key=", StringComparison.OrdinalIgnoreCase))
                        {
                            string key = line.Substring("api_key=".Length).Trim();
                            if (!string.IsNullOrEmpty(key)) return key;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to read TS3 clientquery.ini: {ex.Message}", "TS3Client");
            }
            return string.Empty;
        }

        public Task DisconnectAsync()
        {
            DisconnectInternal();
            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(false, ClientType.ToString()));
            return Task.CompletedTask;
        }

        private void DisconnectInternal()
        {
            _cts?.Cancel();
            _stream?.Dispose();
            _tcpClient?.Dispose();
            _stream = null;
            _tcpClient = null;
            _cts = null;
            _channelClients.Clear();
        }

        private async Task SendCommandAsync(string command)
        {
            if (_stream == null || !IsConnected) return;
            Logger.Debug($"Sending CMD: '{command}'", "TS3Client");
            byte[] bytes = Encoding.UTF8.GetBytes(command + "\n");
            await _stream.WriteAsync(bytes, 0, bytes.Length);
        }

        private async Task ReadLoopAsync(CancellationToken token)
        {
            var buffer = new byte[8192];
            var sb = new StringBuilder();

            try
            {
                while (!token.IsCancellationRequested && _stream != null)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead <= 0) break;

                    string text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    sb.Append(text);

                    string content = sb.ToString();
                    int lineBreakIndex;
                    while ((lineBreakIndex = content.IndexOf('\n')) >= 0)
                    {
                        string line = content.Substring(0, lineBreakIndex).TrimEnd('\r');
                        content = content.Substring(lineBreakIndex + 1);
                        sb.Clear();
                        sb.Append(content);

                        ProcessLine(line);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.Error("TS3 ReadLoop exception", ex, "TS3Client");
            }
            finally
            {
                DisconnectInternal();
                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(false, ClientType.ToString()));
            }
        }

        private readonly Dictionary<string, string> _channels = new();
        private string _selfChannelId = string.Empty;
        private string _selfClid = string.Empty;

        private void ProcessLine(string rawLine)
        {
            if (string.IsNullOrWhiteSpace(rawLine)) return;
            string line = rawLine.Trim();

            Logger.Debug($"RECV: {line}", "TS3Client");

            if (line.StartsWith("notifytalkstatuschange", StringComparison.OrdinalIgnoreCase))
            {
                HandleTalkStatusChange(line);
            }
            else if (line.StartsWith("notifyclientupdated", StringComparison.OrdinalIgnoreCase))
            {
                HandleClientUpdatedEvent(line);
            }
            else if (line.StartsWith("notifyclientmove", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("notifyclientmoved", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("notifycliententerview", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("notifyclientleftview", StringComparison.OrdinalIgnoreCase))
            {
                HandleClientMoveEvent(line);
            }
            else if (line.StartsWith("notifycurrentscchanged", StringComparison.OrdinalIgnoreCase))
            {
                _ = RefreshStateAsync();
            }
            else if (line.StartsWith("notifyconnectstatuschange", StringComparison.OrdinalIgnoreCase))
            {
                HandleConnectStatusChangeEvent(line);
            }
            else if (line.StartsWith("notifyclientpoke", StringComparison.OrdinalIgnoreCase))
            {
                HandleClientPokeEvent(line);
            }
            else if (line.StartsWith("notifytextmessage", StringComparison.OrdinalIgnoreCase))
            {
                HandleTextMessageEvent(line);
            }
            else if (line.Contains("cid=", StringComparison.Ordinal) || line.Contains("channel_name=", StringComparison.Ordinal) || line.Contains("client_nickname=", StringComparison.Ordinal))
            {
                ParseQueryResponse(line);
            }
        }

        private void HandleClientMoveEvent(string line)
        {
            var dict = ParseParams(line);

            if (line.StartsWith("notifyclientleftview", StringComparison.OrdinalIgnoreCase))
            {
                if (dict.TryGetValue("clid", out var clidStr) && int.TryParse(clidStr, out int clid))
                {
                    _channelClients.Remove(clid);
                    Logger.Info($"[TS3Client] Client clid={clid} left view. Removed from channelClients.", "TS3Client");
                    NotifyStateUpdated();
                }
                return;
            }

            if (dict.TryGetValue("clid", out var cIdStr) && int.TryParse(cIdStr, out int clientClid))
            {
                string targetChId = dict.TryGetValue("ctid", out var ctid) ? ctid : (dict.TryGetValue("cid", out var cid) ? cid : string.Empty);

                Logger.Info($"[TS3Client] HandleClientMoveEvent: clid={clientClid}, targetChId='{targetChId}', selfClid='{_selfClid}', selfChId='{_selfChannelId}'", "TS3Client");

                bool isSelfMove = (cIdStr == _selfClid) || string.IsNullOrEmpty(_selfClid);

                if (isSelfMove && !string.IsNullOrEmpty(targetChId) && targetChId != "0")
                {
                    _selfClid = cIdStr;
                    _selfChannelId = targetChId;
                    Logger.Info($"[TS3Client] SELF CLIENT MOVED to new channel cid={targetChId} (selfClid={_selfClid})! Re-querying channel & clients state...", "TS3Client");
                    _ = RefreshStateAsync();
                    return;
                }

                if (_channelClients.TryGetValue(clientClid, out var client))
                {
                    if (int.TryParse(targetChId, out int newChId) && newChId > 0)
                    {
                        client.ChannelId = newChId;
                        if (!string.IsNullOrEmpty(_selfChannelId) && newChId.ToString() != _selfChannelId)
                        {
                            _channelClients.Remove(clientClid);
                            Logger.Info($"[TS3Client] Client clid={clientClid} ('{client.Nickname}') moved outside our room (cid={newChId} vs selfCid={_selfChannelId}). Removed.", "TS3Client");
                        }
                    }
                }
                else if (dict.TryGetValue("client_nickname", out var nick))
                {
                    client = new ClientItem
                    {
                        ClientId = clientClid,
                        Nickname = nick,
                        ChannelId = int.TryParse(targetChId, out int nCh) ? nCh : (int.TryParse(_selfChannelId, out int sCh) ? sCh : 0)
                    };
                    _channelClients[clientClid] = client;
                }

                if (targetChId == _selfChannelId || isSelfMove)
                {
                    _ = RefreshStateAsync();
                }

                NotifyStateUpdated();
            }
        }

        private void HandleClientPokeEvent(string line)
        {
            var dict = ParseParams(line);
            dict.TryGetValue("invokername", out string? invokerName);
            dict.TryGetValue("msg", out string? msg);
            string sender = UnescapeTsString(invokerName ?? "Коллега TS");
            string text = UnescapeTsString(msg ?? "");

            Logger.Info($"Poke received from '{sender}': {text}", "TS3Client");
            PokeReceived?.Invoke(this, new PokeEventArgs(sender, text));
        }

        private void HandleTextMessageEvent(string line)
        {
            var dict = ParseParams(line);
            dict.TryGetValue("invokername", out string? invokerName);
            dict.TryGetValue("msg", out string? msg);
            string sender = UnescapeTsString(invokerName ?? "Сообщение TS");
            string text = UnescapeTsString(msg ?? "");

            Logger.Info($"Text message received from '{sender}': {text}", "TS3Client");
            PokeReceived?.Invoke(this, new PokeEventArgs(sender, text));
        }

        private void HandleConnectStatusChangeEvent(string line)
        {
            var dict = ParseParams(line);
            if (dict.TryGetValue("status", out string? status))
            {
                Logger.Info($"[TS3Client] HandleConnectStatusChangeEvent: status='{status}'", "TS3Client");
                if (status.Equals("disconnected", StringComparison.OrdinalIgnoreCase))
                {
                    _channelClients.Clear();
                    _channels.Clear();
                    _selfChannelId = string.Empty;
                    _selfClid = string.Empty;
                    Logger.Info("[TS3Client] Client disconnected from server. Clearing state and notifying UI.", "TS3Client");
                    ChannelChanged?.Invoke(this, new ChannelChangedEventArgs(
                        new ChannelInfo { Name = string.Empty, ServerName = string.Empty },
                        new List<ClientItem>()
                    ));
                }
                else if (status.Equals("connected", StringComparison.OrdinalIgnoreCase) || status.Equals("connecting", StringComparison.OrdinalIgnoreCase))
                {
                    _selfClid = string.Empty;
                    _selfChannelId = string.Empty;
                    _channelClients.Clear();
                    _channels.Clear();
                    _ = RefreshStateAsync();
                }
            }
        }

        private static string UnescapeTsString(string str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            return str.Replace("\\s", " ")
                      .Replace("\\p", "|")
                      .Replace("\\/", "/")
                      .Replace("\\\\", "\\");
        }

        private void ParseQueryResponse(string line)
        {
            var items = line.Split('|', StringSplitOptions.RemoveEmptyEntries);
            bool updated = false;

            foreach (var item in items)
            {
                var dict = ParseParams(item);

                // 1. Извлечение информации о себе (whoami)
                if (dict.ContainsKey("clid") && dict.ContainsKey("cid") && !dict.ContainsKey("client_database_id") && !dict.ContainsKey("channel_name"))
                {
                    if (dict.TryGetValue("clid", out var selfClid))
                    {
                        _selfClid = selfClid;
                    }

                    if (dict.TryGetValue("cid", out var selfCid))
                    {
                        _selfChannelId = selfCid;
                        updated = true;
                    }
                    else if (dict.TryGetValue("client_channel_id", out var selfChId))
                    {
                        _selfChannelId = selfChId;
                        updated = true;
                    }
                }
                // 2. Список каналов (channellist)
                else if (dict.TryGetValue("cid", out var cid) && dict.TryGetValue("channel_name", out var chName))
                {
                    _channels[cid] = chName;
                    updated = true;
                }
                // 3. Список клиентов (clientlist)
                else if (dict.TryGetValue("clid", out var clidStr) && int.TryParse(clidStr, out int clid) && dict.TryGetValue("client_nickname", out var nick))
                {
                    string clientChId = dict.TryGetValue("cid", out var cChId) ? cChId : (dict.TryGetValue("ctid", out var ctid) ? ctid : _selfChannelId);
                    
                    if (!_channelClients.TryGetValue(clid, out var client))
                    {
                        client = new ClientItem { ClientId = clid };
                        _channelClients[clid] = client;
                    }

                    client.Nickname = nick;
                    if (int.TryParse(clientChId, out int pChId)) client.ChannelId = pChId;
                    
                    bool isMicMuted = dict.TryGetValue("client_input_muted", out var im) && im == "1";
                    if (dict.TryGetValue("client_input_hardware", out var ih) && ih == "0") isMicMuted = true;
                    client.IsMicMuted = isMicMuted;

                    bool isDeafened = dict.TryGetValue("client_output_muted", out var om) && om == "1";
                    if (dict.TryGetValue("client_output_hardware", out var oh) && oh == "0") isDeafened = true;
                    client.IsDeafened = isDeafened;

                    updated = true;
                }
            }

            if (updated)
            {
                NotifyStateUpdated();
            }
        }

        private void HandleClientUpdatedEvent(string line)
        {
            var dict = ParseParams(line);
            if (dict.TryGetValue("clid", out var clidStr) && int.TryParse(clidStr, out int clid))
            {
                if (_channelClients.TryGetValue(clid, out var client))
                {
                    if (dict.TryGetValue("client_input_muted", out var im)) client.IsMicMuted = im == "1";
                    if (dict.TryGetValue("client_input_hardware", out var ih))
                    {
                        if (ih == "0") client.IsMicMuted = true;
                        else if (dict.TryGetValue("client_input_muted", out var im2) && im2 == "0") client.IsMicMuted = false;
                    }

                    if (dict.TryGetValue("client_output_muted", out var om)) client.IsDeafened = om == "1";
                    if (dict.TryGetValue("client_output_hardware", out var oh))
                    {
                        if (oh == "0") client.IsDeafened = true;
                        else if (dict.TryGetValue("client_output_muted", out var om2) && om2 == "0") client.IsDeafened = false;
                    }

                    if (dict.TryGetValue("client_nickname", out var nick)) client.Nickname = nick;

                    Logger.Info($"[TS3Client] HandleClientUpdatedEvent: clid={clid}, Nick='{client.Nickname}', IsMicMuted={client.IsMicMuted}, IsDeafened={client.IsDeafened}", "TS3Client");
                    NotifyStateUpdated();
                }
            }
        }

        public void ForceNotifyState()
        {
            _ = RefreshStateAsync();
            NotifyStateUpdated();
        }

        private void NotifyStateUpdated()
        {
            if (string.IsNullOrEmpty(_selfChannelId) && int.TryParse(_selfClid, out int selfId) && _channelClients.TryGetValue(selfId, out var selfClient) && selfClient.ChannelId > 0)
            {
                _selfChannelId = selfClient.ChannelId.ToString();
            }

            if (string.IsNullOrEmpty(_selfChannelId) && !string.IsNullOrEmpty(_selfClid))
            {
                var match = _channelClients.Values.FirstOrDefault(c => c.ClientId.ToString() == _selfClid);
                if (match != null && match.ChannelId > 0)
                {
                    _selfChannelId = match.ChannelId.ToString();
                }
            }

            if (string.IsNullOrEmpty(_selfChannelId) && _channelClients.Count > 0)
            {
                // Fallback: Use the most common channelId among active channelClients
                var fallbackCid = _channelClients.Values
                    .Where(c => c.ChannelId > 0)
                    .GroupBy(c => c.ChannelId)
                    .OrderByDescending(g => g.Count())
                    .Select(g => (int?)g.Key)
                    .FirstOrDefault();

                if (fallbackCid.HasValue && fallbackCid.Value > 0)
                {
                    _selfChannelId = fallbackCid.Value.ToString();
                    Logger.Info($"NotifyStateUpdated fallback: assigned _selfChannelId={_selfChannelId}", "TS3Client");
                }
            }

            if (string.IsNullOrEmpty(_selfChannelId))
            {
                Logger.Warn($"NotifyStateUpdated skipped: _selfChannelId is empty (selfClid='{_selfClid}', channelClients={_channelClients.Count})", "TS3Client");
                return;
            }

            int selfCidInt = int.TryParse(_selfChannelId, out int cid) ? cid : 0;

            string channelName = _channels.TryGetValue(_selfChannelId, out var chName)
                ? chName
                : $"Channel #{_selfChannelId}";

            var channelSnapshot = new ChannelInfo
            {
                ChannelId = selfCidInt,
                Name = channelName,
                ServerName = "TS3 ClientQuery"
            };

            var clientsList = _channelClients.Values
                .Where(c => c.ChannelId == selfCidInt)
                .OrderBy(c => c.Nickname, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Logger.Info($"TS3 Invoking ChannelChanged: Channel='{channelName}' (cid={selfCidInt}), Clients={clientsList.Count}", "TS3Client");
            ChannelChanged?.Invoke(this, new ChannelChangedEventArgs(channelSnapshot, clientsList));
        }

        private void HandleTalkStatusChange(string line)
        {
            var dict = ParseParams(line);
            if (dict.TryGetValue("status", out string? statusStr) &&
                int.TryParse(statusStr, out int status) &&
                dict.TryGetValue("clid", out string? clidStr) &&
                int.TryParse(clidStr, out int clid))
            {
                bool isTalking = status == 1;

                if (_channelClients.TryGetValue(clid, out var client))
                {
                    client.IsTalking = isTalking;
                }

                Logger.Info($"[TS3Client] HandleTalkStatusChange: clid={clid}, status={status}, isTalking={isTalking}, clientFound={_channelClients.TryGetValue(clid, out var c)} (nick='{c?.Nickname}')", "TS3Client");

                TalkStatusChanged?.Invoke(this, new TalkStatusEventArgs(clid, isTalking));
            }
        }

        private async Task RefreshStateAsync()
        {
            try
            {
                await SendCommandAsync("whoami");
                await Task.Delay(100);
                await SendCommandAsync("channellist");
                await Task.Delay(100);
                await SendCommandAsync("clientlist -voice -away");
            }
            catch (Exception ex)
            {
                Logger.Error("TS3 RefreshState exception", ex, "TS3Client");
            }
        }

        private static Dictionary<string, string> ParseParams(string response)
        {
            var dict = new Dictionary<string, string>();
            var tokens = response.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                int eqIndex = token.IndexOf('=');
                if (eqIndex > 0)
                {
                    string key = token.Substring(0, eqIndex);
                    string val = Unescape(token.Substring(eqIndex + 1));
                    dict[key] = val;
                }
            }

            return dict;
        }

        private static string Unescape(string str)
        {
            return str
                .Replace(@"\s", " ")
                .Replace(@"\p", "|")
                .Replace(@"\n", "\n")
                .Replace(@"\r", "\r")
                .Replace(@"\t", "\t")
                .Replace(@"\v", "\v")
                .Replace(@"\\", "\\");
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                DisconnectInternal();
                _isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}

