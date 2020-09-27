using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using True_Mining_v4.Core;
using True_Mining_v4.ViewModel;
using Application = System.Windows.Application;
using ListView = System.Windows.Controls.ListView;
using MessageBox = System.Windows.MessageBox;
using MessageBoxOptions = System.Windows.MessageBoxOptions;
using UserControl = System.Windows.Controls.UserControl;

namespace True_Mining_v4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon nIcon = new System.Windows.Forms.NotifyIcon();

        public MainWindow()
        {
            User.Settings.SettingsRecover();

            if (!User.Settings.User.LICENSE_read)
            if (MessageBoxResult.Yes == MessageBox.Show("By using the software in any way you are agreeing to the license located at " + AppDomain.CurrentDomain.BaseDirectory + @"LICENSE.txt."+ "\n\nContinue?", "Accept True Mining License", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly))
            {
                    User.Settings.User.LICENSE_read = true;
            }
            else { this.Close(); }

            InitializeComponent();

            Device.cpu.what(); new Device();

            User.Settings.Device.cpu.AlgorithmsList.Add("RandomX");
            User.Settings.Device.opencl.AlgorithmsList.Add("RandomX");
            User.Settings.Device.cuda.AlgorithmsList.Add("RandomX");

            User.Settings.User.Payment_CoinsList.Add("DOGE");
            User.Settings.User.Payment_CoinsList.Add("RDCT");

            MenuMenu.Items.Add(new UserControlItemMenu(new ItemMenu("Home", Janelas.Pages.Home, PackIconKind.Home), this));
            MenuMenu.Items.Add(new UserControlItemMenu(new ItemMenu("Dashboard", Janelas.Pages.Dashboard, PackIconKind.ViewDashboard), this));
            MenuMenu.Items.Add(new UserControlItemMenu(new ItemMenu("Settings", Janelas.Pages.Settings, PackIconKind.Settings), this));

            var menuConfigHardware = new List<SubItem>();
            menuConfigHardware.Add(new SubItem("CPU", Janelas.Pages.SettingsCPU));
            menuConfigHardware.Add(new SubItem("AMD GPU", Janelas.Pages.SettingsOPENCL));
            menuConfigHardware.Add(new SubItem("NVIDIA GPU", Janelas.Pages.SettingsCUDA));
            MenuMenu.Items.Add(new UserControlItemMenu(new ItemMenu("Hardware Config", menuConfigHardware, PackIconKind.Gpu), this));

            Janelas.Pages.Home.TitleWrapPanel.MouseDown += this.Down;
            Janelas.Pages.Home.TitleWrapPanel.MouseMove += this.Move;
            Janelas.Pages.Home.TitleWrapPanel.MouseUp += this.Up;
            Janelas.Pages.Dashboard.TitleWrapPanel.MouseDown += this.Down;
            Janelas.Pages.Dashboard.TitleWrapPanel.MouseMove += this.Move;
            Janelas.Pages.Dashboard.TitleWrapPanel.MouseUp += this.Up;
            Janelas.Pages.Settings.TitleWrapPanel.MouseDown += this.Down;
            Janelas.Pages.Settings.TitleWrapPanel.MouseMove += this.Move;
            Janelas.Pages.Settings.TitleWrapPanel.MouseUp += this.Up;
            Janelas.Pages.SettingsCPU.TitleWrapPanel.MouseDown += this.Down;
            Janelas.Pages.SettingsCPU.TitleWrapPanel.MouseMove += this.Move;
            Janelas.Pages.SettingsCPU.TitleWrapPanel.MouseUp += this.Up;
            Janelas.Pages.SettingsOPENCL.TitleWrapPanel.MouseDown += this.Down;
            Janelas.Pages.SettingsOPENCL.TitleWrapPanel.MouseMove += this.Move;
            Janelas.Pages.SettingsOPENCL.TitleWrapPanel.MouseUp += this.Up;
            Janelas.Pages.SettingsCUDA.TitleWrapPanel.MouseDown += this.Down;
            Janelas.Pages.SettingsCUDA.TitleWrapPanel.MouseMove += this.Move;
            Janelas.Pages.SettingsCUDA.TitleWrapPanel.MouseUp += this.Up;

            Tools.KillMiners();
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
            if (((UserControlItemMenu)((ListView)sender).SelectedItem).ListViewMenu.Items.Count <= 0)
            {
                SwitchScreen(((UserControlItemMenu)((ListView)sender).SelectedItem).Screen);
            }

            bool expandItem = ((UserControlItemMenu)((ListView)sender).SelectedItem).ExpanderMenu.IsExpanded ? false : true;

            for (int item = ((ListView)sender).Items.Count - 1; 0 <= item; item--)
            {
                ((UserControlItemMenu)((ListView)sender).Items[item]).ExpanderMenu.IsExpanded = false;
            }

                ((UserControlItemMenu)((ListView)sender).SelectedItem).ExpanderMenu.IsExpanded = expandItem;

            User.Settings.SettingsSaver();

            MenuMenu.ScrollIntoView(MenuMenu.Items[0]); //scroll to top
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
            User.Settings.SettingsSaver();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (User.Settings.User.StartHide) { Hide(); }

            if (User.Settings.User.AutostartMining)
            {
                if (!String.IsNullOrEmpty(User.Settings.User.Payment_Wallet))
                {
                    if (!Miner.IsMining) { Miner.StartMiner(); }
                }
            }

            this.ShowInTaskbar = true;
            this.TaskbarItemInfo = new System.Windows.Shell.TaskbarItemInfo();

            nIcon.Icon = new System.Drawing.Icon("Resources/icone.ico");
            nIcon.Visible = true;
            nIcon.MouseDown -= notifier_MouseDown;
            nIcon.MouseDown += notifier_MouseDown;

            this.IsEnabled = false;
            if (User.Settings.User.StartHide) { new Janelas.CheckerPopup("TrueMining", true).ShowDialog(); } else { new Janelas.CheckerPopup("TrueMining").ShowDialog(); }
            this.IsEnabled = true;

            MenuMenu.SelectedIndex = 0; // mostra a tela Home
        }

        private void notifier_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                ContextMenu menu = (ContextMenu)this.FindResource("NotifierContextMenu");
                menu.IsOpen = true;
                menu.StaysOpen = true;
                new Task(() => { Thread.Sleep(5000); Application.Current.Dispatcher.Invoke((Action)delegate { menu.IsOpen = false; }); }).Start();
            }
        }

        private void Menu_Show(object sender, RoutedEventArgs e)
        {
            this.Show();
        }

        private void Menu_Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Miner.IsMining)
            {
                if (MessageBoxResult.Yes == MessageBox.Show("Closing True Mining, mining will be stopped. Are you sure?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly))
                {
                    Miner.StopMiner();
                }
                else { e.Cancel = true; }
            }
        }

        private bool clicado = false;
        private Point lm = new Point();

        public void Down(object sender, MouseButtonEventArgs e)
        {
            clicado = true;
            this.lm.X = System.Windows.Forms.Control.MousePosition.X;
            this.lm.Y = System.Windows.Forms.Control.MousePosition.Y;
            this.lm.X = Convert.ToInt16(this.Left) - this.lm.X;
            this.lm.Y = Convert.ToInt16(this.Top) - this.lm.Y;
        }

        public void Move(object sender, MouseEventArgs e)
        {
            if (clicado)
            {
                this.Left = (System.Windows.Forms.Control.MousePosition.X + this.lm.X);
                this.Top = (System.Windows.Forms.Control.MousePosition.Y + this.lm.Y);
            }
        }

        public void Up(object sender, MouseButtonEventArgs e)
        {
            clicado = false;
        }

        private void PackIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void PackIcon_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            this.Hide();
            nIcon.ShowBalloonTip(4000, "Hiding", "True Mining was hidden in the notification bar", System.Windows.Forms.ToolTipIcon.Info);
        }

        private void PackIcon_MouseDown_2(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            MenuMenu.ScrollIntoView(MenuMenu.Items[0]); //scroll to top
        }
    }
}