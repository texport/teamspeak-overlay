using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TeamSpeakOverlay.Domain.Entities
{
    public class ChannelInfo : INotifyPropertyChanged
    {
        private int _channelId;
        private string _name = "No Channel";
        private string _serverName = "TeamSpeak Disconnected";

        public int ChannelId
        {
            get => _channelId;
            set { if (_channelId != value) { _channelId = value; OnPropertyChanged(); } }
        }

        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        public string ServerName
        {
            get => _serverName;
            set { if (_serverName != value) { _serverName = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

