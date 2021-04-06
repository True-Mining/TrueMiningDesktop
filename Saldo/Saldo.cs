using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using True_Mining_Desktop.Core;
using True_Mining_Desktop.Janelas;
using True_Mining_Desktop.PoolAPI;

namespace True_Mining_Desktop.Server
{
    public class Saldo
    {
        private System.Timers.Timer timerUpdateDashboard = new System.Timers.Timer(100);

        public Saldo()
        {
            Task.Run(() =>
            {
                Server.SoftwareParameters.Update(new Uri("https://truemining.online/TrueMiningDesktop.json"));

                while (User.Settings.loadingSettings) { Thread.Sleep(500); }

                timerUpdateDashboard.Elapsed += timerUpdateDashboard_Elapsed;

                timerUpdateDashboard.Start();
            });
        }

        private void timerUpdateDashboard_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            UpdateDashboardInfo();
        }

        public void UpdateDashboardInfo()
        {
            string warningMessage = "You need to enter a valid wallet address on the home screen so we can view your balances";
            if (Tools.WalletAddressIsValid(User.Settings.User.Payment_Wallet))
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    if (isUpdatingBalances)
                    {
                        Pages.Dashboard.loadingVisualElement.Visibility = Visibility.Visible;
                        Pages.Dashboard.DashboardContent.IsEnabled = false;
                    }
                    else
                    {
                        Pages.Dashboard.loadingVisualElement.Visibility = Visibility.Hidden;
                        Pages.Dashboard.DashboardContent.IsEnabled = true;
                    }

                    if (lastUpdated.Ticks < DateTime.Now.Ticks || Janelas.Pages.Home.walletIsChanged && Pages.Dashboard.IsLoaded)
                    {
                        Janelas.Pages.Home.walletIsChanged = false;
                        lastUpdated = DateTime.Now.AddMinutes(10);
                        UpdateBalances();
                    }

                    Pages.Dashboard.LabelNextPayout = ((int)23 - (int)DateTime.UtcNow.Hour) + " hours, " + ((int)59 - (int)DateTime.UtcNow.Minute) + " minutes";
                    Pages.Dashboard.LabelAccumulatedBalance = Math.Round(AccumulatedBalance_Points, 0) + " points ⇒ ≈ " + Math.Round(AccumulatedBalance_Coins, 4) + ' ' + User.Settings.User.Payment_Coin;
                    if (Pages.Dashboard.DashboardWarnings.Contains(warningMessage)) Janelas.Pages.Dashboard.DashboardWarnings.Remove(warningMessage); Pages.Dashboard.WarningWrapVisibility = Pages.Dashboard.DashboardWarnings.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                });
            }
            else
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    Pages.Dashboard.loadingVisualElement.Visibility = Visibility.Hidden;
                    Pages.Dashboard.DashboardContent.IsEnabled = true;

                    Pages.Dashboard.LabelNextPayout = ((int)23 - (int)DateTime.UtcNow.Hour) + " hours, " + ((int)59 - (int)DateTime.UtcNow.Minute) + " minutes";
                    Pages.Dashboard.LabelAccumulatedBalance = "??? points ⇒ ≈ ??? COINs";
                    if (!Pages.Dashboard.DashboardWarnings.Contains(warningMessage)) Janelas.Pages.Dashboard.DashboardWarnings.Add(warningMessage); Pages.Dashboard.WarningWrapVisibility = Pages.Dashboard.DashboardWarnings.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                });
            }
        }

        public bool isUpdatingBalances;

        public DateTime lastPayment = DateTime.UtcNow.AddHours(-(DateTime.UtcNow.Hour)).AddMinutes(-(DateTime.UtcNow.Minute));

        public double AccumulatedBalance_Points = 0;
        public double AccumulatedBalance_Coins = 0;

        public decimal conversionHashrateToPoints;
        public decimal exchangeRatePontosToMiningCoin;

        private static DateTime lastUpdated = DateTime.Now.AddMinutes(-10);

        public void UpdateBalances()
        {
            Task.Run(() =>
            {
                isUpdatingBalances = true;

                lastPayment = DateTime.UtcNow.AddHours(-DateTime.UtcNow.Hour).AddMinutes(-DateTime.UtcNow.Minute).AddSeconds(-DateTime.UtcNow.Second).AddMilliseconds(-DateTime.UtcNow.Millisecond);
                TimeSpan sinceLastPayment = new TimeSpan(DateTime.UtcNow.Ticks - lastPayment.Ticks);
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    if (isUpdatingBalances)
                    {
                        Pages.Dashboard.loadingVisualElement.Visibility = Visibility.Visible;
                        Pages.Dashboard.DashboardContent.IsEnabled = false;
                    }
                });
                while (!Tools.IsConnected()) { Thread.Sleep(5000); }
                try
                {
                    TruePayment.Nanopool.Objects.HashrateHistory hashrateHystory_user_raw = TruePayment.Nanopool.NanopoolData.GetHashrateHystory("xmr", Server.SoftwareParameters.ServerConfig.Pools[0].wallet_TM, User.Settings.User.Payment_Wallet);
                    TruePayment.Nanopool.Objects.HashrateHistory hashrateHystory_tm_raw = TruePayment.Nanopool.NanopoolData.GetHashrateHystory("xmr", Server.SoftwareParameters.ServerConfig.Pools[0].wallet_TM);
                    BitcoinPrice.FIAT_rates = JsonConvert.DeserializeObject<PoolAPI.Coins>(new WebClient().DownloadString("https://blockchain.info/ticker"));

                    Crex24.XMRBTC_Orderbook = JsonConvert.DeserializeObject<Orderbook>(new WebClient().DownloadString(new Uri("https://api.crex24.com/v2/public/orderBook?instrument=XMR-BTC")));
                    Crex24.MiningCoinBTC_Orderbook = JsonConvert.DeserializeObject<Orderbook>(new WebClient().DownloadString(new Uri("https://api.crex24.com/v2/public/orderBook?instrument=" + User.Settings.User.Payment_Coin + "-BTC")));
                    XMR_nanopool.approximated_earnings = JsonConvert.DeserializeObject<PoolAPI.approximated_earnings>(new WebClient().DownloadString(new Uri("https://api.nanopool.org/v1/xmr/approximated_earnings/1000")));

                    PoolAPI.XMR_nanopool.hashrateHistory_user.Clear();

                    foreach (TruePayment.Nanopool.Objects.Datum datum in hashrateHystory_user_raw.data)
                    {
                        if (!PoolAPI.XMR_nanopool.hashrateHistory_user.ContainsKey(datum.date))
                        {
                            try
                            {
                                PoolAPI.XMR_nanopool.hashrateHistory_user.Add(datum.date, datum.hashrate);
                            }
                            catch { }
                        }
                    }
                    foreach (TruePayment.Nanopool.Objects.Datum datum in hashrateHystory_tm_raw.data)
                    {
                        if (!PoolAPI.XMR_nanopool.hashrateHistory_tm.ContainsKey(datum.date))
                        {
                            try
                            {
                                PoolAPI.XMR_nanopool.hashrateHistory_tm.Add(datum.date, datum.hashrate);
                            }
                            catch { }
                        }
                    }
                }
                catch { }

            int sumHashrate_user =
            PoolAPI.XMR_nanopool.hashrateHistory_user
            .Where((KeyValuePair<int, int> value) =>
            value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
            .Select((KeyValuePair<int, int> value) => value.Value)
            .Aggregate(0, (acc, now) =>
            {
                return acc + now;
            });

                int sumHashrate_tm =
                PoolAPI.XMR_nanopool.hashrateHistory_tm
                .Where((KeyValuePair<int, int> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<int, int> value) => value.Value)
                .Aggregate(0, (acc, now) =>
                {
                    return acc + now;
                });

                decimal AverageHashrateUser = (decimal)Math.Floor((decimal)(sumHashrate_user / (sinceLastPayment.TotalMinutes / 10)));

                decimal AverageHashrateTrueMining = (decimal)Math.Floor((decimal)(sumHashrate_tm / (sinceLastPayment.TotalMinutes / 10)));

                decimal totalXMRmineradoTrueMining = XMR_nanopool.approximated_earnings.data.minute.coins * AverageHashrateTrueMining / 1000m * 10m * (decimal)(sinceLastPayment.TotalMinutes / 10);

                decimal XMRpraVirarBTC = totalXMRmineradoTrueMining;

                decimal XMRfinalPrice = 0;

                for (int i = 0; XMRpraVirarBTC > 0; i++)
                {
                    int I = i;
                    if (Crex24.XMRBTC_Orderbook.buyLevels[I].volume > XMRpraVirarBTC)
                    {
                        XMRpraVirarBTC -= Crex24.XMRBTC_Orderbook.buyLevels[I].volume;
                        XMRfinalPrice = Crex24.XMRBTC_Orderbook.buyLevels[I].price;
                    }
                    else
                    {
                        XMRpraVirarBTC -= Crex24.XMRBTC_Orderbook.buyLevels[I].volume;
                    }
                }

                decimal BTCpraVirarCOIN = totalXMRmineradoTrueMining * XMRfinalPrice;

                decimal COINfinalPrice = 0;

                for (int i = 0; BTCpraVirarCOIN > 0; i++)
                {
                    int I = i;
                    if (Crex24.MiningCoinBTC_Orderbook.sellLevels[I].volume > BTCpraVirarCOIN / Crex24.MiningCoinBTC_Orderbook.sellLevels[I].price)
                    {
                        BTCpraVirarCOIN -= Crex24.MiningCoinBTC_Orderbook.sellLevels[I].volume;
                        COINfinalPrice = Crex24.MiningCoinBTC_Orderbook.sellLevels[I].price;
                    }
                    else
                    {
                        BTCpraVirarCOIN -= Crex24.MiningCoinBTC_Orderbook.sellLevels[I].price * Crex24.MiningCoinBTC_Orderbook.sellLevels[I].volume;
                    }
                }

                AccumulatedBalance_Coins = (double)Math.Round((totalXMRmineradoTrueMining * (XMRfinalPrice / COINfinalPrice) * (AverageHashrateUser / AverageHashrateTrueMining) * (100 - 3) / 100), 4); //fee

                AccumulatedBalance_Points = sumHashrate_user / 840;

                string warningMessage = "Balance less than 1 DOGE will be paid once a week when you reach the minimum amount. Your balance will disappear from the dashboard, but it will still be saved in our system";
                string warningMessage2 = "Mined points take an average of 10-20 minutes to be displayed on the dashboard.";

                if (AccumulatedBalance_Coins == 0)
                {
                    if (!Pages.Dashboard.DashboardWarnings.Contains(warningMessage2)) Janelas.Pages.Dashboard.DashboardWarnings.Add(warningMessage2); Pages.Dashboard.WarningWrapVisibility = Pages.Dashboard.DashboardWarnings.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    if (Pages.Dashboard.DashboardWarnings.Contains(warningMessage2)) Janelas.Pages.Dashboard.DashboardWarnings.Remove(warningMessage2); Pages.Dashboard.WarningWrapVisibility = Pages.Dashboard.DashboardWarnings.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                }

                if (AccumulatedBalance_Coins <= 1 && User.Settings.User.Payment_Coin.Equals("doge", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Pages.Dashboard.DashboardWarnings.Contains(warningMessage)) Janelas.Pages.Dashboard.DashboardWarnings.Add(warningMessage); Pages.Dashboard.WarningWrapVisibility = Pages.Dashboard.DashboardWarnings.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    if (Pages.Dashboard.DashboardWarnings.Contains(warningMessage)) Janelas.Pages.Dashboard.DashboardWarnings.Remove(warningMessage); Pages.Dashboard.WarningWrapVisibility = Pages.Dashboard.DashboardWarnings.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                }

                try
                {
                    Pages.Dashboard.changeChartZoom(null, null);
                }
                catch { }

                isUpdatingBalances = false;
            });
        }
    }
}