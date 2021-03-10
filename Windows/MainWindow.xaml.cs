using AutoIt;

using Dofus.Retro.Supertools.Controls;
using Dofus.Retro.Supertools.Controls.DragDropListView;
using Dofus.Retro.Supertools.Core;
using Dofus.Retro.Supertools.Core.Helpers;
using Dofus.Retro.Supertools.Core.Messages;
using Dofus.Retro.Supertools.Core.Packets;

using Gma.System.MouseKeyHook;

using HandyControl.Data;

using IniParser;

using Newtonsoft.Json;

using PacketDotNet;

using SharpPcap;
using SharpPcap.Npcap;

using Squirrel;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using Winook;

using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Timers.Timer;

namespace Dofus.Retro.Supertools.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        #region Variables globales

        public int CurrentYear { get; set; } = DateTime.Now.Year;

        public IKeyboardMouseEvents GlobalMkHook { get; set; }

        public static event EventHandler<List<CharacterListMessage>> CharacterListReceived;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private bool _isNpcapInstalled;
        public bool IsNpcapInstalled
        {
            get => _isNpcapInstalled;
            set
            {
                _isNpcapInstalled = value;
                OnPropertyChanged();
            }
        }

        public List<Character> NotConnectedCharacters => Static.Datacenter.CharacterControllers
            ?.Where(c => !c.Character.IsConnected).Select(p => p.Character).ToList();

        public List<Character> CheckedCharacters => Static.Datacenter.CharacterControllers
            ?.Where(c => c.Character.IsChecked).Select(p => p.Character).ToList();

        public Timer DofusMonitorProcess { get; set; }

        private string _status;
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        private bool _isFollowerRunning;
        public bool IsFollowerRunning
        {
            get => _isFollowerRunning;
            set
            {
                _isFollowerRunning = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public MainWindow()
        {
            Static.Datacenter = new Datacenter();
            
            try
            {
                LoadUserParameters();
            }
            catch (Exception)
            {
                Status = "Erreur dans le chargement des paramètres";
            }


            IsNpcapInstalled = RegistryHelper.IsNpcapInstalled();

            InitializeComponent();

            Static.Datacenter.MainWindow = this;

            // ReSharper disable once ObjectCreationAsStatement
            new ListViewDragDropManager<CharacterController>(LstvCharacters);

            DofusMonitorProcess = new Timer(1000);
            DofusMonitorProcess.Elapsed += DofusProcessMonitorTimer_Elapsed;
            DofusMonitorProcess.Start();

            if (!IsNpcapInstalled)
            {
                ChkSwitchFenetreCombat.IsChecked = false; 
                ChkSwitchFenetreGroupe.IsChecked = false;
                ChkSwitchFenetreEchange.IsChecked = false;
                StkAutoSwitches.ToolTip =
                    "Npcap n'est pas installé.\n" +
                    "Pour utiliser le logiciel, veuillez l'installer\n" +
                    "en cliquant sur le bouton ci-dessous"; 
            }

            RegisterKeyboardShortcuts();

        }

        #region Keyboard Hook

        private void RegisterKeyboardShortcuts()
        {
            GlobalMkHook = Hook.GlobalEvents();
            var undo = Combination.FromString("Alt+W");
            void ActionStartStopFollower() => StartStopFollowing();
            var assignment = new Dictionary<Combination, Action>
            {
                {undo, ActionStartStopFollower}
            };

            GlobalMkHook.OnCombination(assignment);

        }

        #endregion

        #region Evenements MainWidow

        /// <summary>
        /// Lorsque la forme est "dessinée" (chargée entièrement)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Mainwindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadCharactersData();
            }
            catch (Exception)
            { 
                Status = "Erreur dans le chargement des persos";
            }


            if (!IsNpcapInstalled)
                return;

            StartPacketCapture();


        }

        private async void Mainwindow_Closing(object sender, CancelEventArgs e)
        {
            Hide();
            GlobalMkHook.Dispose();
            await Task.Run(() =>
            {
                //On arrête proprement le timer 
                DofusMonitorProcess?.Stop();
                DofusMonitorProcess?.Dispose();

                //On arrête la capture des paquets
                Static.Datacenter.CaptureDevice?.StopCapture();
                Static.Datacenter.CaptureDevice?.Close();

                //On enregistre les paramètres utilisateur
                SaveUserParameters();

                //Et les paramètres des personnages
                SaveCharactersData();

                //On désinstalle le MouseHook de chaque personnage qui en a un
                foreach (var dofusProcess in Static.Datacenter.CharacterControllers.Where(p => p.Character.Process != null))
                    dofusProcess.MouseHook?.Uninstall();

                Dispatcher.Invoke(() => Application.Current.Windows.OfType<Window>().ToList().ForEach(f => f.Close()));
            });




        }

        #endregion

        #region Chargement/Sauvegarde des personnages

        /// <summary>
        /// Charge les personnages enregistrés dans le fichier .ini dans la liste
        /// </summary>
        private void LoadCharactersData()
        {
            if (Static.Datacenter.CharacterControllers == null)
                Static.Datacenter.CharacterControllers = new ObservableCollection<CharacterController>();

            foreach (var section in Static.Datacenter.IniData.Sections.Where(s =>
                    s.SectionName != "GENERAL" &&
                    Static.Datacenter.CharacterControllers.All(p => p.Character.Id != s.SectionName))
                .OrderBy(p => int.TryParse(p.Keys["Index"],
                    out _)))
            {
                var character = new Character
                {
                    Name = section.Keys["Nom"],
                    Id = section.SectionName,
                    Key = section.Keys["Touche"],
                    Username = section.Keys["Utilisateur"],
                    Index = section.Keys["Index"]
                };

                var server = section.Keys["Serveur"];
                if (!string.IsNullOrEmpty(server))
                {
                    character.Server = Static.Datacenter.Servers.FirstOrDefault(s => s.Name == server);
                    if (server.ToLower().Contains("monocompte"))
                        character.Server = Static.Datacenter.Servers.FirstOrDefault(s => s.Name == "Boune");

                    // TODO : gérer un serveur qui n'existe plus
                }


                var pwd = section.Keys["MotDePasse"];
                if (!string.IsNullOrEmpty(pwd))
                    character.Password = SecurityHelper.Decrypt(pwd);


                Static.Datacenter.CharacterControllers.Add(new CharacterController(character));
            }
            Status = "Prêt";
        }

        /// <summary>
        /// Sauvegarde les données des personnages
        /// </summary>
        private static void SaveCharactersData()
        {
            for (var i = 0; i < Static.Datacenter.CharacterControllers.Count; i++)
            {
                var character = Static.Datacenter.CharacterControllers[i];
                var section = Static.Datacenter.IniData[character.Character.Id];
                section["Index"] = i.ToString();
                section["Nom"] = character.Character.Name;
                section["Utilisateur"] = character.Character.Username;
                section["MotDePasse"] = SecurityHelper.Encrypt(character.Character.Password);
                section["Serveur"] = character.Character.Server?.Name;
            }

            Static.Datacenter.IniParser.WriteFile(Static.Datacenter.IniConfigFilePath, Static.Datacenter.IniData, Encoding.UTF8);
        }

        #endregion

        #region Chargement/Sauvegarde des paramètres utilisateur

        /// <summary>
        /// Récupère les paramètres utilisateur (position fenêtre, délai mouvement...)
        /// </summary>
        private void LoadUserParameters()
        {
            //Chargement des paramètres utilisateur

            Static.Datacenter.IniParser = new FileIniDataParser();

            //Si le dossier "C:\Users\%username%\AppData\Roaming\Dofus.Retro.Supertool" n'éxiste pas, on le créé
            if (!Directory.Exists(Static.Datacenter.IniConfigFolderPath))
                Directory.CreateDirectory(Static.Datacenter.IniConfigFolderPath);

            //Si le fichier de configuration "configuration.ini" n'éxiste pas, on le créé
            if (!File.Exists(Static.Datacenter.IniConfigFilePath))
                File.Create(Static.Datacenter.IniConfigFilePath).Close();


            Static.Datacenter.IniData = Static.Datacenter.IniParser.ReadFile(Static.Datacenter.IniConfigFilePath);
            Static.Datacenter.IniData.Configuration.CommentString = "#";


            //S'il n'y a pas de section "GENERAL", on la créé,
            //car c'est dans cette section que l'on stock les paramètres de l'utilisateur tels que
            //la position de la fenêtre, les cases autoswitch cochées etc..
            if (Static.Datacenter.IniData.Sections.All(s => s.SectionName != "GENERAL"))
            {
                Static.Datacenter.IniData.Sections.AddSection("GENERAL");

            }

            //On récupères les données
            var xPos = Static.Datacenter.IniData.Sections["GENERAL"]["X"];
            var yPos = Static.Datacenter.IniData.Sections["GENERAL"]["Y"];
            var xSpace = Static.Datacenter.IniData.Sections["GENERAL"]["XSpace"];



            var dofusRetroExePath = Static.Datacenter.IniData.Sections["GENERAL"]["DofusRetroExePath"];
            if (string.IsNullOrEmpty(dofusRetroExePath))
            {
                // On essaie de trouver le chemin de Dofus Retro (par défaut)
                var dofRetroPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Ankama\\zaap\\retro\\Dofus Retro.exe");

                if (File.Exists(dofRetroPath))
                    Static.Datacenter.DofusRetroExePath = dofRetroPath;
            }
            else
            {
                Static.Datacenter.DofusRetroExePath = dofusRetroExePath;
            }

            var ySpace = Static.Datacenter.IniData.Sections["GENERAL"]["YSpace"];

            var moveDelay = Static.Datacenter.IniData.Sections["GENERAL"]["MoveDelay"];

            var switchFight = Static.Datacenter.IniData.Sections["GENERAL"]["SwitchFight"];
            var switchExchange = Static.Datacenter.IniData.Sections["GENERAL"]["SwitchExchange"];
            var switchGroup = Static.Datacenter.IniData.Sections["GENERAL"]["SwitchGroup"];
            

            if (!string.IsNullOrEmpty(xPos) && !string.IsNullOrEmpty(yPos))
            {
                double.TryParse(xPos, out var left);
                double.TryParse(yPos, out var top);

                Left = left;
                Top = top;
            }

            Static.Datacenter.MoveDelay = !string.IsNullOrEmpty(moveDelay) ? int.Parse(moveDelay) : 200;


            Static.Datacenter.XAxisSpace = !string.IsNullOrEmpty(xSpace) ? int.Parse(xSpace) : 10;
            Static.Datacenter.YAxisSpace = !string.IsNullOrEmpty(ySpace) ? int.Parse(ySpace) : 25;

            //Si jamais la position de la fenêtre dépasse la taille de l'écran
            //on remet remet la fenêtre sur l'écran
            //Cela peut se produire si on change la résolution entre temps
            var screenW = SystemParameters.PrimaryScreenWidth;
            var screenH = SystemParameters.PrimaryScreenHeight;
            if (Left > screenW)
                Left = screenW - Width;
            if (Top > screenH)
                Top = screenH - Height;

            var isTopMost = Static.Datacenter.IniData.Sections["GENERAL"]["IsTopMost"];
            if (!string.IsNullOrEmpty(isTopMost))
            {
                Static.Datacenter.IsTopMost = bool.Parse(isTopMost);
            }

            if (!string.IsNullOrEmpty(switchFight))
            {
                Static.Datacenter.SwitchOnTurnStart = bool.Parse(switchFight);
            }

            if (!string.IsNullOrEmpty(switchExchange))
            {
                Static.Datacenter.SwitchOnExchangeRequest = bool.Parse(switchExchange);
            }

            if (!string.IsNullOrEmpty(switchGroup))
            {
                Static.Datacenter.SwitchOnGroupInvite = bool.Parse(switchGroup);
            }

        }

        /// <summary>
        /// Sauvegarde les paramètres utilisateur (position fenêtre, délai mouvement...)
        /// </summary>
        private void SaveUserParameters()
        {
            if (Static.Datacenter.IniData == null)
                return;

            if (Static.Datacenter.IniData.Sections.All(s => s.SectionName != "GENERAL"))
                Static.Datacenter.IniData.Sections.AddSection("GENERAL");

            Dispatcher.Invoke(() =>
            {
                Static.Datacenter.IniData.Sections["GENERAL"]["X"] = Math.Round(Left, 0).ToString(CultureInfo.InvariantCulture);
                Static.Datacenter.IniData.Sections["GENERAL"]["Y"] = Math.Round(Top, 0).ToString(CultureInfo.InvariantCulture);
                Static.Datacenter.IniData.Sections["GENERAL"]["IsTopMost"] = Static.Datacenter.IsTopMost.ToString();
                Static.Datacenter.IniData.Sections["GENERAL"]["SwitchFight"] = Static.Datacenter.SwitchOnTurnStart.ToString();
                Static.Datacenter.IniData.Sections["GENERAL"]["SwitchExchange"] = Static.Datacenter.SwitchOnExchangeRequest.ToString();
                Static.Datacenter.IniData.Sections["GENERAL"]["SwitchGroup"] = Static.Datacenter.SwitchOnGroupInvite.ToString();
                Static.Datacenter.IniData.Sections["GENERAL"]["XSpace"] = Static.Datacenter.XAxisSpace.ToString();
                Static.Datacenter.IniData.Sections["GENERAL"]["YSpace"] = Static.Datacenter.YAxisSpace.ToString();
                Static.Datacenter.IniData.Sections["GENERAL"]["MoveDelay"] = Static.Datacenter.MoveDelay.ToString();
                Static.Datacenter.IniData.Sections["GENERAL"]["DofusRetroExePath"] = Static.Datacenter.DofusRetroExePath;
                Static.Datacenter.SaveIni();
            });
        }

        #endregion

        #region Traitement des paquets


        #region Capture des paquets

        /// <summary>
        /// Thread permettant la capture des paquets en arrière plan
        /// </summary>
        private void StartPacketCapture()
        {
            var packetCaptureThread = new Thread(() =>
            {
                if (!NetworkHelper.LocalIpFound())
                {
                    Dispatcher.Invoke(() =>
                    {
                        BrdAutoSwitch.IsEnabled = false;
                        Status = "Ip locale non trouvée !";
                        BrdAutoSwitch.ToolTip = Status;
                    });

                }

                var captureDevice = (NpcapDevice)CaptureDeviceList.Instance.FirstOrDefault(
                    d => ((NpcapDevice)d).Addresses.Any(a => a.Addr.ipAddress?.ToString() == Static.Datacenter.LocalIp));

                if (captureDevice == null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        BrdAutoSwitch.IsEnabled = false;
                        Status = $"Carte réseau ayant l'IP {Static.Datacenter.LocalIp} non trouvée";
                        BrdAutoSwitch.ToolTip = Status;
                    });
                    return;
                }
                Static.Datacenter.CaptureDevice = captureDevice;
                if (Static.Datacenter.CaptureDevice == null)
                    return;
                Static.Datacenter.CaptureDevice.OnPacketArrival += CaptureDevice_OnPacketArrival;
                Static.Datacenter.CaptureDevice.Open();
                Dispatcher.Invoke(() =>
                {
                    Status = "Prêt";
                    (Resources["LogoGlow"] as Storyboard)?.Begin();
                });

                Static.Datacenter.CaptureDevice.Capture();
            })
            {
                IsBackground = true
            };

            packetCaptureThread.Start();
        }

        #endregion


        #region Capture des paquets

        /// <summary>
        /// Événement qui se produit lors de l'arrivée d'un paquet tcp.
        /// Pour que cela fonctionne, il faut avoir au préalable installé Npcap (2ème lien ci-dessous)
        /// </summary>
        /// <see cref="http://www.linux-france.org/prj/edu/archinet/systeme/ch01s03.html"/>
        /// <seealso cref="https://nmap.org/npcap/dist/npcap-1.00.exe"/>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CaptureDevice_OnPacketArrival(object sender, CaptureEventArgs e)
        {

            //On vérifie que le paquet est bien un paquet "Ethernet"
            var packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
            if (!(packet is EthernetPacket))
                return;

            //On essaie d'extraire la partie "IP" du paquet
            var ip = packet.Extract<IPPacket>();
            if (ip == null)
                return;


            var ipSource = ip.SourceAddress.ToString();
            var ipDestation = ip.DestinationAddress.ToString();

            //On vérifie que l'IP source et/ou destination est celle des serveurs Dofus Retro
            //Normalement, l'IP du serveur ne change pas. Si besoin, il faudra utiliser WireShark
            //ou autre alternative pour trouver l'IP
            if (!Static.Datacenter.Servers.Any(p => p.Ip == ipSource || p.Ip == ipDestation))
                return;

            var tcp = packet.Extract<TcpPacket>();
            if (tcp == null)
                return;

            //On transforme les données brutes (octets) en string
            var data = Encoding.UTF8.GetString(tcp.PayloadData);

            if (string.IsNullOrEmpty(data))
                return;


            //Une fois arrivé ici, on est sûr que le paquet contient des données et que c'est un paquet
            //pour Dofus Retro

            //On traite donc la donnée
            ProcessData(data, ipSource);
        }

        #endregion


        /// <summary>
        /// Traite le paquet en fonction du type de celui-ci
        /// </summary>
        /// <param name="data">Données du paquet sous forme de string</param>
        /// <param name="ipSource"></param>
        private void ProcessData(string data, string ipSource = null)
        {
            //Tout ce qui suit a été développé en faisant les actions sur Dofus puis en analysant les paquets WireShark
            //Pour vous entraîner, vous pouvez lancer WireShark, et coller le filtre ci-dessous (n'oubliez pas d'appuyer sur ENTREE pour valider)
            //pour avoir uniquement les paquets début de tour, message dans le chat, échange, invitation de groupe :
            //ip.addr == 172.65.214.172 && (eth contains "GTS" || eth contains "cMK" || eth contains "PIK" || eth contains "ERK")

            if (data.Contains("GTS") && Static.Datacenter.SwitchOnTurnStart)
                Dispatcher.Invoke(() => ProcessGameTurnStartMessage(data));

            else if (data.StartsWith("cMK"))
                Dispatcher.Invoke(() => ProcessChatMessage(data, ipSource));

            else if (data.StartsWith("ERK") && Static.Datacenter.SwitchOnExchangeRequest)
                Dispatcher.Invoke(() => ProcessExchangeInviteMessage(data));

            else if (data.StartsWith("PIK") && Static.Datacenter.SwitchOnGroupInvite)
                Dispatcher.Invoke(() => ProcessGroupInviteMessage(data));

            else if (data.Contains("ALK"))
                Dispatcher.Invoke(() => ProcessCharacterListMessage(data));

        }

        /// <summary>
        /// S'éxecute lorsque le message est un message de demande d'échange
        /// </summary>
        /// <param name="data"></param>
        private void ProcessCharacterListMessage(string data)
        {

            var msg = DataProcessor.Deserialize<List<CharacterListMessage>>(data);
            if (msg == null)
                return;

            Console.WriteLine(data);
            CharacterListReceived?.Invoke(this, msg);

        }


        /// <summary>
        /// Cette fonction nous sert uniquement pour enregistrer un personnage, et récupérer son ID unique.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ipSource"></param>
        private void ProcessChatMessage(string data, string ipSource)
        {
            //Si on ne réussi pas à transformer le string data en un objet "ChatMesssage",
            //c'est qu'il doit être mal formé, et donc nous n'en voulons pas
            //Pour + d'informations, voir la classe DataProcessor
            var msg = DataProcessor.Deserialize<ChatMessage>(data, ipSource);
            if (msg == null)
                return;

            //Si l'ID du personnage qui a envoyé le message a été trouvée dans la liste des personnages déjà existants,
            //cela veut dire que le personnage est déjà enregistré, on quitte donc la fonction
            if (Static.Datacenter.CharacterControllers.Any(p => p.Character.Id == msg.ActorId))
                return;

            //Si la fenêtre d'enregistrement de personnage est déjà ouverte (donc on enregistre déjà un personnage actuellement),
            //on quitte la fonction
            if (Static.Datacenter.AddCharacterView != null)
                return;

            Status = "Le personnage : " + msg.ActorName + " s'enregistre...";
            Static.Datacenter.AddCharacterView = new AddCharacterView(msg);
            Static.Datacenter.AddCharacterView.ShowDialog();

            //On recharge les personnages car si le nouveau personnage a été enregistré, il faut récupérer ses valeurs
            LoadCharactersData();
        }

        /// <summary>
        /// S'éxecute lorsque le message est un message de début de tour de personnage
        /// </summary>
        /// <param name="data"></param>
        private void ProcessGameTurnStartMessage(string data)
        {
            //On désérialise "data" en un objet de type "GameTurnStartMessage"
            var msg = DataProcessor.Deserialize<GameTurnStartMessage>(data);
            if (msg == null)
                return;

            //On regarde si l'ID du personnage pour lequel le tour débute, se trouve dans la liste de nos personnages
            //Autrement dit, si c'est le tour de l'un de mes personnages
            var character = Static.Datacenter.CharacterControllers.FirstOrDefault(p => p.Character.Id == msg.ActorId);
            if (character == null)
                return;

            Status = "C'est au tour de " + character.Character.Name;



            //On l'affiche et l'active
            ShowCharacterScreen(character);

        }


        /// <summary>
        /// S'éxecute lorsque le message est un message de demande d'échange
        /// </summary>
        /// <param name="data"></param>
        private void ProcessExchangeInviteMessage(string data)
        {

            var msg = DataProcessor.Deserialize<ExchangeInviteMessage>(data);
            if (msg == null)
                return;

            var characterInList = Static.Datacenter.CharacterControllers.FirstOrDefault(p => p.Character.Id == msg.ExchangeInviteTo);
            if (characterInList == null)
                return;

            Status = "Demande d'échange sur " + characterInList.Character.Name;


            //On l'affiche et l'active
            ShowCharacterScreen(characterInList);

        }

        /// <summary>
        /// S'éxecute lorsque le message est un message de d'invitation à un groupe
        /// </summary>
        /// <param name="data"></param>
        private void ProcessGroupInviteMessage(string data)
        {

            var msg = DataProcessor.Deserialize<GroupInviteMessage>(data);
            if (msg == null)
                return;

            var character = Static.Datacenter.CharacterControllers.FirstOrDefault(p => msg.InviteTo.Contains(p.Character.Name));
            if (character == null)
                return;

            Status = "Invitation à un groupe sur " + character.Character.Name;

            //On l'affiche et l'active
            ShowCharacterScreen(character);

        }

        #endregion

        #region Affichage de la fenêtre d'un personnage

        /// <summary>
        /// Met le processus du personnage au premier plan
        /// </summary>
        /// <param name="perso"></param>
        private static async void ShowCharacterScreen(CharacterController perso)
        {
            ////Ici j'utilise une petit programe développé avec AutoIt
            ////Je lui passe en paramètre le nom du personnage
            ////Le programme met juste le premier processus qui a pour nom de fenêtre, le nom du personnage, au premier plan
            ////Les sources sont disponibles dans Resources\activate_window.au3

            //if (Static.Datacenter.UseOldSwitchMode)
            //    Process.Start(@".\Resources\activate_window.exe", perso.Character.Name);
            //else
            if (perso.Character.Process != null)
                await Task.Run(() => AutoItX.WinActivate(perso.Character.Process.MainWindowHandle));




            //C'était l'ancienne fonction que j'utilisais, mais je ne sais pas pourquoi,
            //ça ne fonctionnait pas tout le temps


            //    if (PInvoke.IsIconic(perso.Character.Process.MainWindowHandle))
            //    {
            //        PInvoke.ShowWindow(perso.Character.Process.MainWindowHandle, 9);
            //    }

            //    PInvoke.ShowWindow(perso.Character.Process.MainWindowHandle, 3);
            //    PInvoke.SetForegroundWindow(perso.Character.Process.MainWindowHandle);
        }

        #endregion

        #region Monitoring des processus Dofus

        /// <summary>
        /// Événement qui se produit à chaque "Tick" du timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DofusProcessMonitorTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                //On utilise ici un dispatcher car le code se trouvant ici est exécuté dans un thread différent
                //du thread sur lequel s'exécute l'interface du logiciel
                Dispatcher.Invoke(MonitorDofusWindows);
            }
            catch (Exception)
            {
                //ignored
            }
        }

        /// <summary>
        /// Fonction qui permet d'associer un processus Dofus avec les personnages dans "CharacterList"
        /// </summary>
        private void MonitorDofusWindows()
        {
            BtnConnectEveryone.GetBindingExpression(IsEnabledProperty)?.UpdateTarget();
            BtnStartStopFollower.GetBindingExpression(IsEnabledProperty)?.UpdateTarget();

            //On récupère uniquement les processus Dofus Retro
            var dofusProcesses = ProcessHelper.GetDofusRetroProcesses();

            if (dofusProcesses.Count == 0)
                return;


            //Boucle sur la liste des processus
            foreach (var dofusProcess in dofusProcesses.ToList())
            {
                //On extrait le nom du personnage depuis le titre de MainWindow du processus
                var characterName = ProcessHelper.GetCharacterNameFromWindow(dofusProcess);

                //On regarde si on retrouve le personnage qui a le même PID que le processus actuel de la boucle
                var characterWithSamePid = Static.Datacenter.CharacterControllers.FirstOrDefault(p => p.Character.Process?.Id == dofusProcess.Id);



                if (string.IsNullOrEmpty(characterName))
                {
                    //Si le nom du personnage que l'on a extrait est égal à rien (donc si la fenêtre s'appelle uniquement "Dofus Retro v1.xxx"
                    //Cela veut dire que le personnage n'est plus connecté (car son nom s'affiche uniquement lorsqu'il est en jeu)
                    //mais que son processus est toujours présent (il peut être dans la liste des serveurs par exemple)
                    if (characterWithSamePid != null)
                        characterWithSamePid.Character.IsConnected = false; //On assigne donc la valeur false

                    //On continue la boucle sans descendre plus bas
                    continue;
                }


                //Arrivé ici, nous avons donc un processus ayant un titre qui contient le nom d'un personnage
                //Nous allons donc chercher dans la liste de nos persos, celui qui porte le nom que l'on vient de trouver
                var foundCharacterInList = Static.Datacenter.CharacterControllers.FirstOrDefault(p => p.Character.Name == characterName);

                //Si on n'a rien trouvé, on continue la boucle sans descendre plus bas

                if (foundCharacterInList?.Character == null)
                    continue;


                //Si le handle du processus qui est au premier plan actuellement est égal au handle du processus de notre personnage
                //alors on lui assigne IsWindowFocus à true (et c'est ce qui déclenche le fait que le nom du personnage devienne Jaune avec des paillettes vertes)
                foundCharacterInList.Character.IsWindowFocused = foundCharacterInList.Character.Process?.MainWindowHandle == PInvokeHelper.GetForegroundWindow();

                //Le personnage est forcément connecté si le code arrive jusqu'ici
                foundCharacterInList.Character.IsConnected = true;
                foundCharacterInList.Character.IsConnecting = false;

                //Si :
                //  - Le processus du personnage n'est pas déjà défini
                //  - ou Le PID de son processus est différent du PID du processus actuel de la boucle
                //  - ou que son MouseHook n'est pas défini
                // (le if ici est inversé par rapport au text ci-dessus, mais ça fait la même chose)
                if (foundCharacterInList.Character.Process != null &&
                    foundCharacterInList.Character.Process.Id == dofusProcess.Id &&
                    foundCharacterInList.MouseHook == null)
                    continue;
                {
                    //Si au moins une des conditions ci-dessus est remplie, on arrive ici

                    //On assigne le nouveau processus
                    foundCharacterInList.Character.Process = dofusProcess;
                    foundCharacterInList.Character.Process.EnableRaisingEvents = true; //Obligatoire pour écouter les évenements du processus, notamment l'évenement "Exited"

                    #region Character's Process Exited
                    //On assigne ici un évenement "lambda". Cela évite d'avoir à faire foundCharacterInList.Character.Process.Exited += Character_ProcessExited;
                    //et d'avoir l'évenement Character_ProcessExited de mis plus bas
                    foundCharacterInList.Character.Process.Exited += (sender, args) =>
                    {
                        //Si le processus a quitté, le personnage n'est plus connecté
                        foundCharacterInList.Character.IsConnected = false;
                        //On désassigne son processus
                        foundCharacterInList.Character.Process = null;
                        //Il ne peut donc évidemment plus être au premier plan
                        foundCharacterInList.Character.IsWindowFocused = false;

                        //On décoche le personnage du Follower
                        foundCharacterInList.Character.IsChecked = false;

                        //Si son MouseHook était déjà disposé, on quitte la fonction
                        if (foundCharacterInList.MouseHook == null)
                            return;

                        try
                        {
                            //Sinon, on dispose le MouseHook correctement pour éviter les fuites de mémoire
                            foundCharacterInList.MouseHook.Uninstall();
                            foundCharacterInList.MouseHook?.Dispose();
                            foundCharacterInList.MouseHook = null;
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    };
                    #endregion

                    //S'il a déjà un MouseHook de défini, on continue la boucle sans descendre plus bas
                    if (foundCharacterInList.MouseHook != null)
                        continue;

                    #region Character's Mouse Hook

                    //Sinon, on lui créé un nouveau MouseHook, qui va écouter uniquement les Cliques
                    foundCharacterInList.MouseHook = new MouseHook(dofusProcess.Id, MouseMessageTypes.Click);

                    //Comme pour le Process_Exited, on assigne un évenement lambda au click
                    foundCharacterInList.MouseHook.LeftButtonDown += async (sender, e) =>
                    {
                        //Le code ci-dessous, sera exécuté une fois qu'un des processus de nos personnages reçoit un clique

                        //Comme il nous fait minimum 2 personnages pour pour que l'un suivre l'autre
                        //on vérifie qu'il y en a bien au minimum 2
                        if (Static.Datacenter.CharacterControllers.Count(p => p.Character.IsChecked) < 2 || !IsFollowerRunning)
                            return;

                        //Lorsqu'on clique sur une fenêtre, celle-ci est automatiquement mise au premier plan
                        //On récupère donc le handle de la fenêtre qui a été cliquée
                        var clickedWindowHandle = PInvokeHelper.GetForegroundWindow();

                        //Ainsi que son RECT (correspond à la largeur, hauteur, et la position de la fenêtre sur l'écran
                        var clickedWindowRect = PInvokeHelper.GetWindowRectangle(clickedWindowHandle);


                        //Petite formule pour résoudre le problème des fenêtres Dofus ayant une taille différente
                        //Voir https://i.stack.imgur.com/Nd8z3.jpg
                        var offsetA = new Point(e.X - clickedWindowRect.Left, e.Y - clickedWindowRect.Top);
                        var xPct = offsetA.X / (clickedWindowRect.Right - clickedWindowRect.Left + 1);
                        var yPct = offsetA.Y / (clickedWindowRect.Bottom - clickedWindowRect.Top + 1);


                        //On boucle sur tous les personnages de la liste qui ont un processus de défini,
                        //qui sont cochés, et qui ne correspondent pas à la fenêtre sur laquelle nous venons déjà de cliquer
                        foreach (var character in Static.Datacenter.CharacterControllers.Where(c =>
                            c.Character.Process != null && c.Character.IsChecked &&
                            c.Character.Process.MainWindowHandle != PInvokeHelper.GetForegroundWindow()))
                        {
                            //Si un délai a été défini, on applique un délai de façon asynchrone
                            //(en arrière plan, pour ne pas bloquer la MainWindow, comme le ferait un Thread.Sleep(xx)
                            if (Static.Datacenter.MoveDelay > 0)
                                await Task.Delay(Static.Datacenter.MoveDelay);


                            //On récupère le RECT de la fenêtre du personnage actuel de la boucle
                            var characterRect =
                                PInvokeHelper.GetWindowRectangle(character.Character.Process.MainWindowHandle);

                            //On fait un calcul pour savoir où il faut cliquer sur la fenêtre, relativement par rapport
                            //à la fenêtre du premier personnage où l'on a cliqué (par exemple, si on a une fenêtre en plein écran, et l'autre en petit)
                            var xOffsetB = (int)((characterRect.Right - characterRect.Left + 1) * xPct);
                            var yOffsetB = (int)((characterRect.Bottom - characterRect.Top + 1) * yPct);
                            var offsetB = new Point(characterRect.Left + xOffsetB, characterRect.Top + yOffsetB);

                            //On lui envoi le clique
                            SendClick(character, (int)offsetB.X - Static.Datacenter.XAxisSpace, (int)offsetB.Y - Static.Datacenter.YAxisSpace);
                        }
                    };
                    foundCharacterInList.MouseHook.InstallAsync();

                    #endregion


                }
            }

        }

        #endregion

        #region Click sur les fenêtres en arrière plan

        /// <summary>
        /// Permet d'envoyer un clique à un processus avec en paramètre des coordonnées (x pour l'abscisse, et y pour l'ordonnée)
        /// </summary>
        /// <param name="characterController"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private static void SendClick(CharacterController characterController, int x, int y)
        {
            AutoItX.ControlClick(characterController.Character.Process.MainWindowHandle, characterController.Character.Process.MainWindowHandle, "Left", 1, x, y);
        }

        #endregion

        #region Follower

        /// <summary>
        /// Fonction qui démarre ou arrête le suivi des personnages
        /// </summary>
        private void StartStopFollowing()
        {
            var countSelectedPlayers = Static.Datacenter.CharacterControllers.Count(c => c.Character.IsChecked);

            if (countSelectedPlayers < 2)
            {
                Status = "Vous devez sélectionner au moins deux personnages";
                return;
            }

            if (!IsFollowerRunning)
            {
                IsFollowerRunning = true;
                Status = "Follower démarré";
            }
            else
            {
                Status = "Follower arrêté";
                IsFollowerRunning = false;
            }
        }

        #endregion

        #region Sélection/Désélection des personnages

        /// <summary>
        /// Événement qui se produit lorsqu'on coche "Sélectionner tout"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChkSelectAllCharacters_OnChecked(object sender, RoutedEventArgs e)
        {
            foreach (var perso in Static.Datacenter.CharacterControllers)
                if (perso.Character.Process != null)
                    perso.Character.IsChecked = true;
        }

        /// <summary>
        /// Événement qui se produit lorsqu'on décoche "Sélectionner tout"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChkSelectAllCharacters_OnUnchecked(object sender, RoutedEventArgs e)
        {
            foreach (var perso in Static.Datacenter.CharacterControllers)
                perso.Character.IsChecked = false;
        }

        #endregion

        #region Boutons

        private void BtnShowSettingsView_Click(object sender, RoutedEventArgs e)
        {
            var settingsView = new SettingsView();
            settingsView.ShowDialog();
        }

        private async void BtnConnectEveryone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var c in Static.Datacenter.CharacterControllers.Where(c => !c.Character.IsConnected).ToList())
            {
                using (var tokenSource = new CancellationTokenSource())
                    await Task.Run(async () => await Dispatcher.Invoke(() => c.Connect(tokenSource)), tokenSource.Token);
            }
        }

        private void BtnShowAccountManagerView_Click(object sender, RoutedEventArgs e)
        {
            var accountManagerView = new AccountManagerView();
            accountManagerView.ShowDialog();
        }

        private void BtnStartStopFollower_Click(object sender, RoutedEventArgs e) => StartStopFollowing();

        /// <summary>
        /// Installe NPcap
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnInstallNpcap_OnClick(object sender, RoutedEventArgs e)
        {
            Status = "Installation de Npcap...";
            await ProcessHelper.RunProgram("Resources\\npcap-1.00.exe");
            Status = "Prêt";
            IsNpcapInstalled = RegistryHelper.IsNpcapInstalled();

            if (IsNpcapInstalled)
                StartPacketCapture();

        }

        #endregion

        #region Site web

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://keytrap.fr");
        }

        #endregion

    }
}