using System.Windows;
using System.Windows.Controls;
using True_Mining_Desktop.Core;

namespace True_Mining_Desktop.Janelas
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
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

                if (!User.Settings.loadingSettings) { User.Settings.SettingsSaver(); }
            }

            if (TextBox_PaymentAddress.Text.Length == 34 && Tools.WalletAddressIsValid(TextBox_PaymentAddress.Text))
            {
                Button_CreateWallet.Visibility = Visibility.Hidden;
                walletIsChanged = true;
            }
            else
            {
                //Button_CreateWallet.Visibility = Visibility.Visible;
            }
        }

        private void Button_CreateWallet_Click(object sender, RoutedEventArgs e)
        {
            Tools.OpenLinkInBrowser("https://www.4stake.com/truemining");
        }

        private void RestartAsAdministrator_Click(object sender, RoutedEventArgs e)
        {
            Tools.RestartAsAdministrator();
        }
    }
}