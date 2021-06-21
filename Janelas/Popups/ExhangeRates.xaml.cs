using System;
using System.Windows;
using System.Windows.Input;

namespace True_Mining_Desktop.Janelas.Popups
{
    /// <summary>
    /// Lógica interna para Calculator.xaml
    /// </summary>
    public partial class ExchangeRates : Window
    {
        public ExchangeRates(decimal exchangeRatePontosToMiningCoin)
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
                        this.Close();

                        MessageBox.Show("Wait for Dashboard load first"); return;
                    }
                    else
                    {
                        CoinName = User.Settings.User.Payment_Coin;

                        BTCToCoinRate = Decimal.Round(BTCToBTCRate / (PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price + PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price) / 2);
                        BTCToBTCRate = 1;
                        BTCToUSDRate = Decimal.Round(PoolAPI.BitcoinPrice.FIAT_rates.USD.Last, 2);

                        PointToCoinRate = Decimal.Round(exchangeRatePontosToMiningCoin, 5);
                        PointToBTCRate = Decimal.Round((PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price + PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price) / 2 * exchangeRatePontosToMiningCoin / BTCToBTCRate, 8);
                        PointToUSDRate = Decimal.Round((PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price + PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price) / 2 * exchangeRatePontosToMiningCoin / BTCToBTCRate * BTCToUSDRate, 5);

                        CoinToCoinRate = 1;
                        CoinToPointRate = Decimal.Round(CoinToCoinRate / PointToCoinRate, 5);
                        CoinToBTCRate = Decimal.Round((PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price + PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price) / 2 / BTCToBTCRate, 8);
                        CoinToUSDRate = Decimal.Round((PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price + PoolAPI.Crex24.MiningCoinBTC_Orderbook.buyLevels[0].price) / 2 / BTCToBTCRate * BTCToUSDRate, 5);

                        loadingVisualElement.Visibility = Visibility.Hidden;
                        AllContent.Visibility = Visibility.Visible;

                        DataContext = null;
                        DataContext = this;
                    }
                });
            }).Start();
        }

        public string CoinName { get; set; }

        public decimal PointToCoinRate { get; set; } = 1;
        public decimal PointToBTCRate { get; set; } = 1;
        public decimal PointToUSDRate { get; set; } = 1;

        public decimal CoinToCoinRate { get; set; } = 1;
        public decimal CoinToPointRate { get; set; } = 1;
        public decimal CoinToBTCRate { get; set; } = 1;
        public decimal CoinToUSDRate { get; set; } = 1;

        public decimal BTCToCoinRate { get; set; } = 1;
        public decimal BTCToBTCRate { get; set; } = 1;
        public decimal BTCToUSDRate { get; set; } = 1;

        private void CloseButton_click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close();
        }

        public static bool clicado = false;
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
            if (clicado && e.LeftButton == MouseButtonState.Pressed)
            {
                this.Left = (System.Windows.Forms.Control.MousePosition.X + this.lm.X);
                this.Top = (System.Windows.Forms.Control.MousePosition.Y + this.lm.Y);
            }
            else { clicado = false; }
        }

        public void Up(object sender, MouseButtonEventArgs e)
        {
            clicado = false;
        }
    }
}