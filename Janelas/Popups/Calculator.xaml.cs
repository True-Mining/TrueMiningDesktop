﻿using System;
using System.Windows;
using True_Mining_Desktop.Core;

namespace True_Mining_Desktop.Janelas.Popups
{
    /// <summary>
    /// Lógica interna para Calculator.xaml
    /// </summary>
    public partial class Calculator : Window
    {
        private decimal HashesPerPoint;
        private decimal ExchangeRatePontosToMiningCoin;

        private System.Timers.Timer timerUpdate = new System.Timers.Timer(2000);

        public Calculator(decimal hashesPerPoint, decimal exchangeRatePontosToMiningCoin)
        {
            InitializeComponent();

            this.Closing += Calculator_Closing;

            this.HashesPerPoint = (decimal)hashesPerPoint;
            this.ExchangeRatePontosToMiningCoin = (decimal)exchangeRatePontosToMiningCoin;

            timerUpdate.Elapsed += TimerUpdate_Elapsed;
            timerUpdate.AutoReset = false;
            timerUpdate.Start();

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                loadingVisualElement.Visibility = Visibility.Visible;
                AllContent.Visibility = Visibility.Hidden;
            });
        }

        private void Calculator_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            timerUpdate.Stop();
        }

        private void TimerUpdate_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!Miner.IsMining || Janelas.Pages.Dashboard.loadingVisualElement.Visibility == Visibility.Visible)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    timerUpdate.Stop();
                    this.Close();
                });
                MessageBox.Show("It was not possible to determine statistics for your mining. Start mining and wait for the hashrate to appear and become more stable for more accurate results."); return;
            }
            else
            {
                CPU_hashrate_decimal = Miner.GetHashrate("cpu", User.Settings.Device.cpu.Algorithm);
                OPENCL_hashrate_decimal = Miner.GetHashrate("opencl", User.Settings.Device.opencl.Algorithm);
                CUDA_hashrate_decimal = Miner.GetHashrate("cuda", User.Settings.Device.cuda.Algorithm);

                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    CoinName = User.Settings.User.Payment_Coin;

                    CPU_algorithm = User.Settings.Device.cpu.Algorithm;
                    if (CPU_hashrate_decimal == -1) { CPUpannel.IsEnabled = false; CPU_hashrate_decimal = 0; } else { CPUpannel.IsEnabled = true; }
                    CPU_hashrate = Math.Round(CPU_hashrate_decimal, 2).ToString() + " H/s";
                    CPUestimated_day_Coins = CPU_hashrate_decimal * (decimal)TimeSpan.FromDays(1).TotalSeconds / HashesPerPoint * ExchangeRatePontosToMiningCoin;
                    CPUestimated_day_Sats = CPUestimated_day_Coins * (decimal)PoolAPI.Crex24.MiningCoinBTC_Orderbook.sellLevels[0].price;
                    CPUestimated_day_USD = CPUestimated_day_Sats * (decimal)PoolAPI.BitcoinPrice.FIAT_rates.USD.Last;
                    CPUestimated_day_Coins_string = Math.Round(CPUestimated_day_Coins, 4).ToString();
                    CPUestimated_day_Sats_string = ((decimal)Math.Round(CPUestimated_day_Sats, 8)).ToString();
                    CPUestimated_day_USD_string = Math.Round(CPUestimated_day_USD, 2).ToString();

                    OPENCL_algorithm = User.Settings.Device.opencl.Algorithm;
                    if (OPENCL_hashrate_decimal == -1) { OPENCLpannel.IsEnabled = false; OPENCL_hashrate_decimal = 0; } else { OPENCLpannel.IsEnabled = true; }
                    OPENCL_hashrate = Math.Round(OPENCL_hashrate_decimal, 2).ToString() + " H/s";
                    OPENCLestimated_day_Coins = OPENCL_hashrate_decimal * (decimal)TimeSpan.FromDays(1).TotalSeconds / HashesPerPoint * ExchangeRatePontosToMiningCoin;
                    OPENCLestimated_day_Sats = OPENCLestimated_day_Coins * (decimal)PoolAPI.Crex24.MiningCoinBTC_Orderbook.sellLevels[0].price;
                    OPENCLestimated_day_USD = OPENCLestimated_day_Sats * (decimal)PoolAPI.BitcoinPrice.FIAT_rates.USD.Last;
                    OPENCLestimated_day_Coins_string = Math.Round(OPENCLestimated_day_Coins, 4).ToString();
                    OPENCLestimated_day_Sats_string = ((decimal)Math.Round(OPENCLestimated_day_Sats, 8)).ToString();
                    OPENCLestimated_day_USD_string = Math.Round(OPENCLestimated_day_USD, 2).ToString();

                    CUDA_algorithm = User.Settings.Device.cuda.Algorithm;
                    if (CUDA_hashrate_decimal == -1) { CUDApannel.IsEnabled = false; CUDA_hashrate_decimal = 0; } else { CUDApannel.IsEnabled = true; }
                    CUDA_hashrate = Math.Round(CUDA_hashrate_decimal, 2).ToString() + " H/s";
                    CUDAestimated_day_Coins = CUDA_hashrate_decimal * (decimal)TimeSpan.FromDays(1).TotalSeconds / HashesPerPoint * ExchangeRatePontosToMiningCoin;
                    CUDAestimated_day_Sats = CUDAestimated_day_Coins * (decimal)PoolAPI.Crex24.MiningCoinBTC_Orderbook.sellLevels[0].price;
                    CUDAestimated_day_USD = CUDAestimated_day_Sats * (decimal)PoolAPI.BitcoinPrice.FIAT_rates.USD.Last;
                    CUDAestimated_day_Coins_string = Math.Round(CUDAestimated_day_Coins, 4).ToString();
                    CUDAestimated_day_Sats_string = ((decimal)Math.Round(CUDAestimated_day_Sats, 8)).ToString();
                    CUDAestimated_day_USD_string = Math.Round(CUDAestimated_day_USD, 2).ToString();

                    timerUpdate.Enabled = true;
                });

                if (CPU_hashrate_decimal <= 0 && OPENCL_hashrate_decimal <= 0 && CUDA_hashrate_decimal <= 0)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        timerUpdate.Stop();
                        this.Close();
                    });
                    MessageBox.Show("It was not possible to determine statistics for your mining. Start mining and wait for the hashrate to appear and become more stable for more accurate results."); return;
                }

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

        public decimal CPU_hashrate_decimal { get; set; } = 0;
        public string CPU_hashrate { get; set; }
        public string CPU_algorithm { get; set; }
        public decimal CPUestimated_day_Coins { get; set; }
        public decimal CPUestimated_day_Sats { get; set; }
        public decimal CPUestimated_day_USD { get; set; }
        public string CPUestimated_day_Coins_string { get; set; }
        public string CPUestimated_day_Sats_string { get; set; }
        public string CPUestimated_day_USD_string { get; set; }

        public decimal OPENCL_hashrate_decimal { get; set; } = 0;
        public string OPENCL_hashrate { get; set; }
        public string OPENCL_algorithm { get; set; }
        public decimal OPENCLestimated_day_Coins { get; set; }
        public decimal OPENCLestimated_day_Sats { get; set; }
        public decimal OPENCLestimated_day_USD { get; set; }
        public string OPENCLestimated_day_Coins_string { get; set; }
        public string OPENCLestimated_day_Sats_string { get; set; }
        public string OPENCLestimated_day_USD_string { get; set; }

        public decimal CUDA_hashrate_decimal { get; set; } = 0;
        public string CUDA_hashrate { get; set; }
        public string CUDA_algorithm { get; set; }
        public decimal CUDAestimated_day_Coins { get; set; }
        public decimal CUDAestimated_day_Sats { get; set; }
        public decimal CUDAestimated_day_USD { get; set; }
        public string CUDAestimated_day_Coins_string { get; set; }
        public string CUDAestimated_day_Sats_string { get; set; }
        public string CUDAestimated_day_USD_string { get; set; }
    }
}