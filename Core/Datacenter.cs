using Dofus.Retro.Supertools.Controls;
using Dofus.Retro.Supertools.Core.Helpers;
using Dofus.Retro.Supertools.Core.Object;
using Dofus.Retro.Supertools.Windows;

using IniParser;
using IniParser.Model;

using SharpPcap;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace Dofus.Retro.Supertools.Core
{
    public class Datacenter : INotifyPropertyChanged
    {
        public Datacenter()
        {
            IniConfigFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Dofus.Retro.Supertools";
            IniConfigFilePath = Path.Combine(IniConfigFolderPath, "configuration.ini");

            // Données récupérées via WireShark
            Servers = new List<Server>
            {
                new Server("1", "172.65.206.193", "Lobby"),
                new Server("601", "172.65.214.172", "Eratz"),
                new Server("602", "172.65.242.55", "Henual"),
                new Server("612", "172.65.226.26", "Boune"),
                new Server("613", "172.65.230.220", "Crail"),
                new Server("614", "172.65.204.203", "Galgarion")
            };

            // Pour créer de nouvelles valeurs, se référer à :
            // https://www.autohotkey.com/boards/viewtopic.php?style=2&f=6&t=17834
            // https://i.imgur.com/5zOc7co.gif
            OcrStrings = new List<OcrString>
            {
                new OcrString("Eratz", "|<>0x9E7B47@1.00$29.zzzzy1zyDw3zwTts000EE000UX0XXD616C0A2040M40Dzzzzs"),
                new OcrString("Crail", "|<>0x9E7B47@1.00$26.zzzzw7zsa1zy9XE0WFw00YTC09XH02M4k0b1C09zzzzs"),
                new OcrString("Henual", "|<>0x9E7B47@1.00$41.zzzzzzz6DzzzzCATzzzyQMUUF84s000WE9k00N4UHX0sm90b601Y21CAE3842Tzzzzzzs"),
                new OcrString("Galgarion", "|<>0x9E7B47@1.00$55.zzzzzzzzzzzzzzzzzzw3zDzzwTzw1zbzzyDzyCUH10U8430E90UE401U84WE9mM4lY2F84t42M21842QUFC10a21CM8bzzy1zzzzy7zz1zzzzkU"),
                new OcrString("ServerList", "|<>0x796F5A@1.00$69.zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz" +
                                            "3DzszXzzzzzU9zz7wTzzzzs10kNkX1sW1z3s4184ED4E7sz0W92U1sW8z7tYF84ED4F7sNAW9001sW8zU9Y1800D0F7y1AkN0U3w28zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzw"),
                new OcrString("Boune", "|<>*179$41.zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzw7zzzzzs7zzzzznA4n1UzUt9aH9z8q3AY3yMA6N9zw31UmMDwD71isTzzzzzzy"),
                new OcrString("SInscrire", "|<>*193$62.zzzzzzzzzzzzizzzzyzzzkNDzzzzDzztqzzzzzzzzyTgkA624kETUTAGB6VA9rw7nAkHtnC0zswnA4yQnbz0TAm90bAw3sDvAUs9nDVzzzzzzzzzzzzzzzzzzzzzU")
            };

            Uid = UidHelper.GetComputerSid()?.ToString();

            CharacterControllers = new ObservableCollection<CharacterController>();
        }

        #region INotify

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion INotify

        internal void SaveIni()
        {
            IniParser?.WriteFile(Static.Datacenter.IniConfigFilePath, Static.Datacenter.IniData, Encoding.UTF8);
        }

        public string Uid { get; set; }

        public List<Server> Servers { get; set; }

        public List<OcrString> OcrStrings { get; set; }

        public MainWindow MainWindow { get; set; }

        public string IniConfigFolderPath { get; set; }

        public string IniConfigFilePath { get; set; }

        public string DofusRetroExePath { get; set; } 

        public bool IsTopMost { get; set; }

        public bool SwitchOnTurnStart { get; set; }

        public bool SwitchOnExchangeRequest { get; set; }

        public bool SwitchOnGroupInvite { get; set; }

        public string LocalIp { get; set; }

        public ICaptureDevice CaptureDevice = null;

        private ObservableCollection<CharacterController> _charactercontrollers;
        public ObservableCollection<CharacterController> CharacterControllers
        {
            get => _charactercontrollers;
            set
            {
                _charactercontrollers = value;
                OnPropertyChanged();
            }
        }

        public IniData IniData;
        public FileIniDataParser IniParser;
        public AddCharacterView AddCharacterView { get; set; }

        private int _yAxisSpace;
        public int YAxisSpace
        {
            get => _yAxisSpace;
            set
            {
                if (value == _yAxisSpace)
                    return;
                _yAxisSpace = value;
                OnPropertyChanged();
            }
        }

        private int _xAxisSpace;
        public int XAxisSpace
        {
            get => _xAxisSpace;
            set
            {
                if (value == _xAxisSpace)
                    return;
                _xAxisSpace = value;
                OnPropertyChanged();
            }
        }

        private int _moveDelay;
        public int MoveDelay
        {
            get => _moveDelay;
            set
            {
                if (value == _moveDelay) 
                    return;
                _moveDelay = value;
                OnPropertyChanged();
            }
        }

        private string _currentVersion;
        public string CurrentVersion
        {
            get => _currentVersion;
            set
            {
                _currentVersion = value;
                OnPropertyChanged();
            }
        }

    }
}