using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using WindowsInput;
using WindowsInput.Native;
using AutoIt;
using Dofus.Retro.Supertools.Core;
using Dofus.Retro.Supertools.Core.Extensions;
using Dofus.Retro.Supertools.Core.Helpers;
using Dofus.Retro.Supertools.Core.Messages;
using Dofus.Retro.Supertools.Windows;
using Winook;
using MessageBox = System.Windows.MessageBox;

namespace Dofus.Retro.Supertools.Controls
{
    /// <summary>
    /// Interaction logic for CharacterController.xaml
    /// </summary>
    public partial class CharacterController
    {
        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Character _character;
        public Character Character
        {
            get => _character;
            set
            {
                _character = value;
                OnPropertyChanged();
            }
        }

        public string Touche
        {
            get => (string)GetValue(ToucheProperty);
            set => SetValue(ToucheProperty, value);
        }

        public MouseHook MouseHook { get; set; }

        public static readonly DependencyProperty ToucheProperty =
            DependencyProperty.Register("Touche", typeof(string), typeof(CharacterController), new PropertyMetadata(""));

        #endregion

        public CharacterController()
        {
            InitializeComponent();
        }

        public CharacterController(Character character)
        {
            Character = character;
            InitializeComponent();
        }



        /// <summary>
        /// Fonction qui permet de connecter le personnage
        /// </summary>
        /// <returns></returns>
        public async Task Connect(CancellationTokenSource cancellationTokenSource)
        {
            if (Character.Server == null)
            {
                try
                {
                    MessageBox.Show("Vous devez spécifier un serveur associé à votre personnage", Character.Name, MessageBoxButton.OK);
                    cancellationTokenSource?.Cancel();
                    AbordConnect();
                    return;
                }
                catch (Exception)
                {
                    // ignored
                }
            }



            List<CharacterListMessage> characterListMessages = null;
            void OnCharacterListReceive(object sender, List<CharacterListMessage> clm)
            {
                characterListMessages = clm;
            }



            MainWindow.CharacterListReceived += OnCharacterListReceive;
            WindowHelper.MoveMainWindow();

            Character.IsConnecting = true;

            //On place la souris au milieu de l'écran pour empêcher à
            //l'utilisateur de cliquer autre part pendant la connexion du personnage
            //et donc potentiellement, faire échouer la connexion
            MouseHelper.CenterMouseOnScreen();


            var loginSatusWindow = new LoginStatus($"Connexion de {Character.Name} en cours... (Echap pour annuler)");
            loginSatusWindow.Show();


            void GlobalKeyDown(object sender, System.Windows.Forms.KeyEventArgs args)
            {
                if (args.KeyCode != Keys.Escape)
                    return;

                try
                {
                    if (!cancellationTokenSource.IsCancellationRequested)
                        cancellationTokenSource?.Cancel();
                }
                catch (Exception)
                {
                    // ignored
                }

                AbordConnect(loginSatusWindow);
                Static.Datacenter.MainWindow.GlobalMkHook.KeyDown -= GlobalKeyDown;
            }

            Static.Datacenter.MainWindow.GlobalMkHook.KeyDown += GlobalKeyDown;


            Character.Process = await this.StartDofusProcessAsync();

            if (Character.Process == null)
            {
                cancellationTokenSource?.Cancel();
                AbordConnect(loginSatusWindow);
                return;
            }

            Character.Process.EnableRaisingEvents = true;

            void ProcExitEventHandler(object s, EventArgs p)
            {
                try
                {
                    if (cancellationTokenSource?.Token != null)
                            cancellationTokenSource?.Cancel();
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            Character.Process.Exited += ProcExitEventHandler;


            MouseHelper.CenterMouseOnScreen();

            var loginResult = await LoginHelper.FindByOcr("SInscrire".FindOcrString(), cancellationTokenSource);

            if (loginResult == null || !string.IsNullOrEmpty(loginResult.Error))
            {
                loginSatusWindow.Status = "Impossible de trouver le bouton 'S'inscrire' sur l'écran. Abandon de la connexion auto...";
                await Task.Delay(3000);
                AbordConnect(loginSatusWindow);
                return;
            }


            //AutoItX.MouseMove(loginResult.X, loginResult.Y);

            var sim = new InputSimulator();
            sim.Keyboard.TextEntry(Character.Username);
            sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
            sim.Keyboard.TextEntry(Character.Password);
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);



            loginSatusWindow.Status = $"Recherche du serveur {Character.Server?.Name}...";

            MouseHelper.CenterMouseOnScreen();

            var serverListImage = await LoginHelper.FindByOcr("ServerList".FindOcrString(), cancellationTokenSource);
            if (serverListImage == null)
            {
                loginSatusWindow.Status = "Impossible de trouver la liste des serveurs sur l'écran. Vous pouvez continuer manuellement";
                await Task.Delay(3000);
                AbordConnect(loginSatusWindow);
                return;
            }

            if (!string.IsNullOrEmpty(serverListImage.Error))
            {
                loginSatusWindow.Status = serverListImage + ". Abandon de la connexion auto... Vous pouvez continuer manuellement";
                await Task.Delay(2000);
                AbordConnect();
                return;
            }


            var serverResult = await LoginHelper.FindByOcr(Character.Server?.Name.FindOcrString(), cancellationTokenSource);
            if (serverResult == null || !string.IsNullOrEmpty(serverResult.Error))
            {
                loginSatusWindow.Status = $"Impossible de trouver le serveur {Character.Server?.Name}. Vous pouvez continuer manuellement";
                await Task.Delay(3000);
                AbordConnect(loginSatusWindow);
                return;
            }

            if (string.IsNullOrEmpty(serverResult.Error))
            {
                AutoItX.MouseClick("LEFT", serverResult.X, serverResult.Y - 50, 2);
            }
            else
            {
                var serverResult2 = await LoginHelper.FindByImage(LoginHelper.Mode.Server, cancellationTokenSource, Character.Server?.Name);
                if (serverResult2 == null)
                {
                    loginSatusWindow.Status = $"Impossible de trouver le serveur {Character.Server?.Name}.. Vous pouvez continuer manuellement";
                    await Task.Delay(3000);
                    AbordConnect(loginSatusWindow);
                    return;
                }

                AutoItX.MouseClick("LEFT", serverResult2.X, serverResult2.Y, 2);

            }


            loginSatusWindow.Status = $"Recherche de {Character.Name}...";

            var playButtonFound = await LoginHelper.FindByImage(LoginHelper.Mode.Play, cancellationTokenSource);
            if (playButtonFound == null)
            {
                AbordConnect(loginSatusWindow);
                return;
            }


            WindowHelper.MaximizeWindow(Character);

            WindowHelper.SetMainWindowOpacity(0.1);

            await Task.Delay(1500);


            var characterFound = await LoginHelper.FindCharacter(Character.Name, cancellationTokenSource);
            if (characterFound != null && characterFound.Trim().Length > 2 && characterFound.Contains(";"))
            {

                var point = characterFound.Trim().Split(';');
                var x = Convert.ToInt32(point[0].Substring(0, point[0].IndexOf('.') > 0 ? point[0].IndexOf('.') : point[0].Length));
                var y = Convert.ToInt32(point[1].Substring(0, point[1].IndexOf('.') > 0 ? point[1].IndexOf('.') : point[1].Length));
                var w = Convert.ToInt32(point[2].Substring(0, point[2].IndexOf('.') > 0 ? point[2].IndexOf('.') : point[2].Length));
                var h = Convert.ToInt32(point[3].Substring(0, point[3].IndexOf('.') > 0 ? point[3].IndexOf('.') : point[3].Length));
                Console.WriteLine($@"Found character's name at {x};{y} w={w} h={h}");
                loginSatusWindow.Status = "C'est parti !";
                AutoItX.MouseClick("LEFT", x + w / 2, (y + h / 2) - 100, 2);
            }
            else
            {
                if (characterListMessages != null)
                {
                    var cpl = new CharactersPositionLocator();
                    cpl.Show();

                    var characterIndex = characterListMessages.FindIndex(c => c.ActorId == Character.Id);
                    if (characterIndex > -1)
                    {
                        var borderLocator = cpl.Borders[characterIndex];
                        var coordinates = borderLocator.PointToScreen(new Point(0d, 0d));

                        var centerPoint = borderLocator.GetCenter(coordinates);

                        AutoItX.MouseClick("Left", centerPoint.X + 30, centerPoint.Y + 100, 2);
                        Console.WriteLine($@"{centerPoint.X} : {centerPoint.Y}");
                        cpl.Close();
                    }
                    else
                    {
                        loginSatusWindow.Status = $"Impossible de trouver {Character.Name}. Vous pouvez continuer manuellement";
                        await Task.Delay(3000);
                        AbordConnect(loginSatusWindow);
                        return;
                    }


                }
                else
                {
                    loginSatusWindow.Status = $"Impossible de trouver {Character.Name}. Vous pouvez continuer manuellement";
                    await Task.Delay(3000);
                    AbordConnect(loginSatusWindow);
                    return;
                }

            }


            if (Character.Process != null)
                Character.Process.Exited -= ProcExitEventHandler;

            Character.Process = null;
            Character.IsConnecting = false;
            loginSatusWindow.Close();
            WindowHelper.SetMainWindowOpacity(100);
            Static.Datacenter.MainWindow.GlobalMkHook.KeyDown -= GlobalKeyDown;

        }


        public void AbordConnect(Window loginStatus = null, bool killProcess = false, Window charactersPositionLocator = null)
        {
            if (killProcess)
            {
                Character.Process?.Kill();
                Character.Process?.Dispose();
            }

            WindowHelper.SetMainWindowOpacity(100);
            charactersPositionLocator?.Close();
            loginStatus?.Close();
            Character.Process = null;
            Character.IsConnecting = false;
            Character.IsConnected = false;
        }

        private void SwitchToCharacterScreen()
        {
            if (Character.Process == null)
                return;

            PInvokeHelper.ShowWindow(Character.Process.MainWindowHandle, 3);
            PInvokeHelper.SetForegroundWindow(Character.Process.MainWindowHandle);
        }

        private void TxtName_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            SwitchToCharacterScreen();
        }

        private async void BtnConnectCharacter_Click(object sender, RoutedEventArgs e)
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
                await Task.Run(async () => await Dispatcher.Invoke(() => Connect(cancellationTokenSource)),
                    cancellationTokenSource.Token);

        }

        private void BtnConfigureAccount_Click(object sender, RoutedEventArgs e)
        {
            var acm = new AccountManagerView(Character);
            acm.ShowDialog();
        }

        private void TxtName_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
