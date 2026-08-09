using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Infrastructure.Logging;
using TeamSpeakOverlay.Infrastructure.Win32;
using TeamSpeakOverlay.ViewModels;

namespace TeamSpeakOverlay.Views
{
    public partial class OverlayWindow : Window
    {
        private MainViewModel? _viewModel;

        public OverlayWindow()
        {
            InitializeComponent();
            Logger.Info("OverlayWindow parameterless created", "OverlayWindow");
        }

        public OverlayWindow(MainViewModel viewModel) : this()
        {
            SetViewModel(viewModel);
        }

        public void SetViewModel(MainViewModel viewModel)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            _viewModel = viewModel;
            DataContext = _viewModel;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            Logger.Info("OverlayWindow ViewModel attached", "OverlayWindow");
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Apply Win32 Extended Window Styles & WPF HitTestVisibility based on EnableClickThrough setting
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            bool clickThrough = _viewModel?.EnableClickThrough ?? false;
            IsHitTestVisible = !clickThrough;
            Win32Interop.SetClickThrough(hwnd, clickThrough);
            Logger.Info($"Applied Win32 ClickThrough ({clickThrough}, IsHitTestVisible={IsHitTestVisible}) on HWND {hwnd}", "OverlayWindow");

            if (_viewModel != null && !_viewModel.IsOverlayVisible)
            {
                Visibility = Visibility.Hidden;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_viewModel == null) return;

