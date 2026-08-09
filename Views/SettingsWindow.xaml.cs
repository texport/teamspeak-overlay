using System.Windows;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.ViewModels;

namespace TeamSpeakOverlay.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(AppSettings settings, Action? onApplyImmediate = null)
        {
            InitializeComponent();
            
            // Set up ViewModel
            DataContext = new SettingsViewModel(
                settings,
                onSaveCallback: () => 
                {
                    // Handle any immediate apply logic if necessary, though AppSettings are saved by VM
                },
                onCloseCallback: () => 
                {
                    this.Close();
                },
                onApplyImmediate: onApplyImmediate
            );
        }
    }
}

