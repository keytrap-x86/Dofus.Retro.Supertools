using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Dofus.Retro.Supertools.Windows
{
    /// <summary>
    /// Interaction logic for LoginStatus.xaml
    /// </summary>
    public partial class LoginStatus : INotifyPropertyChanged
    {
        private string _status;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Status
        {
            get => _status;
            set { _status = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Status"));
                Console.WriteLine(value);
            }
        }

        public LoginStatus(string status)
        {
            InitializeComponent();
            Status = status;
        }


        public void UpdateWindowLocation()
        {
            var scrWidth = Screen.PrimaryScreen.WorkingArea.Width;
            var scrHeight = Screen.PrimaryScreen.WorkingArea.Height;

            // ReSharper disable once PossibleLossOfFraction
            Left = scrWidth / 2 + (Width / 2);
            Top = scrHeight + (Height - 30);
           
        }
    }
}
