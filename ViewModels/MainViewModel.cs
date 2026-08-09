using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Threading;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Domain.Interfaces;
using TeamSpeakOverlay.Application.UseCases;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ObserveGameStateUseCase _observeGameUseCase;
        private readonly ObserveTeamSpeakStateUseCase _observeTSUseCase;
        private readonly UpdateSettingsUseCase _updateSettingsUseCase;
        private readonly ApplyVisualPresetUseCase _applyPresetUseCase;
        private readonly Dispatcher _dispatcher;
        private readonly DispatcherTimer _clockTimer;

        private ChannelInfo _currentChannel = new();
        private bool _isGameActive;
        private bool _isTSConnected;
        private string _statusText = "Initializing...";
        private Rectangle _gameWindowRect;
        private string _currentTimeText = DateTime.Now.ToString("HH:mm:ss");

        public ObservableCollection<ClientItem> Clients { get; } = new();
        public ObservableCollection<ClientItem> WhisperClients { get; } = new();
        public ObservableCollection<ToastItem> ToastNotifications { get; } = new();

        public ICollectionView ClientsView { get; }
        public ICollectionView WhisperClientsView { get; }

        public string CurrentTimeText
        {
            get => _currentTimeText;
            private set
            {
                if (_currentTimeText != value)
                {
                    _currentTimeText = value;
                    OnPropertyChanged();
                }
            }
        }

        private string FormatCurrentTime(DateTime dt)
        {
            var format = _updateSettingsUseCase.GetSettings().TimeFormat;
            return format switch
            {
                TimeDisplayFormat.Short24Hour => dt.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                TimeDisplayFormat.TwelveHour => dt.ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture),
                TimeDisplayFormat.TwelveHourWithSeconds => dt.ToString("hh:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture),
                TimeDisplayFormat.Full24HourWithSeconds => dt.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                _ => dt.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
            };
        }

        public TimeDisplayFormat TimeFormat
        {
            get => _updateSettingsUseCase.GetSettings().TimeFormat;
            set
            {
                if (_updateSettingsUseCase.GetSettings().TimeFormat != value)
                {
                    _updateSettingsUseCase.UpdateTimeFormat(value);
                    OnPropertyChanged();
                    CurrentTimeText = FormatCurrentTime(DateTime.Now);
                }
            }
        }

        public bool ShowClockInHeader
        {
            get => _updateSettingsUseCase.GetSettings().ShowClockInHeader;
            set
            {
                if (_updateSettingsUseCase.GetSettings().ShowClockInHeader != value)
                {
                    _updateSettingsUseCase.GetSettings().ShowClockInHeader = value;
                    _updateSettingsUseCase.GetSettings().Save();
                    OnPropertyChanged();
                }
            }
        }

        public bool EnablePokeNotifications
        {
            get => _updateSettingsUseCase.GetSettings().EnablePokeNotifications;
            set
            {
                if (_updateSettingsUseCase.GetSettings().EnablePokeNotifications != value)
                {
                    _updateSettingsUseCase.GetSettings().EnablePokeNotifications = value;
                    _updateSettingsUseCase.GetSettings().Save();
                    OnPropertyChanged();
                }
            }
        }

        public bool EnableHotkeys
        {
            get => _updateSettingsUseCase.GetSettings().EnableHotkeys;
            set
            {
                if (_updateSettingsUseCase.GetSettings().EnableHotkeys != value)
                {
                    _updateSettingsUseCase.GetSettings().EnableHotkeys = value;
                    _updateSettingsUseCase.GetSettings().Save();
                    OnPropertyChanged();
                }
            }
        }

        public bool EnableClickThrough
        {
            get => _updateSettingsUseCase.GetSettings().EnableClickThrough;
            set
            {
                if (_updateSettingsUseCase.GetSettings().EnableClickThrough != value)
                {
                    _updateSettingsUseCase.GetSettings().EnableClickThrough = value;
                    _updateSettingsUseCase.GetSettings().Save();
                    OnPropertyChanged();
                }
            }
        }

        public bool EnableVoiceEqualizerAnimation
        {
            get => _updateSettingsUseCase.GetSettings().EnableVoiceEqualizerAnimation;
            set
            {
                if (_updateSettingsUseCase.GetSettings().EnableVoiceEqualizerAnimation != value)
                {
                    _updateSettingsUseCase.GetSettings().EnableVoiceEqualizerAnimation = value;
                    _updateSettingsUseCase.GetSettings().Save();
                    OnPropertyChanged();
                }
            }
        }

        public bool EnableSoundNotifications
        {
            get => _updateSettingsUseCase.GetSettings().EnableSoundNotifications;
            set
            {
                if (_updateSettingsUseCase.GetSettings().EnableSoundNotifications != value)
                {
                    _updateSettingsUseCase.GetSettings().EnableSoundNotifications = value;
                    _updateSettingsUseCase.GetSettings().Save();
                    OnPropertyChanged();
                }
            }
        }

        public VisualPreset CurrentPreset
        {
            get => _updateSettingsUseCase.GetSettings().CurrentPreset;
            set
            {
                if (_updateSettingsUseCase.GetSettings().CurrentPreset != value)
                {
                    _applyPresetUseCase.ApplyPreset(value);
                    OnPropertyChanged();
                    RefreshSettings();
                }
            }
        }

        public ChannelInfo CurrentChannel
        {
            get => _currentChannel;
            set { if (_currentChannel != value) { _currentChannel = value; OnPropertyChanged(); } }
        }

        public bool IsGameActive
        {
            get => _isGameActive;
            set 
            { 
                if (_isGameActive != value) 
                { 
                    _isGameActive = value; 
                    OnPropertyChanged(); 
                    OnPropertyChanged(nameof(IsOverlayVisible)); 
                    Logger.Info($"MainViewModel IsGameActive = {value}", "ViewModel");
                } 
            }
        }

        public bool IsTSConnected
        {
            get => _isTSConnected;
            set { if (_isTSConnected != value) { _isTSConnected = value; OnPropertyChanged(); } }
        }

        private bool _forceShowTestMode;

        public bool ForceShowTestMode
        {
            get => _forceShowTestMode;
            set
            {
                if (_forceShowTestMode != value)
                {
                    _forceShowTestMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsOverlayVisible));
                    Logger.Info($"ForceShowTestMode changed to: {value}", "ViewModel");
                }
            }
        }

        public bool AlwaysShowOnTop
        {
            get => _updateSettingsUseCase.GetSettings().AlwaysShowOnTop;
            set
            {
                if (_updateSettingsUseCase.GetSettings().AlwaysShowOnTop != value)
                {
                    _updateSettingsUseCase.GetSettings().AlwaysShowOnTop = value;
                    _updateSettingsUseCase.GetSettings().Save();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsOverlayVisible));
                    Logger.Info($"AlwaysShowOnTop changed to: {value}", "ViewModel");
                }
            }
        }

        public bool IsOverlayVisible => IsGameActive || AlwaysShowOnTop || ForceShowTestMode;

        public string StatusText
        {
            get => _statusText;
            set { if (_statusText != value) { _statusText = value; OnPropertyChanged(); } }
        }

        public Rectangle GameWindowRect
        {
            get => _gameWindowRect;
            set { if (_gameWindowRect != value) { _gameWindowRect = value; OnPropertyChanged(); } }
        }

        public double OverlayOpacity
        {
            get => _updateSettingsUseCase.GetSettings().OverlayOpacity;
            set
            {
                if (_updateSettingsUseCase.GetSettings().OverlayOpacity != value)
                {
                    _updateSettingsUseCase.UpdateOpacity(value);
                    OnPropertyChanged();
                }
            }
        }

        public double OverlayScale
        {
            get => _updateSettingsUseCase.GetSettings().OverlayScale;
            set
            {
                if (Math.Abs(_updateSettingsUseCase.GetSettings().OverlayScale - value) > 0.01)
                {
                    _updateSettingsUseCase.UpdateScale(value);
                    OnPropertyChanged();
                    Logger.Info($"OverlayScale changed via UseCase to: {value}", "ViewModel");
                }
            }
        }

        public OverlayPosition Position
        {
            get => _updateSettingsUseCase.GetSettings().Position;
            set
            {
                if (_updateSettingsUseCase.GetSettings().Position != value)
                {
                    _updateSettingsUseCase.UpdatePosition(value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Position));
                    Logger.Info($"OverlayPosition changed to: {value}", "ViewModel");
                }
            }
        }

        public double? CustomX => _updateSettingsUseCase.GetSettings().CustomX;
        public double? CustomY => _updateSettingsUseCase.GetSettings().CustomY;

        public void UpdateCustomPosition(double left, double top)
        {
            _updateSettingsUseCase.UpdateCustomPosition(left, top);
            OnPropertyChanged(nameof(Position));
            OnPropertyChanged(nameof(CustomX));
            OnPropertyChanged(nameof(CustomY));
        }

        public double OverlayWidth
        {
            get => _updateSettingsUseCase.GetSettings().OverlayWidth;
            set
            {
                if (Math.Abs(_updateSettingsUseCase.GetSettings().OverlayWidth - value) > 0.1)
                {
                    var s = _updateSettingsUseCase.GetSettings();
                    s.OverlayWidth = value;
                    s.Save();
                    OnPropertyChanged();
                }
            }
        }

        public int MarginX
        {
            get => _updateSettingsUseCase.GetSettings().MarginX;
            set
            {
                if (_updateSettingsUseCase.GetSettings().MarginX != value)
                {
                    var s = _updateSettingsUseCase.GetSettings();
                    s.MarginX = value;
                    s.Save();
                    OnPropertyChanged();
                }
            }
        }

        public int MarginY
        {
            get => _updateSettingsUseCase.GetSettings().MarginY;
            set
            {
                if (_updateSettingsUseCase.GetSettings().MarginY != value)
                {
                    var s = _updateSettingsUseCase.GetSettings();
                    s.MarginY = value;
                    s.Save();
                    OnPropertyChanged();
                }
            }
        }

        public OverlayDisplayMode DisplayMode
        {
            get => _updateSettingsUseCase.GetSettings().DisplayMode;
            set
            {
                if (_updateSettingsUseCase.GetSettings().DisplayMode != value)
                {
                    var s = _updateSettingsUseCase.GetSettings();
                    s.DisplayMode = value;
                    s.Save();
                    OnPropertyChanged();
                    RefreshChannelLists();
                }
            }
        }

        public OverlaySortOrder SortOrder
        {
            get => _updateSettingsUseCase.GetSettings().SortOrder;
            set
            {
                if (_updateSettingsUseCase.GetSettings().SortOrder != value)
                {
                    var s = _updateSettingsUseCase.GetSettings();
                    s.SortOrder = value;
                    s.Save();
                    OnPropertyChanged();
                    RefreshChannelLists();
                }
            }
        }

        public OverlayCardStyle CardStyle
        {
            get => _updateSettingsUseCase.GetSettings().CardStyle;
            set
            {
                if (_updateSettingsUseCase.GetSettings().CardStyle != value)
                {
                    var s = _updateSettingsUseCase.GetSettings();
                    s.CardStyle = value;
                    s.Save();
                    OnPropertyChanged();
                }
            }
        }

        public SpeechAccentColor TalkingAccentColor
        {
            get => _updateSettingsUseCase.GetSettings().TalkingAccentColor;
            set
            {
                if (_updateSettingsUseCase.GetSettings().TalkingAccentColor != value)
                {
                    var s = _updateSettingsUseCase.GetSettings();
                    s.TalkingAccentColor = value;
                    s.Save();
                    OnPropertyChanged();
                }
            }
        }

        public SpeakingNicknameColorMode SpeakingNickMode
        {
            get => _updateSettingsUseCase.GetSettings().SpeakingNickMode;
            set
            {
                if (_updateSettingsUseCase.GetSettings().SpeakingNickMode != value)
                {
                    var s = _updateSettingsUseCase.GetSettings();
                    s.SpeakingNickMode = value;
                    s.Save();
                    OnPropertyChanged();
                }
            }
        }

        public NicknameTextColor NicknameColor
        {
            get => _updateSettingsUseCase.GetSettings().NicknameColor;
            set
            {
                if (_updateSettingsUseCase.GetSettings().NicknameColor != value)
                {
                    var s = _updateSettingsUseCase.GetSettings();
                    s.NicknameColor = value;
                    s.Save();
                    OnPropertyChanged();
                }
            }
        }

        public WhisperAccentColor WhisperAccentColor
        {
            get => _updateSettingsUseCase.GetSettings().WhisperAccentColor;
            set
            {
                if (_updateSettingsUseCase.GetSettings().WhisperAccentColor != value)
                {
                    var s = _updateSettingsUseCase.GetSettings();
                    s.WhisperAccentColor = value;
                    s.Save();
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowAuthorBranding
        {
            get => _updateSettingsUseCase.GetSettings().ShowAuthorBranding;
            set
            {
                if (_updateSettingsUseCase.GetSettings().ShowAuthorBranding != value)
                {
                    var s = _updateSettingsUseCase.GetSettings();
                    s.ShowAuthorBranding = value;
                    s.Save();
                    OnPropertyChanged();
                }
            }
        }

        public bool UseGameCharacterName
        {
            get => _updateSettingsUseCase.GetSettings().UseGameCharacterName;
            set
            {
                if (_updateSettingsUseCase.GetSettings().UseGameCharacterName != value)
                {
                    var s = _updateSettingsUseCase.GetSettings();
                    s.UseGameCharacterName = value;
                    s.Save();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayedBadgeText));
                    OnPropertyChanged(nameof(DisplayedBadgePrefix));
                    OnPropertyChanged(nameof(IsHeaderBadgeVisible));
                }
            }
        }

        public string AuthorTelegramHandle => _updateSettingsUseCase.GetSettings().AuthorTelegramHandle;

        private readonly GetHeaderBadgeInfoUseCase _getHeaderBadgeUseCase = new();
        private string _lastRawWindowTitle = string.Empty;
        private string _gameCharacterName = string.Empty;

        public HeaderBadgeInfo HeaderBadge => _getHeaderBadgeUseCase.Execute(
            _updateSettingsUseCase.GetSettings(), 
            _gameCharacterName, 
            _lastRawWindowTitle);

        public string DisplayedBadgeText => HeaderBadge.Text;
        public string DisplayedBadgePrefix => HeaderBadge.Prefix;
        public bool IsHeaderBadgeVisible => HeaderBadge.IsVisible;

        public bool ShowHeader
        {
            get => _updateSettingsUseCase.GetSettings().ShowHeader;
            set
            {
                if (_updateSettingsUseCase.GetSettings().ShowHeader != value)
                {
                    var s = _updateSettingsUseCase.GetSettings();
                    s.ShowHeader = value;
                    s.Save();
                    OnPropertyChanged();
                }
            }
        }

        public int MaxVisibleClients
        {
            get => _updateSettingsUseCase.GetSettings().MaxVisibleClients;
            set
            {
                if (_updateSettingsUseCase.GetSettings().MaxVisibleClients != value)
                {
                    var s = _updateSettingsUseCase.GetSettings();
                    s.MaxVisibleClients = value;
                    s.Save();
                    OnPropertyChanged();
                    RefreshChannelLists();
                }
            }
        }

        private bool _isDragModeEnabled;
        public bool IsDragModeEnabled
        {
            get => _isDragModeEnabled;
            set
            {
                if (_isDragModeEnabled != value)
                {
                    _isDragModeEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public void RefreshSettings()
        {
            OnPropertyChanged(nameof(CurrentPreset));
            OnPropertyChanged(nameof(OverlayOpacity));
            OnPropertyChanged(nameof(OverlayScale));
            OnPropertyChanged(nameof(OverlayWidth));
            OnPropertyChanged(nameof(MarginX));
            OnPropertyChanged(nameof(MarginY));
            OnPropertyChanged(nameof(Position));
            OnPropertyChanged(nameof(DisplayMode));
            OnPropertyChanged(nameof(SortOrder));
            OnPropertyChanged(nameof(CardStyle));
            OnPropertyChanged(nameof(TalkingAccentColor));
            OnPropertyChanged(nameof(SpeakingNickMode));
            OnPropertyChanged(nameof(WhisperAccentColor));
            OnPropertyChanged(nameof(ShowHeader));
            OnPropertyChanged(nameof(ShowClockInHeader));
            OnPropertyChanged(nameof(TimeFormat));
            OnPropertyChanged(nameof(EnablePokeNotifications));
            OnPropertyChanged(nameof(EnableHotkeys));
            OnPropertyChanged(nameof(ShowAuthorBranding));
            OnPropertyChanged(nameof(AuthorTelegramHandle));
            OnPropertyChanged(nameof(UseGameCharacterName));
            OnPropertyChanged(nameof(DisplayedBadgeText));
            OnPropertyChanged(nameof(DisplayedBadgePrefix));
            OnPropertyChanged(nameof(IsHeaderBadgeVisible));
            OnPropertyChanged(nameof(EnableClickThrough));
            OnPropertyChanged(nameof(AlwaysShowOnTop));
            OnPropertyChanged(nameof(MaxVisibleClients));
            RefreshChannelLists();
        }

        public void AddToast(string title, string message, string iconType = "Poke")
        {
            if (!EnablePokeNotifications) return;

            _dispatcher.Invoke(() =>
            {
                var toast = new ToastItem { Title = title, Message = message, IconType = iconType };
                ToastNotifications.Add(toast);

                // Play sound notification if enabled
                var soundUseCase = new Application.UseCases.PlaySoundNotificationUseCase();
                soundUseCase.Execute(_updateSettingsUseCase.GetSettings());

                // Auto dismiss after 4 seconds
                var dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
                dismissTimer.Tick += (s, e) =>
                {
                    dismissTimer.Stop();
                    ToastNotifications.Remove(toast);
                };
                dismissTimer.Start();
            });
        }

        public void ToggleVisibilityHotkey()
        {
            _dispatcher.Invoke(() =>
            {
                ForceShowTestMode = !ForceShowTestMode;
                OnPropertyChanged(nameof(IsOverlayVisible));
                string statusText = ForceShowTestMode ? "Оверлей отображается (Тест)" : "Оверлей скрыт";
                AddToast("Горячая клавиша", statusText, "Hotkey");
            });
        }

        public void ToggleDisplayModeHotkey()
        {
            _dispatcher.Invoke(() =>
            {
                DisplayMode = DisplayMode == OverlayDisplayMode.ShowAll ? OverlayDisplayMode.OnlySpeaking : OverlayDisplayMode.ShowAll;
                string modeText = DisplayMode == OverlayDisplayMode.ShowAll ? "Показывать всех участников" : "Только говорящих участников";
                AddToast("Режим отображения", $"Переключено: {modeText}", "Hotkey");
            });
        }

        private ConnectionStatusEventArgs? _lastConnectionEventArgs;

        public MainViewModel(
            ObserveGameStateUseCase observeGameUseCase,
            ObserveTeamSpeakStateUseCase observeTSUseCase,
            UpdateSettingsUseCase updateSettingsUseCase,
            ApplyVisualPresetUseCase applyPresetUseCase)
        {
            _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            _observeGameUseCase = observeGameUseCase;
            _observeTSUseCase = observeTSUseCase;
            _updateSettingsUseCase = updateSettingsUseCase;
            _applyPresetUseCase = applyPresetUseCase;

            ClientsView = CollectionViewSource.GetDefaultView(Clients);
            ClientsView.SortDescriptions.Add(new SortDescription(nameof(ClientItem.Nickname), ListSortDirection.Ascending));

            WhisperClientsView = CollectionViewSource.GetDefaultView(WhisperClients);
            WhisperClientsView.SortDescriptions.Add(new SortDescription(nameof(ClientItem.Nickname), ListSortDirection.Ascending));

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => CurrentTimeText = FormatCurrentTime(DateTime.Now);
            _clockTimer.Start();

            // Load initial settings
            Position = _updateSettingsUseCase.GetSettings().Position;
            OverlayOpacity = _updateSettingsUseCase.GetSettings().OverlayOpacity;

            Logger.Info("MainViewModel initialized", "ViewModel");

            // Wire TS events
            _observeTSUseCase.ConnectionStatusChanged += OnTSConnectionStatusChanged;
            _observeTSUseCase.TalkStatusChanged += OnTSTalkStatusChanged;
            _observeTSUseCase.ChannelChanged += OnTSChannelChanged;
            _observeTSUseCase.PokeReceived += OnTSPokeReceived;
            _observeTSUseCase.Execute();

            // Wire Game Tracker events
            _observeGameUseCase.GameWindowStateChanged += OnGameWindowStateChanged;
            _observeGameUseCase.Execute();
        }

        private void OnTSPokeReceived(object? sender, PokeEventArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                AddToast($"👉 Poke от {e.InvokerName}", e.Message, "Poke");
            });
        }

        private void AddDummyClientsIfEmpty()
        {
            if (Clients.Count == 0)
            {
                CurrentChannel = new ChannelInfo
                {
                    Name = "🔊 [КП] Lineage II Raid Room",
                    ServerName = "TeamSpeak Server"
                };

                Clients.Add(new ClientItem { ClientId = 1, Nickname = "Alex (Leader)", IsTalking = true, IsMicMuted = false, IsDeafened = false });
                Clients.Add(new ClientItem { ClientId = 2, Nickname = "Dmitry (Healer)", IsTalking = false, IsMicMuted = false, IsDeafened = false });
                Clients.Add(new ClientItem { ClientId = 3, Nickname = "Sergey (Tank)", IsTalking = false, IsMicMuted = true, IsDeafened = false });
                Clients.Add(new ClientItem { ClientId = 4, Nickname = "Ivan (AFK)", IsTalking = false, IsMicMuted = true, IsDeafened = true });
            }
        }

        private void OnGameWindowStateChanged(object? sender, GameWindowStateEventArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                IsGameActive = e.IsGameActive;
                if (e.IsGameActive)
                {
                    GameWindowRect = e.GameWindowRect;
                    _lastRawWindowTitle = e.GameName;
                    var charName = GetHeaderBadgeInfoUseCase.ExtractCharacterNameFromTitle(e.GameName);
                    Logger.Info($"[MainViewModel] GameWindowStateChanged! Active=True, RawTitle='{e.GameName}', ExtractedChar='{charName}'", "MainViewModel");
                    if (_gameCharacterName != charName)
                    {
                        _gameCharacterName = charName;
                        OnPropertyChanged(nameof(DisplayedBadgeText));
                        OnPropertyChanged(nameof(DisplayedBadgePrefix));
                        OnPropertyChanged(nameof(IsHeaderBadgeVisible));
                    }
                }
            });
        }

        private static string ExtractCharacterNameFromTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;

            var prefixes = new[] { 
                "LU4 - ", "LU4 : ", "LU4 ", "L2 - ", "L2 : ", "L2 ",
                "Lineage II - ", "Lineage 2 - ", "LineageII - ", "Lineage2 - ",
                "Lineage II : ", "Lineage 2 : ", "Lineage II ", "Lineage 2 " 
            };

            foreach (var p in prefixes)
            {
                if (title.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                {
                    var candidate = title.Substring(p.Length).Trim('[', ']', ' ', '-', ':');
                    if (!string.IsNullOrWhiteSpace(candidate) && !IsClientExecutableKeyword(candidate))
                        return candidate;
                }
            }

            var parts = title.Split(new[] { '-', ':', ']', '[', '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                var last = parts[^1].Trim();
                var first = parts[0].Trim();

                if (!IsClientExecutableKeyword(last))
                {
                    return last;
                }
                if (!IsClientExecutableKeyword(first))
                {
                    return first;
                }
            }

            return IsClientExecutableKeyword(title) ? string.Empty : title.Trim();
        }

        private static bool IsClientExecutableKeyword(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return true;
            var w = word.Trim().ToLowerInvariant();
            return w is "lu4" or "l2" or "lineage" or "lineage2" or "lineage ii" or "lineageii" or "client" or "game";
        }

        private void OnTSConnectionStatusChanged(object? sender, ConnectionStatusEventArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                _lastConnectionEventArgs = e;
                IsTSConnected = e.IsConnected;
                UpdateStatusText(e);

                if (!e.IsConnected && e.ClientType == "None")
                {
                    Clients.Clear();
                    WhisperClients.Clear();
                    CurrentChannel = new ChannelInfo();
                }

                Logger.Info($"TS Connection Status: Connected={e.IsConnected}, ClientType={e.ClientType}, ServerName={e.ServerName}", "ViewModel");
            });
        }

        private void UpdateStatusText(ConnectionStatusEventArgs e)
        {
            if (e.IsConnected)
            {
                var connStr = System.Windows.Application.Current?.TryFindResource("Status_Connected") as string ?? "Connected";
                StatusText = string.IsNullOrEmpty(e.ServerName) ? $"{connStr} ({e.ClientType})" : e.ServerName;
            }
            else
            {
                var searchStr = System.Windows.Application.Current?.TryFindResource("Status_Searching") as string ?? "Searching TS3 / TS6...";
                StatusText = string.IsNullOrEmpty(e.ServerName) ? searchStr : e.ServerName;
            }
        }

        private void OnTSTalkStatusChanged(object? sender, TalkStatusEventArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                var rawClient = _latestRawClients.FirstOrDefault(c => c.ClientId == e.ClientId);
                if (rawClient != null)
                {
                    rawClient.IsTalking = e.IsTalking;
                }

                var client = Clients.FirstOrDefault(c => c.ClientId == e.ClientId) ?? WhisperClients.FirstOrDefault(c => c.ClientId == e.ClientId);
                if (client != null)
                {
                    client.IsTalking = e.IsTalking;
                }

                Logger.Info($"[UI-Log] OnTSTalkStatusChanged: ClientId={e.ClientId}, IsTalking={e.IsTalking}, FoundClient='{client?.Nickname}'", "ViewModel");

                RefreshChannelLists();
                
                try
                {
                    ClientsView?.Refresh();
                    WhisperClientsView?.Refresh();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error refreshing ClientsView", ex, "ViewModel");
                }
            });
        }

        private System.Collections.Generic.List<ClientItem> _latestRawClients = new();

        private void RefreshChannelLists()
        {
            if (_latestRawClients == null) return;

            var settings = _updateSettingsUseCase.GetSettings();

            // Dynamic View Sorting
            if (ClientsView != null)
            {
                using (ClientsView.DeferRefresh())
                {
                    ClientsView.SortDescriptions.Clear();
                    if (settings.SortOrder == OverlaySortOrder.SpeakersFirst)
                    {
                        ClientsView.SortDescriptions.Add(new SortDescription(nameof(ClientItem.IsTalking), ListSortDirection.Descending));
                    }
                    ClientsView.SortDescriptions.Add(new SortDescription(nameof(ClientItem.Nickname), ListSortDirection.Ascending));
                }
            }

            var regularSource = _latestRawClients.Where(c => !c.IsTemporary).AsEnumerable();
            var whisperSource = _latestRawClients.Where(c => c.IsTemporary).AsEnumerable();

            // 1. Фильтрация по OnlySpeaking
            if (settings.DisplayMode == OverlayDisplayMode.OnlySpeaking)
            {
                regularSource = regularSource.Where(c => c.IsTalking);
            }

            // 2. Сортировка по SpeakersFirst или Alphabetical
            if (settings.SortOrder == OverlaySortOrder.SpeakersFirst)
            {
                regularSource = regularSource.OrderByDescending(c => c.IsTalking).ThenBy(c => c.Nickname, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                regularSource = regularSource.OrderBy(c => c.Nickname, StringComparer.OrdinalIgnoreCase);
            }

            // 3. Лимит видимых клиентов
            if (settings.MaxVisibleClients > 0)
            {
                regularSource = regularSource.Take(settings.MaxVisibleClients);
            }

            var regList = regularSource.ToList();
            var whispList = whisperSource.ToList();

            SyncCollection(Clients, regList);
            SyncCollection(WhisperClients, whispList);

            Logger.Info($"[UI-Log] RefreshChannelLists: RawCount={_latestRawClients.Count}, RegOutputCount={Clients.Count}, WhispOutputCount={WhisperClients.Count}, Mode={settings.DisplayMode}", "ViewModel");
        }

        private void OnTSChannelChanged(object? sender, ChannelChangedEventArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                int rawCount = e.Clients != null ? System.Linq.Enumerable.Count(e.Clients) : 0;
                Logger.Info($"[UI-Log] OnTSChannelChanged received: Channel='{e.Channel?.Name}' (cid={e.Channel?.ChannelId}), RawClientsCount={rawCount}", "ViewModel");
                CurrentChannel = new ChannelInfo
                {
                    ChannelId = e.Channel?.ChannelId ?? 0,
                    Name = e.Channel?.Name ?? "No Channel",
                    ServerName = e.Channel?.ServerName ?? "TS3 ClientQuery"
                };
                OnPropertyChanged(nameof(CurrentChannel));
                _latestRawClients = e.Clients != null ? e.Clients.ToList() : new System.Collections.Generic.List<ClientItem>();
                RefreshChannelLists();

                Logger.Info($"[UI-Log] CurrentChannel updated to '{CurrentChannel.Name}', Clients.Count={Clients.Count}, WhisperClients.Count={WhisperClients.Count}", "ViewModel");
            });
        }

        private static void SyncCollection(ObservableCollection<ClientItem> target, System.Collections.Generic.List<ClientItem> source)
        {
            for (int i = target.Count - 1; i >= 0; i--)
            {
                if (!source.Exists(s => s.ClientId == target[i].ClientId))
                {
                    target.RemoveAt(i);
                }
            }

            for (int i = 0; i < source.Count; i++)
            {
                var srcItem = source[i];
                var existing = target.FirstOrDefault(t => t.ClientId == srcItem.ClientId);
                if (existing != null)
                {
                    existing.Nickname = srcItem.Nickname;
                    existing.IsTalking = srcItem.IsTalking;
                    existing.IsMicMuted = srcItem.IsMicMuted;
                    existing.IsDeafened = srcItem.IsDeafened;
                    existing.ChannelId = srcItem.ChannelId;
                    existing.IsTemporary = srcItem.IsTemporary;
                }
                else
                {
                    target.Insert(Math.Min(i, target.Count), srcItem);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


