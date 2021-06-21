using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using TrueMiningDesktop.Core;

namespace TrueMiningDesktop.Janelas
{
    /// <summary>
    /// Interação lógica para Home.xam
    /// </summary>
    public partial class Home : UserControl
    {
        public bool walletIsChanged { get; set; } = false;

        public Home()
        {
            InitializeComponent();
            DataContext = User.Settings.User;
        }

        public void StartStopMining_Click(object sender, RoutedEventArgs e)
        {
            if (Miner.IsMining)
            {
                Miner.StopMiner();
            }
            else
            {
                Miner.StartMiner();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TextBox_PaymentAddress.Text != null)
            {
                TextBox_PaymentAddress.Text = TextBox_PaymentAddress.Text.Replace(" ", "");

                if (TextBox_PaymentAddress.Text.StartsWith("R"))
                { User.Settings.User.Payment_Coin = "RDCT"; }
                if (TextBox_PaymentAddress.Text.StartsWith("D"))
                { User.Settings.User.Payment_Coin = "DOGE"; }

                if (!User.Settings.LoadingSettings) { User.Settings.SettingsSaver(); }
            }

            if (TextBox_PaymentAddress.Text.Length == 34 && Tools.WalletAddressIsValid(TextBox_PaymentAddress.Text))
            {
                Button_CreateWallet.Visibility = Visibility.Hidden;
                walletIsChanged = true;
                Janelas.Pages.Dashboard.xWalletAddress.Content = TextBox_PaymentAddress.Text;
            }
            else
            {
                Button_CreateWallet.Visibility = Visibility.Visible;
            }
        }

        private void Button_CreateWallet_Click(object sender, RoutedEventArgs e)
        {
            new ViewModel.PageCreateWallet().ShowDialog();
        }

        private void RestartAsAdministrator_Click(object sender, RoutedEventArgs e)
        {
            Tools.RestartAsAdministrator();
        }

        private void UninstallWarsawDiebold_Click(object sender, RoutedEventArgs e)
        {
            new System.Threading.Tasks.Task(() =>
            {
                if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Diebold\Warsaw\unins000.exe"))
                {
                    MessageBox.Show("True Mining Desktop will open uninstaller for you. Uninstall it");

                    System.Diagnostics.Process removeDiebold;
                    removeDiebold = new System.Diagnostics.Process() { StartInfo = new System.Diagnostics.ProcessStartInfo(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Diebold\Warsaw\unins000.exe") };

                    removeDiebold.Start();
                    removeDiebold.WaitForExit();
                }

                int count = 20;

                while (count > 0)
                {
                    count--;
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Janelas.Pages.Home.WarningsTextBlock.Text = Janelas.Pages.Home.WarningsTextBlock.Text.Replace("\nWarsaw Diebold found on your system. It is highly recommended to uninstall this agent. Click \"Remove Warsaw\"", "");

                        if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Diebold\Warsaw\unins000.exe")) { Janelas.Pages.Home.UninstallWarsawDiebold.Visibility = Visibility.Visible; Janelas.Pages.Home.WarningsTextBlock.Text += "\nWarsaw Diebold found on your system. It is highly recommended to uninstall this agent. Click \"Remove Warsaw\""; } else { Janelas.Pages.Home.UninstallWarsawDiebold.Visibility = Visibility.Collapsed; }
                    });
                    System.Threading.Thread.Sleep(5000);
                }
            }).Start();
        }
    }
}