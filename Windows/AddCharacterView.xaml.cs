using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Dofus.Retro.Supertools.Controls;
using Dofus.Retro.Supertools.Core;
using Dofus.Retro.Supertools.Core.Messages;
using Dofus.Retro.Supertools.Core.Object;

namespace Dofus.Retro.Supertools.Windows
{
    /// <summary>
    /// Interaction logic for AddCharacterView.xaml
    /// </summary>
    public partial class AddCharacterView : INotifyPropertyChanged
    {
        #region Déclarations

        private ChatMessage ChatMessage { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private double _dragonHeadAngle;
        public double DragonHeadAngle
        {
            get => _dragonHeadAngle;
            set
            {
                _dragonHeadAngle = value;
                OnPropertyChanged();
            }
        }

        #endregion


        public AddCharacterView()
        {
            
            InitializeComponent();
            
        }

        public AddCharacterView(ChatMessage chatMessage)
        {
            ChatMessage = chatMessage;
           
            InitializeComponent();
            var server = Static.Datacenter.Servers.FirstOrDefault(s => s.Ip == chatMessage.SenderIp);
            if (server != null)
                CmbServers.SelectedItem = server;
            TxtCharacterId.Text = $"Id : {chatMessage.ActorId}";
            TxtCharacterName.Text = chatMessage.ActorName;
           
        }

        private void DarkWindow_Closing(object sender, CancelEventArgs e)
        {
            Static.Datacenter.AddCharacterView = null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            Static.Datacenter.CharacterControllers.Add(new CharacterController(new Character
            {
                Id = ChatMessage.ActorId,
                Name = ChatMessage.ActorName,
                Server = CmbServers.SelectedValue as Server,
                Username = null,
                Password = null
            }));

            Static.Datacenter.IniData.Sections.AddSection(ChatMessage.ActorId);
            Static.Datacenter.IniData[ChatMessage.ActorId]["Nom"] = ChatMessage.ActorName;
            Static.Datacenter.IniData[ChatMessage.ActorId]["Touche"] = null;
            Static.Datacenter.IniData[ChatMessage.ActorId]["Serveur"] = (CmbServers.SelectedItem as Server)?.Name;
            Static.Datacenter.IniData[ChatMessage.ActorId]["Index"] = Static.Datacenter.CharacterControllers.Count.ToString();
            Static.Datacenter.IniParser.WriteFile(Static.Datacenter.IniConfigFilePath, Static.Datacenter.IniData);
            Static.Datacenter.AddCharacterView = null;


            Close();
        }

        private void DarkWindow_MouseMove(object sender, MouseEventArgs e)
        {
            var mousePos = e.GetPosition(ImgDragonBody);
            if (mousePos.Y > 140 || mousePos.Y < -60)
                return;

            DragonHeadAngle = mousePos.Y / 2;
        }
    }
}
