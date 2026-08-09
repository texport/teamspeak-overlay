using System;
using System.Collections.Generic;

namespace TeamSpeakOverlay.Domain.Interfaces
{
    public interface IGameTrackerProvider
    {
        event EventHandler<GameWindowStateEventArgs> GameWindowStateChanged;
        void StartTracking();
        void SetTargetProcesses(IEnumerable<string> targets);
    }

    public class GameWindowStateEventArgs : EventArgs
    {
        public bool IsGameActive { get; }
        public System.Drawing.Rectangle GameWindowRect { get; }
        public string GameName { get; }

        public GameWindowStateEventArgs(bool isGameActive, System.Drawing.Rectangle rect, string gameName = "")
        {
            IsGameActive = isGameActive;
            GameWindowRect = rect;
            GameName = gameName;
        }
    }
}
