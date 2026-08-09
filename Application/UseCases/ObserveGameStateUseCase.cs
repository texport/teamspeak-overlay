using System;
using System.Collections.Generic;
using TeamSpeakOverlay.Domain.Interfaces;

namespace TeamSpeakOverlay.Application.UseCases
{
    public class ObserveGameStateUseCase
    {
        private readonly IGameTrackerProvider _gameTracker;

        public event EventHandler<GameWindowStateEventArgs>? GameWindowStateChanged;

        public ObserveGameStateUseCase(IGameTrackerProvider gameTracker)
        {
            _gameTracker = gameTracker;
            _gameTracker.GameWindowStateChanged += (s, e) => GameWindowStateChanged?.Invoke(this, e);
        }

        public void Execute()
        {
            _gameTracker.StartTracking();
        }

        public void UpdateTargets(IEnumerable<string> targets)
        {
            _gameTracker.SetTargetProcesses(targets);
        }
    }
}
