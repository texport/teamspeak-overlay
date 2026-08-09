using System;
using System.Windows;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Domain.Interfaces;
using TeamSpeakOverlay.Application.UseCases;
using TeamSpeakOverlay.Infrastructure;
using TeamSpeakOverlay.Infrastructure.GameTracker;
using TeamSpeakOverlay.Infrastructure.Logging;
using TeamSpeakOverlay.Infrastructure.TeamSpeak;
using TeamSpeakOverlay.Infrastructure.Win32;
using TeamSpeakOverlay.ViewModels;
using TeamSpeakOverlay.Views;

namespace TeamSpeakOverlay
{
    public partial class App : System.Windows.Application
    {
        private SettingsService? _settingsService;
        private Win32GameTrackerService? _gameTracker;
        private TeamSpeakDualScannerService? _tsScanner;
        private MainViewModel? _viewModel;
        private OverlayWindow? _overlayWindow;
        private SystemTrayManager? _trayManager;

        private ObserveGameStateUseCase? _observeGameUseCase;
        private ObserveTeamSpeakStateUseCase? _observeTSUseCase;
        private UpdateSettingsUseCase? _updateSettingsUseCase;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Logger.Info("=================== APPLICATION STARTUP ===================", "App");

            try
            {
                // 1. Initialize Repositories and Providers (Infrastructure)
                _settingsService = new SettingsService();
                
                // Apply the saved theme & language immediately
                ThemeManager.ApplyTheme(_settingsService.Settings.Theme);
                Infrastructure.Localization.LocalizationManager.ApplyLanguage(_settingsService.Settings.Language);

                _gameTracker = new Win32GameTrackerService(_settingsService.Settings.TargetProcesses);
                _tsScanner = new TeamSpeakDualScannerService(_settingsService);

                // 2. Initialize UseCases (Application)
                _observeGameUseCase = new ObserveGameStateUseCase(_gameTracker);
                _observeTSUseCase = new ObserveTeamSpeakStateUseCase(_tsScanner);
                _updateSettingsUseCase = new UpdateSettingsUseCase(_settingsService);
                var applyPresetUseCase = new ApplyVisualPresetUseCase(_settingsService);

                var hotkeyService = new Infrastructure.Hotkeys.Win32HotkeyService();
                var hotkeyUseCase = new RegisterGlobalHotkeysUseCase(hotkeyService);

                // 3. Initialize ViewModels (Presentation)
                _viewModel = new MainViewModel(_observeGameUseCase, _observeTSUseCase, _updateSettingsUseCase, applyPresetUseCase);
                _viewModel.ForceShowTestMode = true; // Show overlay on screen immediately

                hotkeyUseCase.ToggleVisibilityRequested += (s, ev) => _viewModel.ToggleVisibilityHotkey();
                hotkeyUseCase.ToggleDisplayModeRequested += (s, ev) => _viewModel.ToggleDisplayModeHotkey();

                // Attach ViewModel to MainWindow
                if (MainWindow is OverlayWindow window)
                {
                    _overlayWindow = window;
                    _overlayWindow.SetViewModel(_viewModel);
                }
                else
                {
                    _overlayWindow = new OverlayWindow(_viewModel);
                    MainWindow = _overlayWindow;
                }

                _overlayWindow.SourceInitialized += (s, ev) =>
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(_overlayWindow);
                    hotkeyUseCase.Initialize(helper.Handle);
                };

                // Initialize System Tray
                _trayManager = new SystemTrayManager(
                    onExitAction: ShutdownApp,
                    onOpenSettingsAction: () =>
                    {
                        if (System.Windows.Application.Current.Dispatcher.CheckAccess())
                        {
                            var settingsWindow = new SettingsWindow(_settingsService.Settings, () => _viewModel.RefreshSettings());
                            settingsWindow.ShowDialog();
                            _viewModel.RefreshSettings();
                            // Re-init game tracker targets if they changed
                            _observeGameUseCase.UpdateTargets(_settingsService.Settings.TargetProcesses);
                        }
                        else
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                var settingsWindow = new SettingsWindow(_settingsService.Settings, () => _viewModel.RefreshSettings());
                                settingsWindow.ShowDialog();
                                _viewModel.RefreshSettings();
                                _observeGameUseCase.UpdateTargets(_settingsService.Settings.TargetProcesses);
                            });
                        }
                    }
                );

                // Wire status updates to tray icon
                _viewModel.PropertyChanged += (s, ev) =>
                {
                    if (ev.PropertyName == nameof(MainViewModel.StatusText))
                    {
                        _trayManager.UpdateStatus(_viewModel.StatusText);
                    }
                };

                _trayManager.ShowNotification(
                    "TeamSpeak Overlay", 
                    "Overlay active in system tray. Tracking Lineage II (lu4.bin / l2.exe).",
                    System.Windows.Forms.ToolTipIcon.Info
                );
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal startup error", ex, "App");
                System.Windows.MessageBox.Show($"Startup Error: {ex.Message}", "TS Overlay Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void ShutdownApp()
        {
            Logger.Info("Shutdown requested from System Tray", "App");
            _trayManager?.Dispose();
            _gameTracker?.Dispose();
            _tsScanner?.Dispose();
            _settingsService?.Save();

            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("=================== APPLICATION EXIT ===================", "App");
            _trayManager?.Dispose();
            _gameTracker?.Dispose();
            _tsScanner?.Dispose();
            base.OnExit(e);
        }
    }
}


