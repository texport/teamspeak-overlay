using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TeamSpeakOverlay.Domain.Entities
{
    public class ClientItem : INotifyPropertyChanged
    {
        private int _clientId;
        private string _nickname = string.Empty;
        private bool _isTalking;
        private bool _isMicMuted;
        private bool _isDeafened;
        private bool _isAway;
        private int _channelId;
        private bool _isTemporary;

        public int ClientId
        {
            get => _clientId;
            set { if (_clientId != value) { _clientId = value; OnPropertyChanged(); } }
        }

        public string Nickname
        {
            get => _nickname;
            set { if (_nickname != value) { _nickname = value; OnPropertyChanged(); } }
        }

        public bool IsTalking
        {
            get => _isTalking;
            set { if (_isTalking != value) { _isTalking = value; OnPropertyChanged(); } }
        }

        public bool IsMicMuted
        {
            get => _isMicMuted;
            set { if (_isMicMuted != value) { _isMicMuted = value; OnPropertyChanged(); } }
        }

        public bool IsDeafened
        {
            get => _isDeafened;
            set { if (_isDeafened != value) { _isDeafened = value; OnPropertyChanged(); } }
        }

        public bool IsAway
        {
            get => _isAway;
            set { if (_isAway != value) { _isAway = value; OnPropertyChanged(); } }
        }

        public int ChannelId
        {
            get => _channelId;
            set { if (_channelId != value) { _channelId = value; OnPropertyChanged(); } }
        }

        public bool IsTemporary
        {
            get => _isTemporary;
            set
            {
                if (_isTemporary != value)
                {
                    _isTemporary = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsWhisper));
                }
            }
        }

        public bool IsWhisper
        {
            get => _isTemporary;
            set => IsTemporary = value;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

