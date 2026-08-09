using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using TeamSpeakOverlay.Domain.Interfaces;
using TeamSpeakOverlay.Infrastructure.Logging;
using TeamSpeakOverlay.Infrastructure.Win32;

namespace TeamSpeakOverlay.Infrastructure.GameTracker
{
    public class Win32GameTrackerService : IGameTrackerProvider, IDisposable
    {
        private readonly System.Threading.Timer _timer;
        private HashSet<string> _targetProcesses;
        private bool _isTracking;
        private bool _isDisposed;
        private IntPtr _lastActiveHwnd = IntPtr.Zero;
        private Win32Interop.RECT _lastRect;
        private bool _lastStateActive = false;
        private int _tickCount = 0;

        public event EventHandler<GameWindowStateEventArgs>? GameWindowStateChanged;

        public Win32GameTrackerService(IEnumerable<string>? targetProcesses = null)
        {
            _targetProcesses = new HashSet<string>(
                (targetProcesses ?? new[] { "lu4", "lu4.bin", "l2", "lineage2" }).Select(p => CleanProcessName(p)),
                StringComparer.OrdinalIgnoreCase
            );

            _timer = new System.Threading.Timer(OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);
            Logger.Info($"GameTracker created. Target processes: [{string.Join(", ", _targetProcesses)}]", "GameTracker");
        }

        public void SetTargetProcesses(IEnumerable<string> processNames)
        {
            _targetProcesses = new HashSet<string>(
                processNames.Select(p => CleanProcessName(p)),
                StringComparer.OrdinalIgnoreCase
            );
        }

        public void StartTracking()
        {
            if (_isTracking) return;
            _isTracking = true;
            _timer.Change(0, 1000);
            Logger.Info("GameTracker started tracking timer (1000ms).", "GameTracker");
        }

        public void StopTracking()
        {
            if (!_isTracking) return;
            _isTracking = false;
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            Logger.Info("GameTracker stopped tracking timer.", "GameTracker");
        }

        private string _lastWindowTitle = string.Empty;

        private void OnTimerTick(object? state)
        {
            try
            {
                _tickCount++;
                IntPtr activeHwnd = Win32Interop.GetForegroundWindow();
                if (activeHwnd == IntPtr.Zero)
                {
                    NotifyInactive("No foreground window.");
                    return;
                }

                if (_lastActiveHwnd != IntPtr.Zero && (!Win32Interop.IsWindow(_lastActiveHwnd) || Win32Interop.IsIconic(_lastActiveHwnd)))
                {
                    _lastActiveHwnd = IntPtr.Zero;
                    _lastStateActive = false;
                    _lastWindowTitle = string.Empty;
                }

                Win32Interop.GetWindowThreadProcessId(activeHwnd, out uint pid);
                string procName = GetProcessNameByPid(pid);
                string windowTitle = Win32Interop.GetWindowText(activeHwnd);

                if (IsMatch(procName, windowTitle) && Win32Interop.IsWindow(activeHwnd) && !Win32Interop.IsIconic(activeHwnd))
                {
                    if (Win32Interop.GetExactWindowRect(activeHwnd, out Win32Interop.RECT r))
                    {
                        bool isNewHwnd = activeHwnd != _lastActiveHwnd;
                        bool isNewTitle = _lastWindowTitle != windowTitle;
                        bool isNewRect = r.Left != _lastRect.Left || r.Top != _lastRect.Top || r.Right != _lastRect.Right || r.Bottom != _lastRect.Bottom;

                        _lastActiveHwnd = activeHwnd;
                        _lastRect = r;
                        _lastStateActive = true;
                        _lastWindowTitle = windowTitle;

                        var rect = new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);

                        if (isNewHwnd || isNewTitle || isNewRect)
                        {
                            Logger.Info($"Target Game Window Active HWND={activeHwnd} PID={pid} Proc='{procName}' Title='{windowTitle}' RECT=[{rect.X},{rect.Y},{rect.Width}x{rect.Height}]", "GameTracker");
                            GameWindowStateChanged?.Invoke(this, new GameWindowStateEventArgs(true, rect, windowTitle));
                        }
                    }
                    else
                    {
                        NotifyInactive("Failed to get window rect.");
                    }
                }
                else
                {
                    NotifyInactive($"Active foreground window HWND={activeHwnd} PID={pid} Proc='{procName}' Title='{windowTitle}' is not Lineage 2 / lu4.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in GameTracker timer tick", ex, "GameTracker");
            }
        }

        private IntPtr FindTargetGameWindow(out string matchedName)
        {
            matchedName = string.Empty;
            IntPtr foundHwnd = IntPtr.Zero;
            string foundProcName = string.Empty;

            Win32Interop.EnumWindows((hwnd, lParam) =>
            {
                if (!Win32Interop.IsWindow(hwnd) || !Win32Interop.IsWindowVisible(hwnd) || Win32Interop.IsIconic(hwnd)) return true;

                Win32Interop.GetWindowThreadProcessId(hwnd, out uint pid);
                string pName = GetProcessNameByPid(pid);
                string title = Win32Interop.GetWindowText(hwnd);

                if (IsMatch(pName, title))
                {
                    foundHwnd = hwnd;
                    foundProcName = pName;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            matchedName = foundProcName;
            return foundHwnd;
        }

        private bool IsMatch(string procName, string windowTitle = "")
        {
            if (!string.IsNullOrWhiteSpace(procName))
            {
                string clean = CleanProcessName(procName);
                if (_targetProcesses.Contains(procName) ||
                    _targetProcesses.Contains(clean) ||
                    procName.Contains("lu4", StringComparison.OrdinalIgnoreCase) ||
                    procName.Contains("lineage", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (!string.IsNullOrWhiteSpace(windowTitle))
            {
                if (windowTitle.Contains("Lineage", StringComparison.OrdinalIgnoreCase) ||
                    windowTitle.Contains("Liberta", StringComparison.OrdinalIgnoreCase) ||
                    windowTitle.Contains("L2", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void NotifyInactive(string reason)
        {
            if (_lastStateActive || _tickCount == 1)
            {
                _lastStateActive = false;
                _lastActiveHwnd = IntPtr.Zero;
                var lastRect = (_lastRect.Right > _lastRect.Left && _lastRect.Bottom > _lastRect.Top)
                    ? new Rectangle(_lastRect.Left, _lastRect.Top, _lastRect.Right - _lastRect.Left, _lastRect.Bottom - _lastRect.Top)
                    : Rectangle.Empty;
                Logger.Info($"GAME INACTIVE. Reason: {reason}", "GameTracker");
                GameWindowStateChanged?.Invoke(this, new GameWindowStateEventArgs(false, lastRect, string.Empty));
            }
        }

        private static string GetProcessNameByPid(uint pid)
        {
            if (pid == 0) return string.Empty;

            string win32Name = Win32Interop.GetProcessNameFromPidWin32(pid);
            if (!string.IsNullOrEmpty(win32Name))
            {
                return win32Name;
            }

            try
            {
                using var proc = Process.GetProcessById((int)pid);
                return proc.ProcessName;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string CleanProcessName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            string clean = name.Trim();
            if (clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || clean.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(0, clean.Length - 4);
            }
            return clean;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                StopTracking();
                _timer.Dispose();
                _isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