            if (e.PropertyName == nameof(MainViewModel.EnableClickThrough))
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                IsHitTestVisible = !_viewModel.EnableClickThrough;
                Win32Interop.SetClickThrough(hwnd, _viewModel.EnableClickThrough);
                Logger.Info($"Updated Win32 ClickThrough ({_viewModel.EnableClickThrough}, IsHitTestVisible={IsHitTestVisible}) on HWND {hwnd}", "OverlayWindow");
            }
            else if (e.PropertyName == nameof(MainViewModel.IsOverlayVisible))
            {
                if (_viewModel.IsOverlayVisible)
                {
                    ShowOverlay();
                }
                else
                {
                    HideOverlay();
                }
            }
            else if (e.PropertyName == nameof(MainViewModel.GameWindowRect) ||
                     e.PropertyName == nameof(MainViewModel.Position) ||
                     e.PropertyName == nameof(MainViewModel.MarginX) ||
                     e.PropertyName == nameof(MainViewModel.MarginY) ||
                     e.PropertyName == nameof(MainViewModel.OverlayScale) ||
                     e.PropertyName == nameof(MainViewModel.OverlayWidth) ||
                     e.PropertyName == nameof(MainViewModel.ShowHeader))
            {
                if (_viewModel.IsOverlayVisible)
                {
                    UpdatePosition(_viewModel.GameWindowRect);
                }
            }
        }

        private void ShowOverlay()
        {
            Logger.Info($"[UI-Log] ShowOverlay called (CurrentVisibility={Visibility}, IsOverlayVisible={_viewModel?.IsOverlayVisible}, IsGameActive={_viewModel?.IsGameActive})", "OverlayWindow");
            Visibility = Visibility.Visible;
            if (_viewModel != null)
            {
                UpdatePosition(_viewModel.GameWindowRect);
            }
        }

        private void HideOverlay()
        {
            Logger.Info($"[UI-Log] HideOverlay called (CurrentVisibility={Visibility})", "OverlayWindow");
            Visibility = Visibility.Hidden;
        }

        private System.Drawing.Rectangle _lastAppliedGameRect = System.Drawing.Rectangle.Empty;

        private void UpdatePosition(System.Drawing.Rectangle gameRect)
        {
            if (_viewModel == null) return;

            if (gameRect.IsEmpty && !_lastAppliedGameRect.IsEmpty)
            {
                gameRect = _lastAppliedGameRect;
            }
            else if (!gameRect.IsEmpty)
            {
                _lastAppliedGameRect = gameRect;
            }

            if (gameRect.IsEmpty)
            {
                // Fallback to Primary Screen Bounds if GameWindowRect is empty (Test Mode)
                var screenRect = System.Windows.Forms.Screen.PrimaryScreen?.Bounds ?? new System.Drawing.Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
                gameRect = screenRect;
            }

            double dpiScaleX = 1.0;
            double dpiScaleY = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
            }

            double dipLeft = gameRect.Left / dpiScaleX;
            double dipTop = gameRect.Top / dpiScaleY;
            double dipRight = gameRect.Right / dpiScaleX;
            double dipBottom = gameRect.Bottom / dpiScaleY;
            double dipHeight = gameRect.Height / dpiScaleY;

            double scale = _viewModel.OverlayScale > 0 ? _viewModel.OverlayScale : 1.0;
            double winWidth = (_viewModel.OverlayWidth > 0 ? _viewModel.OverlayWidth : 280) * scale;
            double winHeight = (ActualHeight > 0 ? ActualHeight : 200) * scale;

            // Динамический расчет предельной высоты оверлея, чтобы он никогда не вылезал за экран
            double availableScreenHeight = dipHeight > 0 ? dipHeight - 80 : SystemParameters.PrimaryScreenHeight - 100;
            double maxAllowedHeight = Math.Max(150, availableScreenHeight / scale);
            MaxHeight = maxAllowedHeight;
            if (_viewModel.OverlayWidth > 0)
            {
                Width = _viewModel.OverlayWidth;
            }

            int mx = _viewModel.MarginX;
            int my = _viewModel.MarginY;

            double targetLeft = dipLeft + mx;
            double targetTop = dipTop + my;

            switch (_viewModel.Position)
            {
                case OverlayPosition.TopLeft:
                    targetLeft = dipLeft + mx;
                    targetTop = dipTop + my;
                    break;
                case OverlayPosition.TopRight:
                    targetLeft = dipRight - winWidth - mx;
                    targetTop = dipTop + my;
                    break;
                case OverlayPosition.BottomLeft:
                    targetLeft = dipLeft + mx;
                    targetTop = dipBottom - winHeight - my;
                    break;
                case OverlayPosition.BottomRight:
                    targetLeft = dipRight - winWidth - mx;
                    targetTop = dipBottom - winHeight - my;
                    break;
                case OverlayPosition.CenterLeft:
                    targetLeft = dipLeft + mx;
                    targetTop = dipTop + Math.Max(10, (dipHeight - winHeight) / 2);
                    break;
                case OverlayPosition.CenterRight:
                    targetLeft = dipRight - winWidth - mx;
                    targetTop = dipTop + Math.Max(10, (dipHeight - winHeight) / 2);
                    break;
                case OverlayPosition.Custom:
                    targetLeft = _viewModel.CustomX ?? (dipLeft + mx);
                    targetTop = _viewModel.CustomY ?? (dipTop + my);
                    break;
            }

            // Страховка координат от ухода за края экрана
            if (_viewModel.Position != OverlayPosition.Custom)
            {
                if (targetLeft < dipLeft) targetLeft = dipLeft + 10;
                if (targetTop < dipTop) targetTop = dipTop + 10;
            }

            Left = targetLeft;
            Top = targetTop;

            Logger.Info($"Positioned Overlay at [{targetLeft:F1}, {targetTop:F1}] (MaxHeight={maxAllowedHeight:F1}, Scale={scale}, Position={_viewModel.Position}, Dpi={dpiScaleX:F2})", "OverlayWindow");

            // Keep Window Topmost on screen without overriding WPF dynamic SizeToContent
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            Win32Interop.SetWindowPos(
                hwnd, 
                Win32Interop.HWND_TOPMOST, 
                0, 0, 0, 0, 
                Win32Interop.SWP_NOMOVE | Win32Interop.SWP_NOSIZE | Win32Interop.SWP_NOACTIVATE | Win32Interop.SWP_SHOWWINDOW
            );
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (_viewModel != null && !_viewModel.EnableClickThrough)
            {
                try
                {
                    DragMove();
                    _viewModel.UpdateCustomPosition(Left, Top);
                    Logger.Info($"Mouse drag moved overlay to Custom position [{Left:F1}, {Top:F1}]", "OverlayWindow");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"DragMove exception: {ex.Message}", "OverlayWindow");
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Minimize to Tray instead of exit
            e.Cancel = true;
            HideOverlay();
            Logger.Info("OverlayWindow OnClosing -> hidden to tray", "OverlayWindow");
        }
    }
}


