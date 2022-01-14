using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TrueMiningDesktop.Core;
using TrueMiningDesktop.ViewModel;
using Application = System.Windows.Application;
using ListView = System.Windows.Controls.ListView;
using MessageBox = System.Windows.MessageBox;
using MessageBoxOptions = System.Windows.MessageBoxOptions;
using UserControl = System.Windows.Controls.UserControl;

namespace TrueMiningDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static System.Windows.Forms.NotifyIcon NotifyIcon = new();

        public static event EventHandler TapeAllRequest;

        public MainWindow()
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            Tools.PropertyChanged += Tools_PropertyChanged;

            User.Settings.SettingsRecover();

            InitializeComponent();

            TapeAllRequest += CheckerPopup_TapeAllRequest;

            Device.cpu.what();

            _ = new Device();

            MenuMenu.Items.Add(new UserControlItemMenu(new ItemMenu("Home", Janelas.Pages.Home, PackIconKind.Home), this));
            MenuMenu.Items.Add(new UserControlItemMenu(new ItemMenu("Dashboard", Janelas.Pages.Dashboard, PackIconKind.ViewDashboard), this));
            MenuMenu.Items.Add(new UserControlItemMenu(new ItemMenu("Settings", Janelas.Pages.Settings, PackIconKind.Settings), this));

            List<SubItem> menuConfigHardware = new()
            {
                new SubItem("CPU", Janelas.Pages.SettingsCPU),
                new SubItem("AMD GPU", Janelas.Pages.SettingsOPENCL),
                new SubItem("NVIDIA GPU", Janelas.Pages.SettingsCUDA)
            };
            MenuMenu.Items.Add(new UserControlItemMenu(new ItemMenu("Hardware Config", menuConfigHardware, PackIconKind.Gpu), this));

            MenuMenu.Items.Add(new UserControlItemMenu(new ItemMenu("Other Stuff", new Janelas.Other(), PackIconKind.PlusBoxMultiple), this));

            Janelas.Pages.Home.TitleWrapPanel.MouseDown += Down;
            Janelas.Pages.Home.TitleWrapPanel.MouseMove += Move;
            Janelas.Pages.Home.TitleWrapPanel.MouseUp += Up;
            Janelas.Pages.Dashboard.TitleWrapPanel.MouseDown += Down;
            Janelas.Pages.Dashboard.TitleWrapPanel.MouseMove += Move;
            Janelas.Pages.Dashboard.TitleWrapPanel.MouseUp += Up;
            Janelas.Pages.Settings.TitleWrapPanel.MouseDown += Down;
            Janelas.Pages.Settings.TitleWrapPanel.MouseMove += Move;
            Janelas.Pages.Settings.TitleWrapPanel.MouseUp += Up;
            Janelas.Pages.SettingsCPU.TitleWrapPanel.MouseDown += Down;
            Janelas.Pages.SettingsCPU.TitleWrapPanel.MouseMove += Move;
            Janelas.Pages.SettingsCPU.TitleWrapPanel.MouseUp += Up;
            Janelas.Pages.SettingsOPENCL.TitleWrapPanel.MouseDown += Down;
            Janelas.Pages.SettingsOPENCL.TitleWrapPanel.MouseMove += Move;
            Janelas.Pages.SettingsOPENCL.TitleWrapPanel.MouseUp += Up;
            Janelas.Pages.SettingsCUDA.TitleWrapPanel.MouseDown += Down;
            Janelas.Pages.SettingsCUDA.TitleWrapPanel.MouseMove += Move;
            Janelas.Pages.SettingsCUDA.TitleWrapPanel.MouseUp += Up;

            Tools.KillMiners();

            Microsoft.Win32.SystemEvents.SessionEnding += SystemEvents_SessionEnding;

            Application.Current.Exit += Current_Exit;

            Tools.timerSystemAwake.Elapsed += Tools.AwakeSystem;
            Tools.timerSystemAwake.Start();
        }

        private void Tools_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                torIcon.Visibility = User.Settings.User.UseTorSharpOnMining ? Visibility.Visible : Visibility.Collapsed;
                torIcon.Opacity = Tools.TorSharpEnabled ? 1 : Tools.TorSharpProcessesRunning ? 0.7 : User.Settings.User.UseTorSharpOnMining ? 0.4 : 0;
            }));
        }

        private void Current_Exit(object sender, ExitEventArgs e)
        {
            try { Tools.TorSharpProxy.Stop(); } catch { }
        }

        private void SystemEvents_SessionEnding(object sender, Microsoft.Win32.SessionEndingEventArgs e)
        {
            Miner.EmergencyExit = true;
            Miner.StopMiner();
            Application.Current.Shutdown();
        }

        public static void DispararEvento()
        {
            TapeAllRequest.Invoke(null, null);
        }

        private void CheckerPopup_TapeAllRequest(object sender, EventArgs e)
        {
            if (Tools.CheckerPopup.Tape) { PanelTapeAll.Visibility = Visibility.Visible; } else { PanelTapeAll.Visibility = Visibility.Hidden; }
        }

        internal void SwitchScreen(UserControl userControl)
        {
            if (userControl != null)
            {
                TelaExibida.Children.Clear();
                TelaExibida.Children.Add(userControl);
            }
        }

        private void Menu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Clicado = false;

            if (((UserControlItemMenu)((ListView)sender).SelectedItem).ListViewMenu.Items.Count <= 0)
            {
                SwitchScreen(((UserControlItemMenu)((ListView)sender).SelectedItem).Screen);
            }

            bool expandItem = !((UserControlItemMenu)((ListView)sender).SelectedItem).ExpanderMenu.IsExpanded || ((UserControlItemMenu)((ListView)sender).SelectedItem) != ((UserControlItemMenu)((ListView)MenuMenu).SelectedItem);

            for (int item = ((ListView)sender).Items.Count - 1; 0 <= item; item--)
            {
                if (((UserControlItemMenu)((ListView)sender).Items[item]) == ((UserControlItemMenu)((ListView)sender).SelectedItem))
                {
                    ((UserControlItemMenu)((ListView)sender).Items[item]).ExpanderMenu.IsExpanded = true;
                }
                else
                {
                    ((UserControlItemMenu)((ListView)sender).Items[item]).ExpanderMenu.IsExpanded = false;
                }
            }

            Task.Run(() => User.Settings.SettingsSaver());
        }

        private void Menu_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (((ListView)sender).SelectedIndex >= 0)
            {
                Menu_SelectionChanged(sender, null);
            }
        }

        private void WrapPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Task.Run(() => User.Settings.SettingsSaver());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!User.Settings.User.LICENSE_read)
            {
                if (MessageBoxResult.Yes == MessageBox.Show("By using the software in any way you are agreeing to the license located at " + AppDomain.CurrentDomain.BaseDirectory + @"LICENSE.md" + "\n\nContinue?", "Accept True Mining License", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly))
                {
                    User.Settings.User.LICENSE_read = true;
                }
                else { Close(); }
            }

            try
            {
                CanvasSoftwareVersion.Text = "v" + Tools.GetAssemblyVersion();
                CanvasSoftwareVersion.TextDecorations = null;
            }
            catch { }

            TaskbarItemInfo = new System.Windows.Shell.TaskbarItemInfo();

            Tools.TryChangeTaskbarIconAsSettingsOrder();
            NotifyIcon.Visible = true;
            NotifyIcon.MouseDown += Notifier_MouseDown;

            Core.NextStart.Actions.Load();

            if (Core.NextStart.Actions.loadedNextStartInstructions.useThisInstructions)
            {
                if (Core.NextStart.Actions.loadedNextStartInstructions.startHiden)
                {
                    Hide();
                }
                else
                {
                    ShowInTaskbar = true;
                    Activate();
                    Focus();
                }
            }
            else
            {
                if (User.Settings.User.StartHide)
                {
                    Hide();
                }
                else
                {
                    ShowInTaskbar = true;
                    Activate();
                    Focus();
                }
            }

            if (!Tools.HaveADM) { Janelas.Pages.Home.RestartAsAdministratorButton.Visibility = Visibility.Visible; Janelas.Pages.Home.WarningsTextBlock.Text += "You haven't opened True Mining as an administrator. Restart as ADM is recommended and generally results in a better hashrate"; } else { Janelas.Pages.Home.RestartAsAdministratorButton.Visibility = Visibility.Collapsed; }

            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Diebold\Warsaw\unins000.exe")) { Janelas.Pages.Home.UninstallWarsawDiebold.Visibility = Visibility.Visible; Janelas.Pages.Home.WarningsTextBlock.Text += "\nWarsaw Diebold found on your system. It is highly recommended to uninstall this agent. Click \"Remove Warsaw\""; } else { Janelas.Pages.Home.UninstallWarsawDiebold.Visibility = Visibility.Collapsed; }

            MenuMenu.SelectedIndex = 0; // mostra a tela Home

            Tools.CheckerPopup = new Janelas.CheckerPopup("TrueMining");
            Tools.CheckerPopup.ShowDialog();

            try
            {
                List<string> list = new List<string>();

                foreach (Server.PaymentCoin paymentCoin in Server.SoftwareParameters.ServerConfig.PaymentCoins)
                {
                    if (!list.Contains(paymentCoin.CoinTicker + " - " + paymentCoin.CoinName))
                    {
                        list.Add(paymentCoin.CoinTicker + " - " + paymentCoin.CoinName);
                    }
                }
                User.Settings.User.Payment_CoinsList = list;

                Dispatcher.BeginInvoke((Action)(() =>
                {
                    Janelas.Pages.Home.DataContext = User.Settings.User;
                }));
            }
            catch { }

            Tools.TryChangeTaskbarIconAsSettingsOrder();

            Task.Run(() => User.Settings.SettingsSaver());

            if (Core.NextStart.Actions.loadedNextStartInstructions.useThisInstructions)
            {
                if (Core.NextStart.Actions.loadedNextStartInstructions.startMining)
                {
                    if (!string.IsNullOrEmpty(User.Settings.User.Payment_Wallet))
                    {
                        if (!Miner.IsMining) { Miner.StartMiner(); }
                    }
                }
            }
            else
            {
                if (User.Settings.User.AutostartMining)
                {
                    if (!string.IsNullOrEmpty(User.Settings.User.Payment_Wallet))
                    {
                        if (!Miner.IsMining) { Miner.StartMiner(); }
                    }
                }
            }
        }

        private void Notifier_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                ContextMenu menu = (ContextMenu)FindResource("NotifierContextMenu");
                menu.IsOpen = true;
                menu.StaysOpen = true;

                new Task(() => { Thread.Sleep(5000); Dispatcher.BeginInvoke((Action)(() => { menu.IsOpen = false; })); }).Start();
            }
        }

        private void Menu_Show(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
            ShowInTaskbar = true;
            Show();
            Activate();
            Focus();
        }

        private void Menu_Hide(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Menu_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (Miner.IsMining || Miner.IsTryingStartMining)
                {
                    if (MessageBoxResult.Yes == MessageBox.Show("Closing True Mining Desktop, mining will be stopped. Are you sure?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly))
                    {
                        this.Hide();

                        NotifyIcon.Visible = false;

                        Miner.XMRigMiners.ForEach(miner => { try { miner.IsMining = false; miner.MinerProcess.Kill(true); } catch { } });

                        Thread.Sleep(1500);
                    }
                    else { e.Cancel = true; return; }
                }

                if (Tools.CheckerPopup != null && Tools.CheckerPopup.Tape)
                {
                    if (MessageBoxResult.Yes != MessageBox.Show("True Mining is checking and updating software files. Closing True Mining Desktop, process will be stopped. Are you sure?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly))
                    { e.Cancel = true; return; }
                }

                Miner.EmergencyExit = true;

                User.Settings.SettingsSaver(true);

                Thread.Sleep(150);

                Tools.CheckerPopup.Close();

                Application.Current.Shutdown();
            }
            catch { }
        }

        public static bool Clicado;
        private Point lm;

        public void Down(object sender, MouseButtonEventArgs e)
        {
            Clicado = true;

            lm.X = System.Windows.Forms.Control.MousePosition.X;
            lm.Y = System.Windows.Forms.Control.MousePosition.Y;
            lm.X = Convert.ToInt16(Left) - lm.X;
            lm.Y = Convert.ToInt16(Top) - lm.Y;
        }

        public void Move(object sender, MouseEventArgs e)
        {
            if (Clicado && e.LeftButton == MouseButtonState.Pressed)
            {
                Left = (System.Windows.Forms.Control.MousePosition.X + lm.X);
                Top = (System.Windows.Forms.Control.MousePosition.Y + lm.Y);
            }
            else { Clicado = false; }
        }

        public void Up(object sender, MouseButtonEventArgs e)
        {
            Clicado = false;
        }

        private void PackIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void PackIcon_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            Hide();
            NotifyIcon.ShowBalloonTip(4000, "Hiding", "True Mining was hidden in the notification bar", System.Windows.Forms.ToolTipIcon.Info);
        }

        private void PackIcon_MouseDown_2(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            MenuMenu.ScrollIntoView(MenuMenu.Items[0]); //scroll to top
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (Clicado) Clicado = false;
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Clicado) Clicado = false;
        }

        private void Window_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (Clicado) Clicado = false;
        }

        private void TorIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("IP address: " + Tools.HttpGet("https://api.ipify.org", true));
        }
    }
}