using Dofus.Retro.Supertools.Core.Object;

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Dofus.Retro.Supertools.Core
{
    public class Character : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Name { get; set; }
        public string Id { get; set; }
        public string Key { get; set; }

        private string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        private string _index;
        public string Index
        {
            get => _index;
            set
            {
                _index = value;
                OnPropertyChanged();
            }
        }

        private string _username;
        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }


        private bool _isConnecting;
        public bool IsConnecting
        {
            get => _isConnecting;
            set
            {
                _isConnecting = value;
                OnPropertyChanged();
            }
        }

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }



        private Process _process;
        public Process Process
        {
            get => _process;
            set
            {
                if (value != null)
                    if (_process?.Id == value.Id)
                        return;
                _process = value;
                OnPropertyChanged();
            }
        }

        private bool _isWindowFocused;
        public bool IsWindowFocused
        {
            get => _isWindowFocused;

            set
            {
                if (value == _isWindowFocused)
                    return;
                _isWindowFocused = value;
                OnPropertyChanged();
            }
        }

        private Server _server;
        public Server Server
        {
            get => _server;
            set { _server = value;
                OnPropertyChanged();
            }
        }

    }
}
