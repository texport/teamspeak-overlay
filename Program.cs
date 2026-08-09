using System;
using System.Threading;
using System.Windows;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay
{
    public static class Program
    {
        private static Mutex? _singleInstanceMutex;

        [STAThread]
        public static void Main(string[] args)
        {
            const string mutexName = "Global\\TeamSpeakOverlay_SingleInstance_Mutex_998822";
            _singleInstanceMutex = new Mutex(true, mutexName, out bool createdNew);

            if (!createdNew)
            {
                // Already running another instance!
                return;
            }

            try
            {
                Logger.Info($"Starting {TeamSpeakOverlay.Domain.Entities.AppVersion.FullName}...", "Main");

                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal unhandled exception in Program.Main", ex, "Main");
                System.Windows.MessageBox.Show($"Application error: {ex.Message}\n\nCheck overlay.log for details.", "TeamSpeak Overlay Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                _singleInstanceMutex?.ReleaseMutex();
            }
        }
    }
}

