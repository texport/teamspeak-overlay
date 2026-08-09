using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Infrastructure.Localization;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.Infrastructure.Win32
{
    public class SystemTrayManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ToolStripMenuItem _statusMenuItem;
        private readonly ToolStripMenuItem _settingsItem;
        private readonly ToolStripMenuItem _checkForUpdatesItem;
        private readonly ToolStripMenuItem _openLogItem;
        private readonly ToolStripMenuItem _exitItem;
        private readonly Icon _customIcon;
        private readonly Action _onExitAction;
        private bool _isDisposed;

        public SystemTrayManager(
            Action onExitAction, 
            Action onOpenSettingsAction,
            Action? onCheckForUpdatesAction = null)
        {
            _onExitAction = onExitAction;
            _customIcon = CreateMaterial3TrayIcon();

            _notifyIcon = new NotifyIcon
            {
                Icon = _customIcon,
                Text = $"TeamSpeak Overlay {AppVersion.DisplayVersion}",
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();

            // Header / Status
            _statusMenuItem = new ToolStripMenuItem($"TS Overlay {AppVersion.DisplayVersion}: Initializing...")
            {
                Enabled = false,
                Font = new Font(contextMenu.Font, FontStyle.Bold)
            };
            contextMenu.Items.Add(_statusMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());

            // Settings Menu Item
            _settingsItem = new ToolStripMenuItem("Open Settings");
            _settingsItem.Click += (s, e) => onOpenSettingsAction?.Invoke();
            contextMenu.Items.Add(_settingsItem);

            // Check for Updates Menu Item
            _checkForUpdatesItem = new ToolStripMenuItem("Check for Updates", null, async (s, e) =>
            {
                Logger.Info("User clicked 'Check for Updates' in System Tray menu", "TrayManager");
                if (onCheckForUpdatesAction != null)
                {
                    onCheckForUpdatesAction.Invoke();
                }
                else
                {
                    try
                    {
                        var updateUseCase = new Application.UseCases.CheckForUpdatesUseCase();
                        var release = await updateUseCase.ExecuteCheckAsync();
                        if (release != null && release.IsNewerThanCurrent)
                        {
                            _notifyIcon.ShowBalloonTip(5000, "TeamSpeak Overlay", $"Доступна новая версия {release.TagName}! Нажмите для загрузки.", ToolTipIcon.Info);
                            if (!string.IsNullOrEmpty(release.SetupAssetUrl))
                            {
                                await updateUseCase.ExecuteDownloadAndUpdateAsync(release.SetupAssetUrl);
                            }
                        }
                        else
                        {
                            _notifyIcon.ShowBalloonTip(3000, "TeamSpeak Overlay", "У вас установлена последняя версия.", ToolTipIcon.Info);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error checking updates from tray menu", ex, "TrayManager");
                    }
                }
            });
            contextMenu.Items.Add(_checkForUpdatesItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Open Log File
            _openLogItem = new ToolStripMenuItem("View Log File", null, (s, e) =>
            {
                try
                {
                    string path = Logger.AppDataLogPath;
                    if (System.IO.File.Exists(path))
                    {
                        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to open log file", ex, "TrayManager");
                }
            });
            contextMenu.Items.Add(_openLogItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Exit
            _exitItem = new ToolStripMenuItem("Exit", null, (s, e) => _onExitAction());
            contextMenu.Items.Add(_exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;

            // Subscribe to dynamic language changes
            LocalizationManager.LanguageChanged += OnLanguageChanged;
            UpdateTrayTexts();

            Logger.Info("SystemTrayManager initialized with Material 3 Icon & localized menu", "TrayManager");
        }

        private static Icon CreateMaterial3TrayIcon()
        {
            int size = 32;
            using var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 1. Dark Material Surface Background Circle
                using (var bgBrush = new SolidBrush(Color.FromArgb(255, 20, 20, 25)))
                {
                    g.FillEllipse(bgBrush, 1, 1, size - 3, size - 3);
                }

                // 2. Cyan Neon Ring
                using (var borderPen = new Pen(Color.FromArgb(255, 0, 229, 255), 1.8f))
                {
                    g.DrawEllipse(borderPen, 1.5f, 1.5f, size - 4f, size - 4f);
                }

                // 3. Central Voice Microphone Pill
                using (var cyanBrush = new SolidBrush(Color.FromArgb(255, 0, 229, 255)))
                {
                    using var path = new GraphicsPath();
                    path.AddArc(13, 7, 6, 6, 180, 180);
                    path.AddArc(13, 12, 6, 6, 0, 180);
                    path.CloseFigure();
                    g.FillPath(cyanBrush, path);
                }

                // 4. Voice Wave Arc (Neon Green)
                using (var arcPen = new Pen(Color.FromArgb(255, 0, 230, 118), 2f))
                {
                    arcPen.StartCap = LineCap.Round;
                    arcPen.EndCap = LineCap.Round;
                    g.DrawArc(arcPen, 8, 10, 16, 11, 20, 140);
                }

                // 5. Stand Base (Neon Green)
                using (var greenBrush = new SolidBrush(Color.FromArgb(255, 0, 230, 118)))
                {
                    g.FillEllipse(greenBrush, 14, 23, 4, 3);
                }
            }

            IntPtr hIcon = bitmap.GetHicon();
            Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
            return icon;
        }

        private void OnLanguageChanged(AppLanguage lang)
        {
            UpdateTrayTexts();
        }

        public void UpdateTrayTexts()
        {
            try
            {
                var app = System.Windows.Application.Current;
                if (app != null)
                {
                    if (_settingsItem != null)
                        _settingsItem.Text = app.TryFindResource("Tray_OpenSettings") as string ?? "Open Settings";

                    if (_checkForUpdatesItem != null)
                        _checkForUpdatesItem.Text = app.TryFindResource("Tray_CheckForUpdates") as string ?? "Check for Updates";

                    if (_openLogItem != null)
                        _openLogItem.Text = app.TryFindResource("Tray_ViewLog") as string ?? "View Log File";

                    if (_exitItem != null)
                        _exitItem.Text = app.TryFindResource("Tray_Exit") as string ?? "Exit";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error updating tray texts", ex, "TrayManager");
            }
        }

        public void UpdateStatus(string statusText)
        {
            if (_statusMenuItem != null)
            {
                _statusMenuItem.Text = $"TS Overlay: {statusText}";
            }
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = $"TS Overlay - {statusText}";
            }
        }

        public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, message, icon);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                LocalizationManager.LanguageChanged -= OnLanguageChanged;
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _customIcon?.Dispose();
                _isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
