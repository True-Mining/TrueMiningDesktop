using System;
using System.Windows;
using System.Windows.Input;

namespace TrueMiningDesktop.Janelas.Popups
{
    /// <summary>
    /// Lógica interna para Calculator.xaml
    /// </summary>
    public partial class ExchangeRates : Window
    {
        public ExchangeRates(decimal exchangeRatePontosRandomXToMiningCoin, decimal exchangeRatePontosKawPowToMiningCoin, decimal exchangeRatePontosEtchashToMiningCoin)
        {
            InitializeComponent();
            new System.Threading.Tasks.Task(() =>
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    loadingVisualElement.Visibility = Visibility.Visible;
                    AllContent.Visibility = Visibility.Hidden;

                    if (Janelas.Pages.Dashboard.loadingVisualElement.Visibility == Visibility.Visible)
                    {
                        Close();

                        MessageBox.Show("Wait for Dashboard load first"); return;
                    }
                    else
                    {
                        CoinName = User.Settings.User.PayCoin != null ? User.Settings.User.PayCoin.CoinName : "Coins";

                        BTCToCoinRate = decimal.Round(BTCToBTCRate / (ExternalApi.ExchangeOrderbooks.PaymentCoinBTC.buyLevels[0].price + ExternalApi.ExchangeOrderbooks.PaymentCoinBTC.buyLevels[0].price) / 2);
                        BTCToBTCRate = 1;
                        BTCToUSDRate = decimal.Round(ExternalApi.BitcoinPrice.BTCUSD, 2);

                        PointRandomXToCoinRate = decimal.Round(exchangeRatePontosRandomXToMiningCoin * (ExternalApi.ExchangeOrderbooks.XMRBTC.sellLevels[0].price / ExternalApi.ExchangeOrderbooks.PaymentCoinBTC.sellLevels[0].price), 6);
                        PointRandomXToBTCRate = decimal.Round((ExternalApi.ExchangeOrderbooks.XMRBTC.buyLevels[0].price + ExternalApi.ExchangeOrderbooks.XMRBTC.sellLevels[0].price) / 2 * exchangeRatePontosRandomXToMiningCoin / BTCToBTCRate, 8);
                        PointRandomXToUSDRate = decimal.Round((ExternalApi.ExchangeOrderbooks.XMRBTC.buyLevels[0].price + ExternalApi.ExchangeOrderbooks.XMRBTC.sellLevels[0].price) / 2 * exchangeRatePontosRandomXToMiningCoin / BTCToBTCRate * BTCToUSDRate, 6);

                        PointKawPowToCoinRate = decimal.Round(exchangeRatePontosKawPowToMiningCoin * (ExternalApi.ExchangeOrderbooks.RVNBTC.sellLevels[0].price / ExternalApi.ExchangeOrderbooks.PaymentCoinBTC.sellLevels[0].price), 6);
                        PointKawPowToBTCRate = decimal.Round((ExternalApi.ExchangeOrderbooks.RVNBTC.buyLevels[0].price + ExternalApi.ExchangeOrderbooks.RVNBTC.sellLevels[0].price) / 2 * exchangeRatePontosKawPowToMiningCoin / BTCToBTCRate, 8);
                        PointKawPowToUSDRate = decimal.Round((ExternalApi.ExchangeOrderbooks.RVNBTC.buyLevels[0].price + ExternalApi.ExchangeOrderbooks.RVNBTC.sellLevels[0].price) / 2 * exchangeRatePontosKawPowToMiningCoin / BTCToBTCRate * BTCToUSDRate, 6);

                        PointEtchashToCoinRate = decimal.Round(exchangeRatePontosEtchashToMiningCoin * (ExternalApi.ExchangeOrderbooks.ETCBTC.sellLevels[0].price / ExternalApi.ExchangeOrderbooks.PaymentCoinBTC.sellLevels[0].price), 6);
                        PointEtchashToBTCRate = decimal.Round((ExternalApi.ExchangeOrderbooks.ETCBTC.buyLevels[0].price + ExternalApi.ExchangeOrderbooks.ETCBTC.sellLevels[0].price) / 2 * exchangeRatePontosEtchashToMiningCoin / BTCToBTCRate, 8);
                        PointEtchashToUSDRate = decimal.Round((ExternalApi.ExchangeOrderbooks.ETCBTC.buyLevels[0].price + ExternalApi.ExchangeOrderbooks.ETCBTC.sellLevels[0].price) / 2 * exchangeRatePontosEtchashToMiningCoin / BTCToBTCRate * BTCToUSDRate, 6);

                        CoinToCoinRate = 1;
                        CoinToPointRandomXRate = decimal.Round(CoinToCoinRate / PointRandomXToCoinRate, 2);
                        CoinToPointKawPowRate = decimal.Round(CoinToCoinRate / PointKawPowToCoinRate, 2);
                        CoinToPointEtchashRate = decimal.Round(CoinToCoinRate / PointEtchashToCoinRate, 2);
                        CoinToBTCRate = decimal.Round((ExternalApi.ExchangeOrderbooks.PaymentCoinBTC.buyLevels[0].price + ExternalApi.ExchangeOrderbooks.PaymentCoinBTC.buyLevels[0].price) / 2 / BTCToBTCRate, 8);
                        CoinToUSDRate = decimal.Round((ExternalApi.ExchangeOrderbooks.PaymentCoinBTC.buyLevels[0].price + ExternalApi.ExchangeOrderbooks.PaymentCoinBTC.buyLevels[0].price) / 2 / BTCToBTCRate * BTCToUSDRate, 6);

                        loadingVisualElement.Visibility = Visibility.Hidden;
                        AllContent.Visibility = Visibility.Visible;

                        DataContext = null;
                        DataContext = this;
                    }
                });
            }).Start();
        }

        public string CoinName { get; set; }

        public decimal PointRandomXToCoinRate { get; set; } = 1;
        public decimal PointRandomXToBTCRate { get; set; } = 1;
        public decimal PointRandomXToUSDRate { get; set; } = 1;

        public decimal PointKawPowToCoinRate { get; set; } = 1;
        public decimal PointKawPowToBTCRate { get; set; } = 1;
        public decimal PointKawPowToUSDRate { get; set; } = 1;

        public decimal PointEtchashToCoinRate { get; set; } = 1;
        public decimal PointEtchashToBTCRate { get; set; } = 1;
        public decimal PointEtchashToUSDRate { get; set; } = 1;

        public decimal CoinToCoinRate { get; set; } = 1;
        public decimal CoinToPointRandomXRate { get; set; } = 1;
        public decimal CoinToPointKawPowRate { get; set; } = 1;
        public decimal CoinToPointEtchashRate { get; set; } = 1;
        public decimal CoinToBTCRate { get; set; } = 1;
        public decimal CoinToUSDRate { get; set; } = 1;

        public decimal BTCToCoinRate { get; set; } = 1;
        public decimal BTCToBTCRate { get; set; } = 1;
        public decimal BTCToUSDRate { get; set; } = 1;

        private void CloseButton_click(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private static bool clicado;
        private Point lm;

        public void Down(object sender, MouseButtonEventArgs e)
        {
            clicado = true;

            lm.X = System.Windows.Forms.Control.MousePosition.X;
            lm.Y = System.Windows.Forms.Control.MousePosition.Y;
            lm.X = Convert.ToInt16(Left) - lm.X;
            lm.Y = Convert.ToInt16(Top) - lm.Y;
        }

        public void Move(object sender, MouseEventArgs e)
        {
            if (clicado && e.LeftButton == MouseButtonState.Pressed)
            {
                Left = System.Windows.Forms.Control.MousePosition.X + lm.X;
                Top = System.Windows.Forms.Control.MousePosition.Y + lm.Y;
            }
            else { clicado = false; }
        }

        public void Up(object sender, MouseButtonEventArgs e)
        {
            clicado = false;
        }
    }
}