using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TrueMiningDesktop.Core;

namespace TrueMiningDesktop.Janelas.Popups
{
    /// <summary>
    /// Lógica interna para Calculator.xaml
    /// </summary>
    public partial class Calculator : Window
    {
        private readonly System.Timers.Timer timerUpdate = new(2000);

        public Calculator()
        {
            InitializeComponent();

            Closing += Calculator_Closing;

            timerUpdate.Elapsed += TimerUpdate_Elapsed;
            timerUpdate.AutoReset = false;
            timerUpdate.Start();

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                loadingVisualElement.Visibility = Visibility.Visible;
                AllContent.Visibility = Visibility.Hidden;
            });

            new System.Threading.Tasks.Task(() => TimerUpdate_Elapsed(null, null)).Start();
        }

        private void Calculator_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            timerUpdate.Stop();
        }

        private void TimerUpdate_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CPU_hashrate_decimal = Device.cpu.HashrateValue;
            OPENCL_hashrate_decimal = Device.opencl.HashrateValue;
            CUDA_hashrate_decimal = Device.cuda.HashrateValue;

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                CoinName = User.Settings.User.PayCoin != null ? User.Settings.User.PayCoin.CoinName : "Coins";

                CPU_algorithm = User.Settings.Device.cpu.Algorithm;
                if (CPU_hashrate_decimal <= 0) { CPUpannel.IsEnabled = false; CPU_hashrate_decimal = 0; } else { CPUpannel.IsEnabled = true; }
                CPU_hashrate = Device.cpu.HashrateString;

                if (Server.SoftwareParameters.ServerConfig != null && Server.SoftwareParameters.ServerConfig.MiningCoins.Any(coin => coin.Algorithm.Equals(User.Settings.Device.cpu.Algorithm, StringComparison.OrdinalIgnoreCase)))
                {
                    Server.MiningCoin miningCoin = Server.SoftwareParameters.ServerConfig.MiningCoins.First(coin => coin.Algorithm.Equals(User.Settings.Device.cpu.Algorithm, StringComparison.OrdinalIgnoreCase));

                    if ("RandomX".Equals(User.Settings.Device.cpu.Algorithm, StringComparison.OrdinalIgnoreCase))
                    {
                        CPUestimated_day_Coins = CPU_hashrate_decimal / 1000 * PoolAPI.XMR_nanopool.approximated_earnings.data.day.bitcoins.SubtractFee(Server.SoftwareParameters.ServerConfig.DynamicFee) / PoolAPI.Crex24.PaymentCoinBTC_Orderbook.buyLevels[0].price;
                        CPUestimated_day_Bitcoin = CPUestimated_day_Coins * (decimal)PoolAPI.Crex24.PaymentCoinBTC_Orderbook.sellLevels[0].price;
                    }
                    if ("KawPow".Equals(User.Settings.Device.cpu.Algorithm, StringComparison.OrdinalIgnoreCase))
                    {
                        CPUestimated_day_Coins = CPU_hashrate_decimal / 1000 * PoolAPI.RVN_nanopool.approximated_earnings.data.day.bitcoins.SubtractFee(Server.SoftwareParameters.ServerConfig.DynamicFee) / PoolAPI.Crex24.PaymentCoinBTC_Orderbook.buyLevels[0].price;
                        CPUestimated_day_Bitcoin = CPUestimated_day_Coins * (decimal)PoolAPI.Crex24.PaymentCoinBTC_Orderbook.sellLevels[0].price;
                    }
                    CPUestimated_day_USD = CPUestimated_day_Bitcoin * (decimal)PoolAPI.BitcoinPrice.BTCUSD;
                    CPUestimated_day_Coins_string = Math.Round(CPUestimated_day_Coins, 5).ToString();
                    CPUestimated_day_Sats_string = ((decimal)Math.Round(CPUestimated_day_Bitcoin, 8)).ToString();
                    CPUestimated_day_USD_string = Math.Round(CPUestimated_day_USD, 2).ToString();
                }

                OPENCL_algorithm = User.Settings.Device.opencl.Algorithm;
                if (OPENCL_hashrate_decimal <= 0) { OPENCLpannel.IsEnabled = false; OPENCL_hashrate_decimal = 0; } else { OPENCLpannel.IsEnabled = true; }
                OPENCL_hashrate = Device.opencl.HashrateString;

                if (Server.SoftwareParameters.ServerConfig != null && Server.SoftwareParameters.ServerConfig.MiningCoins.Any(coin => coin.Algorithm.Equals(User.Settings.Device.opencl.Algorithm, StringComparison.OrdinalIgnoreCase)))
                {
                    Server.MiningCoin miningCoin = Server.SoftwareParameters.ServerConfig.MiningCoins.First(coin => coin.Algorithm.Equals(User.Settings.Device.opencl.Algorithm, StringComparison.OrdinalIgnoreCase));

                    if ("RandomX".Equals(User.Settings.Device.opencl.Algorithm, StringComparison.OrdinalIgnoreCase))
                    {
                        OPENCLestimated_day_Coins = OPENCL_hashrate_decimal / 1000 * PoolAPI.XMR_nanopool.approximated_earnings.data.day.bitcoins.SubtractFee(Server.SoftwareParameters.ServerConfig.DynamicFee) / PoolAPI.Crex24.PaymentCoinBTC_Orderbook.buyLevels[0].price;
                        OPENCLestimated_day_Bitcoin = OPENCLestimated_day_Coins * (decimal)PoolAPI.Crex24.PaymentCoinBTC_Orderbook.sellLevels[0].price;
                    }
                    if ("KawPow".Equals(User.Settings.Device.opencl.Algorithm, StringComparison.OrdinalIgnoreCase))
                    {
                        OPENCLestimated_day_Coins = OPENCL_hashrate_decimal / 1000 * PoolAPI.RVN_nanopool.approximated_earnings.data.day.bitcoins.SubtractFee(Server.SoftwareParameters.ServerConfig.DynamicFee) / PoolAPI.Crex24.PaymentCoinBTC_Orderbook.buyLevels[0].price;
                        OPENCLestimated_day_Bitcoin = OPENCLestimated_day_Coins * (decimal)PoolAPI.Crex24.PaymentCoinBTC_Orderbook.sellLevels[0].price;
                    }
                    OPENCLestimated_day_USD = OPENCLestimated_day_Bitcoin * (decimal)PoolAPI.BitcoinPrice.BTCUSD;
                    OPENCLestimated_day_Coins_string = Math.Round(OPENCLestimated_day_Coins, 5).ToString();
                    OPENCLestimated_day_Sats_string = ((decimal)Math.Round(OPENCLestimated_day_Bitcoin, 8)).ToString();
                    OPENCLestimated_day_USD_string = Math.Round(OPENCLestimated_day_USD, 2).ToString();
                }

                CUDA_algorithm = User.Settings.Device.cuda.Algorithm;
                if (CUDA_hashrate_decimal <= 0) { CUDApannel.IsEnabled = false; CUDA_hashrate_decimal = 0; } else { CUDApannel.IsEnabled = true; }
                CUDA_hashrate = Device.cuda.HashrateString;

                if (Server.SoftwareParameters.ServerConfig != null && Server.SoftwareParameters.ServerConfig.MiningCoins.Any(coin => coin.Algorithm.Equals(User.Settings.Device.cuda.Algorithm, StringComparison.OrdinalIgnoreCase)))
                {
                    Server.MiningCoin miningCoin = Server.SoftwareParameters.ServerConfig.MiningCoins.First(coin => coin.Algorithm.Equals(User.Settings.Device.cuda.Algorithm, StringComparison.OrdinalIgnoreCase));

                    if ("RandomX".Equals(User.Settings.Device.cuda.Algorithm, StringComparison.OrdinalIgnoreCase))
                    {
                        CUDAestimated_day_Coins = CUDA_hashrate_decimal / 1000 * PoolAPI.XMR_nanopool.approximated_earnings.data.day.bitcoins.SubtractFee(Server.SoftwareParameters.ServerConfig.DynamicFee) / PoolAPI.Crex24.PaymentCoinBTC_Orderbook.buyLevels[0].price;
                        CUDAestimated_day_Bitcoin = CUDAestimated_day_Coins * (decimal)PoolAPI.Crex24.PaymentCoinBTC_Orderbook.sellLevels[0].price;
                    }
                    if ("KawPow".Equals(User.Settings.Device.cuda.Algorithm, StringComparison.OrdinalIgnoreCase))
                    {
                        CUDAestimated_day_Coins = CUDA_hashrate_decimal / 1000 * PoolAPI.RVN_nanopool.approximated_earnings.data.day.bitcoins.SubtractFee(Server.SoftwareParameters.ServerConfig.DynamicFee) / PoolAPI.Crex24.PaymentCoinBTC_Orderbook.buyLevels[0].price;
                        CUDAestimated_day_Bitcoin = CUDAestimated_day_Coins * (decimal)PoolAPI.Crex24.PaymentCoinBTC_Orderbook.sellLevels[0].price;
                    }
                    CUDAestimated_day_USD = CUDAestimated_day_Bitcoin * (decimal)PoolAPI.BitcoinPrice.BTCUSD;
                    CUDAestimated_day_Coins_string = Math.Round(CUDAestimated_day_Coins, 5).ToString();
                    CUDAestimated_day_Sats_string = ((decimal)Math.Round(CUDAestimated_day_Bitcoin, 8)).ToString();
                    CUDAestimated_day_USD_string = Math.Round(CUDAestimated_day_USD, 2).ToString();
                }

                timerUpdate.Enabled = true;
            });

            //if (CPU_hashrate_decimal <= 1 && OPENCL_hashrate_decimal <= 1 && CUDA_hashrate_decimal <= 1)
            //{
            //    Application.Current.Dispatcher.Invoke((Action)delegate
            //    {
            //        timerUpdate.Stop();
            //        this.Close();
            //    });
            //    if (this.Visibility == Visibility.Visible)
            //    {
            //        MessageBox.Show("Start mining and wait for the hashrate to appear."); return;
            //    }
            //}

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                loadingVisualElement.Visibility = Visibility.Hidden;
                AllContent.Visibility = Visibility.Visible;

                DataContext = null;
                DataContext = this;
            });
        }

        public string CoinName { get; set; }

        public decimal CPU_hashrate_decimal { get; set; } = 0;
        public string CPU_hashrate { get; set; }
        public string CPU_algorithm { get; set; }
        public decimal CPUestimated_day_Coins { get; set; }
        public decimal CPUestimated_day_Bitcoin { get; set; }
        public decimal CPUestimated_day_USD { get; set; }
        public string CPUestimated_day_Coins_string { get; set; }
        public string CPUestimated_day_Sats_string { get; set; }
        public string CPUestimated_day_USD_string { get; set; }

        public decimal OPENCL_hashrate_decimal { get; set; } = 0;
        public string OPENCL_hashrate { get; set; }
        public string OPENCL_algorithm { get; set; }
        public decimal OPENCLestimated_day_Coins { get; set; }
        public decimal OPENCLestimated_day_Bitcoin { get; set; }
        public decimal OPENCLestimated_day_USD { get; set; }
        public string OPENCLestimated_day_Coins_string { get; set; }
        public string OPENCLestimated_day_Sats_string { get; set; }
        public string OPENCLestimated_day_USD_string { get; set; }

        public decimal CUDA_hashrate_decimal { get; set; } = 0;
        public string CUDA_hashrate { get; set; }
        public string CUDA_algorithm { get; set; }
        public decimal CUDAestimated_day_Coins { get; set; }
        public decimal CUDAestimated_day_Bitcoin { get; set; }
        public decimal CUDAestimated_day_USD { get; set; }
        public string CUDAestimated_day_Coins_string { get; set; }
        public string CUDAestimated_day_Sats_string { get; set; }
        public string CUDAestimated_day_USD_string { get; set; }

        private void CloseButton_click(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
                Left = (System.Windows.Forms.Control.MousePosition.X + lm.X);
                Top = (System.Windows.Forms.Control.MousePosition.Y + lm.Y);
            }
            else { clicado = false; }
        }

        public void Up(object sender, MouseButtonEventArgs e)
        {
            clicado = false;
        }
    }
}