using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Dofus.Retro.Supertools.Controls
{
    /// <summary>
    /// Interaction logic for WindowControlButton.xaml
    /// </summary>
    public partial class WindowControlButton : INotifyPropertyChanged
    {
        public Brush FocusBrush
        {
            get => _focusBrush;
            set
            {
                if (Equals(value, _focusBrush))
                    return;
                _focusBrush = value;
                OnPropertyChanged();
            }
        }

        private string _text;
        private Brush _focusBrush;

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPropertyChanged();
            }
        }

        public WindowControlButton()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}