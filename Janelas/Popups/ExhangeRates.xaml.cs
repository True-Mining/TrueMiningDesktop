using System;
using System.Windows;

namespace True_Mining_v4.Janelas.Popups
{
    /// <summary>
    /// Lógica interna para Calculator.xaml
    /// </summary>
    public partial class ExchangeRates : Window
    {
        private double PointsToCoins;

        private System.Timers.Timer timerUpdate = new System.Timers.Timer(2000);

        public ExchangeRates(double pointsToCoins)
        {
            InitializeComponent();

            PointsToCoins = pointsToCoins;

            timerUpdate.Elapsed += TimerUpdate_Elapsed;
            timerUpdate.AutoReset = false;
            timerUpdate.Start();

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                loadingVisualElement.Visibility = Visibility.Visible;
                AllContent.Visibility = Visibility.Hidden;
            });
        }

        private void TimerUpdate_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Janelas.Pages.Dashboard.loadingVisualElement.Visibility == Visibility.Visible)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    timerUpdate.Stop();
                    this.Close();
                });
                MessageBox.Show("Wait for Dashboard load first"); return;
            }
            else
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    CoinName = User.Settings.User.Payment_Coin;

                    BTCToCoinRate = Math.Round(BTCToBTCRate / Convert.ToDecimal(((PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price + PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price) / 2)));
                    BTCToBTCRate = 1;
                    BTCToUSDRate = Convert.ToDecimal(Math.Round(PoolAPI.BitcoinPrice.FIAT_rates.USD.Last, 2));

                    CoinToCoinRate = 1;
                    CoinToBTCRate = Math.Round((Convert.ToDecimal((PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price + PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price) / 2) / BTCToBTCRate), 8);
                    CoinToUSDRate = Math.Round((Convert.ToDecimal((PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price + PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price) / 2) / BTCToBTCRate * BTCToUSDRate), 5);

                    PointToCoinRate = Math.Round(Convert.ToDecimal(PointsToCoins), 5);
                    PointToBTCRate = Math.Round((Convert.ToDecimal((PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price + PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price) / 2) * Convert.ToDecimal(PointsToCoins) / BTCToBTCRate), 8);
                    PointToUSDRate = Math.Round((Convert.ToDecimal((PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price + PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price) / 2) * Convert.ToDecimal(PointsToCoins) / BTCToBTCRate * BTCToUSDRate), 5);
                });

                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    loadingVisualElement.Visibility = Visibility.Hidden;
                    AllContent.Visibility = Visibility.Visible;

                    DataContext = null;
                    DataContext = this;
                });
            }
        }

        public string CoinName { get; set; }

        public decimal PointToCoinRate { get; set; } = 1;
        public decimal PointToBTCRate { get; set; } = 1;
        public decimal PointToUSDRate { get; set; } = 1;

        public decimal CoinToCoinRate { get; set; } = 1;
        public decimal CoinToBTCRate { get; set; } = 1;
        public decimal CoinToUSDRate { get; set; } = 1;

        public decimal BTCToCoinRate { get; set; } = 1;
        public decimal BTCToBTCRate { get; set; } = 1;
        public decimal BTCToUSDRate { get; set; } = 1;
    }
}