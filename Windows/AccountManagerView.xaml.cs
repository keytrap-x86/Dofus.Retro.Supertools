using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Dofus.Retro.Supertools.Core;
using Dofus.Retro.Supertools.Core.Helpers;
using Dofus.Retro.Supertools.Core.Object;

namespace Dofus.Retro.Supertools.Windows
{
    /// <summary>
    /// Interaction logic for AccountManagerView.xaml
    /// </summary>
    public partial class AccountManagerView : INotifyPropertyChanged
    {

        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Character _currentCharacter;
        public Character CurrentCharacter
        {
            get => _currentCharacter;
            set
            {
                _currentCharacter = value;
                OnPropertyChanged();
            }
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

        public AccountManagerView()
        {

            InitializeComponent();

            CmbCharacters.ItemsSource = Static.Datacenter.CharacterControllers.Select(p => p.Character);
        }

        public AccountManagerView(Character character)
        {

            InitializeComponent();

            CmbCharacters.ItemsSource = Static.Datacenter.CharacterControllers.Select(p => p.Character);
            CurrentCharacter = character;

            if (CurrentCharacter.Server != null)
                CmbServers.SelectedItem = Static.Datacenter.Servers.FirstOrDefault(s => s.Name == CurrentCharacter?.Server.Name);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {

            CurrentCharacter.Username = TxtUsername.Text.Length > 0 ? TxtUsername.Text : null;
            CurrentCharacter.Password = TxtPassword.Password.Length > 0 ? TxtPassword.Password : null;
            CurrentCharacter.Server = CmbServers.SelectedItem as Server;

            Static.Datacenter.IniData.Sections[CurrentCharacter.Id]["Utilisateur"] = CurrentCharacter.Username;
            Static.Datacenter.IniData.Sections[CurrentCharacter.Id]["MotDePasse"] = SecurityHelper.Encrypt(CurrentCharacter.Password);
            Static.Datacenter.IniData.Sections[CurrentCharacter.Id]["Serveur"] = CurrentCharacter.Server?.Name;
            Static.Datacenter.IniParser.WriteFile(Static.Datacenter.IniConfigFilePath, Static.Datacenter.IniData);
        }

        private void CmbCharacters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedCharacter = (Character)CmbCharacters.SelectedItem;
            CurrentCharacter = selectedCharacter;

            if (selectedCharacter == null)
                return;
            if (TxtPassword != null)
                TxtPassword.Password = selectedCharacter.Password;
        }

        private void DarkWindow_MouseMove(object sender, MouseEventArgs e)
        {
            var mousePos = e.GetPosition(ImgDragonBody);
            if (mousePos.Y > 105 || mousePos.Y < -60)
                return;

            DragonHeadAngle = mousePos.Y / 2;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentCharacter == null)
                return;

            Static.Datacenter.IniData.Sections.RemoveSection(CurrentCharacter.Id);
            Static.Datacenter.IniParser.WriteFile(Static.Datacenter.IniConfigFilePath, Static.Datacenter.IniData);

            var characterToDelete = Static.Datacenter.CharacterControllers.FirstOrDefault(c => c.Character.Equals(CurrentCharacter));
            if (characterToDelete != null)
                Static.Datacenter.CharacterControllers.Remove(characterToDelete);

            CmbCharacters.SelectedIndex = -1;
            CmbCharacters.ItemsSource = Static.Datacenter.CharacterControllers.Select(p => p.Character);
            CurrentCharacter = null;

        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            CurrentCharacter.Password = TxtPassword.Password.Length == 0 ? null : TxtPassword.Password;
        }
    }
}
