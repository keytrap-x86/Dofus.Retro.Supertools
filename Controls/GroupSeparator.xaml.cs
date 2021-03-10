using Dofus.Retro.Supertools.Annotations;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Dofus.Retro.Supertools.Controls
{
    /// <summary>
    /// Interaction logic for GroupSeparator.xaml
    /// </summary>
    public partial class GroupSeparator : INotifyPropertyChanged
    {
        private string _header;

        public string Header
        {
            get => _header;
            set
            {
                _header = value; 
                OnPropertyChanged();
            }
        }

        public GroupSeparator()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
