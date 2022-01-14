using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TrueMiningDesktop.Core;
using TrueMiningDesktop.Server;

namespace TrueMiningDesktop.Janelas
{
    /// <summary>
    /// Interação lógica para Home.xam
    /// </summary>
    public partial class Home : UserControl
    {
        public bool PaymentInfoWasChanged { get; set; } = false;

        public Home()
        {
            InitializeComponent();
        }

        public void StartStopMining_Click(object sender, RoutedEventArgs e)
        {
            if (Miner.IsMining && !Miner.IsStoppingMining)
            {
                new Task(() => Miner.StopMiner()).Start();
            }
            else if (!Miner.IsMining && !Miner.IsTryingStartMining && !Miner.IsStoppingMining)
            {
                new Task(() => Miner.StartMiner()).Start();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                TextBox_PaymentAddress.Text = Regex.Replace(TextBox_PaymentAddress.Text, @"[^\w]", "");

                if (!string.IsNullOrEmpty(TextBox_PaymentAddress.Text) && SoftwareParameters.ServerConfig != null && SoftwareParameters.ServerConfig.PaymentCoins != null)
                {
                    List<PaymentCoin> possibleCoinsByCurrentAddress = SoftwareParameters.ServerConfig.PaymentCoins.Where(x => x.AddressPatterns.Any(x => Regex.IsMatch(TextBox_PaymentAddress.Text, x))).ToList<PaymentCoin>();

                    if (possibleCoinsByCurrentAddress.Count > 0)
                    {
                        Button_CreateWallet.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        Button_CreateWallet.Visibility = Visibility.Visible;
                    }

                    if (possibleCoinsByCurrentAddress.Count == 1)
                    {
                        ComboBox_PaymentCoin.SelectedItem = possibleCoinsByCurrentAddress.First().CoinTicker + " - " + possibleCoinsByCurrentAddress.First().CoinName;
                    }
                    else if (User.Settings.User.Payment_Wallet == TextBox_PaymentAddress.Text)
                    {
                        ComboBox_PaymentCoin.SelectedItem = User.Settings.User.PayCoin.CoinTicker + " - " + User.Settings.User.PayCoin.CoinName;
                    }
                    else
                    {
                        ComboBox_PaymentCoin.SelectedItem = null;
                    }

                    PaymentInfoWasChanged = true;
                    Pages.Dashboard.xWalletAddress.Content = TextBox_PaymentAddress.Text;
                }
                else
                {
                    if (User.Settings.User.PayCoin != null && User.Settings.User.PayCoin.AddressPatterns.Any(x => Regex.IsMatch(TextBox_PaymentAddress.Text, x)))
                    {
                        Button_CreateWallet.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        Button_CreateWallet.Visibility = Visibility.Visible;
                    }
                }
            }
            catch { }
        }

        private void Button_CreateWallet_Click(object sender, RoutedEventArgs e)
        {
            new ViewModel.PageCreateWallet().ShowDialog();
        }

        private void RestartAsAdministrator_Click(object sender, RoutedEventArgs e)
        {
            Tools.RestartApp(true);
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