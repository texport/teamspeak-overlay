using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Domain.Interfaces;
using TeamSpeakOverlay.Application.UseCases;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private AppSettings _settings;
        private Action _onSaveCallback;
        private Action _onCloseCallback;
        private Action? _onApplyImmediate;

        public SettingsViewModel(AppSettings settings, Action onSaveCallback, Action onCloseCallback, Action? onApplyImmediate = null)
        {
            _settings = settings;
            _onSaveCallback = onSaveCallback;
            _onCloseCallback = onCloseCallback;
            _onApplyImmediate = onApplyImmediate;

            // Load settings into ViewModel properties
            _overlayOpacity = _settings.OverlayOpacity;
            _overlayScale = _settings.OverlayScale;
            _overlayWidth = _settings.OverlayWidth;
            _marginX = _settings.MarginX;
            _marginY = _settings.MarginY;
            _displayMode = _settings.DisplayMode;
            _sortOrder = _settings.SortOrder;
            _cardStyle = _settings.CardStyle;
            _talkingAccentColor = _settings.TalkingAccentColor;
            _nicknameColor = _settings.NicknameColor;
            _speakingNickMode = _settings.SpeakingNickMode;
            _whisperAccentColor = _settings.WhisperAccentColor;
            _showHeader = _settings.ShowHeader;
            _enableHotkeys = _settings.EnableHotkeys;
            _showClockInHeader = _settings.ShowClockInHeader;
            _enablePokeNotifications = _settings.EnablePokeNotifications;
            _showAuthorBranding = _settings.ShowAuthorBranding;
            _enableSoundNotifications = _settings.EnableSoundNotifications;
            _useGameCharacterName = _settings.UseGameCharacterName;
            _enableClickThrough = _settings.EnableClickThrough;
            _alwaysShowOnTop = _settings.AlwaysShowOnTop;
            _enableVoiceEqualizerAnimation = _settings.EnableVoiceEqualizerAnimation;
            _maxVisibleClients = _settings.MaxVisibleClients;
            _timeFormat = _settings.TimeFormat;
            _currentPreset = _settings.CurrentPreset;
            _autostartWithWindows = _settings.AutostartWithWindows;
            _ts3ApiKey = string.IsNullOrWhiteSpace(_settings.TS3ApiKey) ? TeamSpeakOverlay.Infrastructure.TeamSpeak.TeamSpeak3Client.TryGetTs3ApiKey() : _settings.TS3ApiKey;
            _ts6ApiKey = _settings.TS6ApiKey;
            _teamSpeakMode = _settings.TeamSpeakMode;
            _position = _settings.Position;
            _theme = _settings.Theme;
            _language = _settings.Language;
            TargetProcesses = new ObservableCollection<string>(_settings.TargetProcesses);

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            AddProcessCommand = new RelayCommand(AddProcess);
            RemoveProcessCommand = new RelayCommand(RemoveProcess, CanRemoveProcess);
            OpenTelegramCommand = new RelayCommand(OpenTelegram);
        }

        public ICommand OpenTelegramCommand { get; }

        private void OpenTelegram(object? obj)
        {
            var useCase = new OpenTelegramLinkUseCase();
            useCase.Execute(AuthorTelegramHandle);
        }

        private double _overlayOpacity;
        public double OverlayOpacity
        {
            get => _overlayOpacity;
            set
            {
                if (Math.Abs(_overlayOpacity - value) > 0.01)
                {
                    _overlayOpacity = value;
                    OnPropertyChanged();
                    
                    // Apply live
                    _settings.OverlayOpacity = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private double _overlayScale = 1.0;
        public double OverlayScale
        {
            get => _overlayScale;
            set
            {
                if (Math.Abs(_overlayScale - value) > 0.01)
                {
                    _overlayScale = value;
                    OnPropertyChanged();
                    _settings.OverlayScale = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private double _overlayWidth = 280;
        public double OverlayWidth
        {
            get => _overlayWidth;
            set
            {
                if (Math.Abs(_overlayWidth - value) > 0.5)
                {
                    _overlayWidth = value;
                    OnPropertyChanged();
                    _settings.OverlayWidth = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private int _marginX = 20;
        public int MarginX
        {
            get => _marginX;
            set
            {
                if (_marginX != value)
                {
                    _marginX = value;
                    OnPropertyChanged();
                    _settings.MarginX = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private int _marginY = 60;
        public int MarginY
        {
            get => _marginY;
            set
            {
                if (_marginY != value)
                {
                    _marginY = value;
                    OnPropertyChanged();
                    _settings.MarginY = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private OverlayDisplayMode _displayMode;
        public OverlayDisplayMode DisplayMode
        {
            get => _displayMode;
            set
            {
                if (_displayMode != value)
                {
                    _displayMode = value;
                    OnPropertyChanged();
                    _settings.DisplayMode = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private OverlaySortOrder _sortOrder;
        public OverlaySortOrder SortOrder
        {
            get => _sortOrder;
            set
            {
                if (_sortOrder != value)
                {
                    _sortOrder = value;
                    OnPropertyChanged();
                    _settings.SortOrder = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private OverlayCardStyle _cardStyle;
        public OverlayCardStyle CardStyle
        {
            get => _cardStyle;
            set
            {
                if (_cardStyle != value)
                {
                    _cardStyle = value;
                    OnPropertyChanged();
                    _settings.CardStyle = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private SpeechAccentColor _talkingAccentColor;
        public SpeechAccentColor TalkingAccentColor
        {
            get => _talkingAccentColor;
            set
            {
                if (_talkingAccentColor != value)
                {
                    _talkingAccentColor = value;
                    OnPropertyChanged();
                    _settings.TalkingAccentColor = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private NicknameTextColor _nicknameColor;
        public NicknameTextColor NicknameColor
        {
            get => _nicknameColor;
            set
            {
                if (_nicknameColor != value)
                {
                    _nicknameColor = value;
                    OnPropertyChanged();
                    _settings.NicknameColor = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        public Array NicknameColorValues => Enum.GetValues(typeof(NicknameTextColor));

        private SpeakingNicknameColorMode _speakingNickMode;
        public SpeakingNicknameColorMode SpeakingNickMode
        {
            get => _speakingNickMode;
            set
            {
                if (_speakingNickMode != value)
                {
                    _speakingNickMode = value;
                    OnPropertyChanged();
                    _settings.SpeakingNickMode = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        public Array SpeakingNickModeValues => Enum.GetValues(typeof(SpeakingNicknameColorMode));

        private bool _showHeader = true;
        public bool ShowHeader
        {
            get => _showHeader;
            set
            {
                if (_showHeader != value)
                {
                    _showHeader = value;
                    OnPropertyChanged();
                    _settings.ShowHeader = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private bool _enableHotkeys = true;
        public bool EnableHotkeys
        {
            get => _enableHotkeys;
            set
            {
                if (_enableHotkeys != value)
                {
                    _enableHotkeys = value;
                    OnPropertyChanged();
                    _settings.EnableHotkeys = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private bool _useGameCharacterName = false;
        public bool UseGameCharacterName
        {
            get => _useGameCharacterName;
            set
            {
                if (_useGameCharacterName != value)
                {
                    _useGameCharacterName = value;
                    OnPropertyChanged();
                    _settings.UseGameCharacterName = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private bool _showClockInHeader = true;
        public bool ShowClockInHeader
        {
            get => _showClockInHeader;
            set
            {
                if (_showClockInHeader != value)
                {
                    _showClockInHeader = value;
                    OnPropertyChanged();
                    _settings.ShowClockInHeader = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private TimeDisplayFormat _timeFormat = TimeDisplayFormat.Full24HourWithSeconds;
        public TimeDisplayFormat TimeFormat
        {
            get => _timeFormat;
            set
            {
                if (_timeFormat != value)
                {
                    _timeFormat = value;
                    OnPropertyChanged();
                    _settings.TimeFormat = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        public Array TimeFormatValues => Enum.GetValues(typeof(TimeDisplayFormat));

        private bool _enablePokeNotifications = true;
        public bool EnablePokeNotifications
        {
            get => _enablePokeNotifications;
            set
            {
                if (_enablePokeNotifications != value)
                {
                    _enablePokeNotifications = value;
                    OnPropertyChanged();
                    _settings.EnablePokeNotifications = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private bool _enableSoundNotifications = true;
        public bool EnableSoundNotifications
        {
            get => _enableSoundNotifications;
            set
            {
                if (_enableSoundNotifications != value)
                {
                    _enableSoundNotifications = value;
                    OnPropertyChanged();
                    _settings.EnableSoundNotifications = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private bool _enableClickThrough = false;
        public bool EnableClickThrough
        {
            get => _enableClickThrough;
            set
            {
                if (_enableClickThrough != value)
                {
                    _enableClickThrough = value;
                    OnPropertyChanged();
                    _settings.EnableClickThrough = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private bool _alwaysShowOnTop;
        public bool AlwaysShowOnTop
        {
            get => _alwaysShowOnTop;
            set
            {
                if (_alwaysShowOnTop != value)
                {
                    _alwaysShowOnTop = value;
                    OnPropertyChanged();
                    _settings.AlwaysShowOnTop = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private bool _enableVoiceEqualizerAnimation = true;
        public bool EnableVoiceEqualizerAnimation
        {
            get => _enableVoiceEqualizerAnimation;
            set
            {
                if (_enableVoiceEqualizerAnimation != value)
                {
                    _enableVoiceEqualizerAnimation = value;
                    OnPropertyChanged();
                    _settings.EnableVoiceEqualizerAnimation = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private VisualPreset _currentPreset = VisualPreset.Custom;
        public VisualPreset CurrentPreset
        {
            get => _currentPreset;
            set
            {
                if (_currentPreset != value)
                {
                    _currentPreset = value;
                    OnPropertyChanged();
                    
                    if (value != VisualPreset.Custom)
                    {
                        var presetUseCase = new ApplyVisualPresetUseCase(new SettingsRepositoryAdapter(_settings));
                        presetUseCase.ApplyPreset(value);
                        
                        // Sync loaded settings back to properties
                        _overlayScale = _settings.OverlayScale;
                        _overlayWidth = _settings.OverlayWidth;
                        _displayMode = _settings.DisplayMode;
                        _sortOrder = _settings.SortOrder;
                        _cardStyle = _settings.CardStyle;
                        _talkingAccentColor = _settings.TalkingAccentColor;
                        _showHeader = _settings.ShowHeader;

                        OnPropertyChanged(nameof(OverlayScale));
                        OnPropertyChanged(nameof(OverlayWidth));
                        OnPropertyChanged(nameof(DisplayMode));
                        OnPropertyChanged(nameof(SortOrder));
                        OnPropertyChanged(nameof(CardStyle));
                        OnPropertyChanged(nameof(TalkingAccentColor));
                        OnPropertyChanged(nameof(ShowHeader));
                    }

                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private AppLanguage _language = AppLanguage.Russian;
        public AppLanguage Language
        {
            get => _language;
            set
            {
                if (_language != value)
                {
                    _language = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MaxVisibleClientsDescription));
                    var langUseCase = new UpdateLanguageUseCase(new SettingsRepositoryAdapter(_settings));
                    langUseCase.UpdateLanguage(value);
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private class SettingsRepositoryAdapter : ISettingsRepository
        {
            public AppSettings Settings { get; }
            public SettingsRepositoryAdapter(AppSettings settings) => Settings = settings;
            public void Save() => Settings.Save();
        }

        private bool _autostartWithWindows;
        public bool AutostartWithWindows
        {
            get => _autostartWithWindows;
            set
            {
                if (_autostartWithWindows != value)
                {
                    _autostartWithWindows = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _ts3ApiKey = string.Empty;
        public string TS3ApiKey
        {
            get => _ts3ApiKey;
            set
            {
                if (_ts3ApiKey != value)
                {
                    _ts3ApiKey = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _ts6ApiKey;
        public string TS6ApiKey
        {
            get => _ts6ApiKey;
            set
            {
                if (_ts6ApiKey != value)
                {
                    _ts6ApiKey = value;
                    OnPropertyChanged();
                }
            }
        }

        private OverlayPosition _position;
        public OverlayPosition Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    OnPropertyChanged();
                    _settings.Position = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private AppTheme _theme;
        public AppTheme Theme
        {
            get => _theme;
            set
            {
                if (_theme != value)
                {
                    _theme = value;
                    OnPropertyChanged();
                    TeamSpeakOverlay.Infrastructure.ThemeManager.ApplyTheme(_theme);
                }
            }
        }

        public ObservableCollection<string> TargetProcesses { get; }

        private string _newProcessName = string.Empty;
        public string NewProcessName
        {
            get => _newProcessName;
            set
            {
                if (_newProcessName != value)
                {
                    _newProcessName = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _selectedProcess;
        public string? SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                if (_selectedProcess != value)
                {
                    _selectedProcess = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddProcessCommand { get; }
        public ICommand RemoveProcessCommand { get; }

        private WhisperAccentColor _whisperAccentColor;
        public WhisperAccentColor WhisperAccentColor
        {
            get => _whisperAccentColor;
            set
            {
                if (_whisperAccentColor != value)
                {
                    _whisperAccentColor = value;
                    OnPropertyChanged();
                    _settings.WhisperAccentColor = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        private bool _showAuthorBranding = true;
        public bool ShowAuthorBranding
        {
            get => _showAuthorBranding;
            set
            {
                if (_showAuthorBranding != value)
                {
                    _showAuthorBranding = value;
                    OnPropertyChanged();
                    _settings.ShowAuthorBranding = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        public string AuthorTelegramHandle => _settings.AuthorTelegramHandle;

        private int _maxVisibleClients = 0;
        public int MaxVisibleClients
        {
            get => _maxVisibleClients;
            set
            {
                if (_maxVisibleClients != value)
                {
                    _maxVisibleClients = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MaxVisibleClientsDescription));
                    _settings.MaxVisibleClients = value;
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        public string MaxVisibleClientsDescription
        {
            get
            {
                if (MaxVisibleClients == 0)
                {
                    var res = System.Windows.Application.Current?.TryFindResource("MaxClients_Unlimited");
                    return res as string ?? "0 — Показывать всех (без ограничений)";
                }
                else
                {
                    var resFormat = System.Windows.Application.Current?.TryFindResource("MaxClients_Limited") as string ?? "{0} участников (максимум)";
                    return string.Format(resFormat, MaxVisibleClients);
                }
            }
        }

        private TeamSpeakConnectionMode _teamSpeakMode;
        public TeamSpeakConnectionMode TeamSpeakMode
        {
            get => _teamSpeakMode;
            set
            {
                if (_teamSpeakMode != value)
                {
                    _teamSpeakMode = value;
                    OnPropertyChanged();
                    _settings.TeamSpeakMode = value;
                    Logger.Info($"[UI-Log] SettingsViewModel TeamSpeakMode changed to: {value}", "SettingsViewModel");
                    _onApplyImmediate?.Invoke();
                }
            }
        }

        public Array TeamSpeakModeValues => Enum.GetValues(typeof(TeamSpeakConnectionMode));

        private void Save(object? parameter)
        {
            Logger.Info($"[UI-Log] Saving AppSettings via SettingsViewModel. Mode={TeamSpeakMode}", "SettingsViewModel");
            _settings.OverlayOpacity = OverlayOpacity;
            _settings.OverlayScale = OverlayScale;
            _settings.OverlayWidth = OverlayWidth;
            _settings.MarginX = MarginX;
            _settings.MarginY = MarginY;
            _settings.DisplayMode = DisplayMode;
            _settings.SortOrder = SortOrder;
            _settings.CardStyle = CardStyle;
            _settings.TalkingAccentColor = TalkingAccentColor;
            _settings.NicknameColor = NicknameColor;
            _settings.SpeakingNickMode = SpeakingNickMode;
            _settings.WhisperAccentColor = WhisperAccentColor;
            _settings.ShowHeader = ShowHeader;
            _settings.EnableHotkeys = EnableHotkeys;
            _settings.ShowClockInHeader = ShowClockInHeader;
            _settings.TimeFormat = TimeFormat;
            _settings.EnablePokeNotifications = EnablePokeNotifications;
            _settings.ShowAuthorBranding = ShowAuthorBranding;
            _settings.EnableSoundNotifications = EnableSoundNotifications;
            _settings.UseGameCharacterName = UseGameCharacterName;
            _settings.EnableClickThrough = EnableClickThrough;
            _settings.AlwaysShowOnTop = AlwaysShowOnTop;
            _settings.EnableVoiceEqualizerAnimation = EnableVoiceEqualizerAnimation;
            _settings.MaxVisibleClients = MaxVisibleClients;
            _settings.CurrentPreset = CurrentPreset;
            _settings.AutostartWithWindows = AutostartWithWindows;
            _settings.TS3ApiKey = TS3ApiKey;
            _settings.TS6ApiKey = TS6ApiKey;
            _settings.TeamSpeakMode = TeamSpeakMode;
            _settings.Position = Position;
            _settings.Theme = Theme;
            _settings.Language = Language;
            _settings.TargetProcesses = TargetProcesses.ToList();
            
            _settings.Save();
            _onSaveCallback?.Invoke();
            _onCloseCallback?.Invoke();
        }

        private void Cancel(object? parameter)
        {
            _onCloseCallback?.Invoke();
        }

        public string AppVersionDisplay => AppVersion.FullName;

        private void AddProcess(object? parameter)
        {
            if (!string.IsNullOrWhiteSpace(NewProcessName) && !TargetProcesses.Contains(NewProcessName))
            {
                TargetProcesses.Add(NewProcessName);
                NewProcessName = string.Empty;
            }
        }

        private bool CanRemoveProcess(object? parameter) => !string.IsNullOrEmpty(SelectedProcess);

        private void RemoveProcess(object? parameter)
        {
            if (!string.IsNullOrEmpty(SelectedProcess))
            {
                TargetProcesses.Remove(SelectedProcess);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}



