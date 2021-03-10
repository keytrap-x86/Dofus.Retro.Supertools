using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Dofus.Retro.Supertools.Windows
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : INotifyPropertyChanged
    {
        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private double _dragonHeadAngle;
        public double DragonHeadAngle
        {
            get { return _dragonHeadAngle; }
            set
            {
                _dragonHeadAngle = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public SettingsView()
        {
            InitializeComponent();
        }

        private void DarkWindow_MouseMove(object sender, MouseEventArgs e)
        {
            var mousePos = e.GetPosition(GrdDragonAngleHelper);
            if (mousePos.Y > 105 || mousePos.Y < -60)
                return;

            DragonHeadAngle = mousePos.Y / 2;
        }

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://keytrap.fr");
        }
    }
}
