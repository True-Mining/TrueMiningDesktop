using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TrueMiningDesktop.Core;
using TrueMiningDesktop.Janelas;
using TrueMiningDesktop.PoolAPI;

namespace TrueMiningDesktop.Server
{
    public class Saldo
    {
        private readonly System.Timers.Timer timerUpdateDashboard = new(1000);

        public Saldo()
        {
            Task.Run(() =>
            {
                Server.SoftwareParameters.Update(new Uri("https://truemining.online/TrueMiningDesktopDotnet5.json"));

                while (User.Settings.LoadingSettings) { Thread.Sleep(500); }

                timerUpdateDashboard.Elapsed += TimerUpdateDashboard_Elapsed;

                timerUpdateDashboard.Start();

                TimerUpdateDashboard_Elapsed(null, null);
            });
        }

        private void TimerUpdateDashboard_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                UpdateDashboardInfo();
            }
            catch { }
        }

        public void UpdateDashboardInfo()
        {
            string warningMessage = "You need to enter a valid wallet address on the home screen so we can view your balances";
            if (Tools.WalletAddressIsValid(User.Settings.User.Payment_Wallet))
            {
                Application.Current.Dispatcher.Invoke(delegate
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

                    if ((lastUpdated.Ticks < DateTime.Now.Ticks || Pages.Home.PaymentInfoWasChanged) && Pages.Dashboard.IsLoaded)
                    {
                        Janelas.Pages.Home.PaymentInfoWasChanged = false;
                        lastUpdated = DateTime.Now.AddMinutes(10);
                        try
                        {
                            UpdateBalances();
                        }
                        catch { lastUpdated = DateTime.Now.AddSeconds(-5); }
                    }

                    Pages.Dashboard.LabelNextPayout = 23 - DateTime.UtcNow.Hour + " hours, " + (59 - DateTime.UtcNow.Minute) + " minutes";

                    List<string> listPointslabel = new();
                    if (AccumulatedBalance_Points_xmr > 0) { listPointslabel.Add(Math.Round(AccumulatedBalance_Points_xmr, 0).ToString() + " RandomX-Points"); }
                    if (AccumulatedBalance_Points_rvn > 0) { listPointslabel.Add(Math.Round(AccumulatedBalance_Points_rvn, 0).ToString() + " KawPow-Points"); }

                    Pages.Dashboard.LabelAccumulatedBalance = (listPointslabel.Count == 0 ? "0 Points" : string.Join(", ", listPointslabel)) + " ⇒ ≈ " + Decimal.Round(AccumulatedBalance_Coins, 5) + ' ' + (User.Settings.User.Payment_Coin != null ? User.Settings.User.Payment_Coin.Split(' ', '-').Last() : "???");
                    if (Pages.Dashboard.DashboardWarnings.Contains(warningMessage)) Pages.Dashboard.DashboardWarnings.Remove(warningMessage); Pages.Dashboard.WarningWrapVisibility = Pages.Dashboard.DashboardWarnings.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                });
            }
            else
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    Pages.Dashboard.loadingVisualElement.Visibility = Visibility.Hidden;
                    Pages.Dashboard.DashboardContent.IsEnabled = true;

                    Pages.Dashboard.LabelNextPayout = 23 - DateTime.UtcNow.Hour + " hours, " + (59 - DateTime.UtcNow.Minute) + " minutes";
                    Pages.Dashboard.LabelAccumulatedBalance = "??? points ⇒ ≈ ??? COINs";
                    if (!Pages.Dashboard.DashboardWarnings.Contains(warningMessage)) Pages.Dashboard.DashboardWarnings.Add(warningMessage); Pages.Dashboard.WarningWrapVisibility = Pages.Dashboard.DashboardWarnings.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                });
            }
        }

        public bool isUpdatingBalances;

        public DateTime lastPayment = DateTime.UtcNow.AddHours(-DateTime.UtcNow.Hour).AddMinutes(-DateTime.UtcNow.Minute);

        public decimal AccumulatedBalance_Points_xmr = 0;
        public decimal AccumulatedBalance_Points_rvn = 0;
        public decimal AccumulatedBalance_Coins = 0;

        public decimal HashesPerPoint_xmr;
        public decimal exchangeRatePontosXmrToMiningCoin;
        public decimal HashesPerPoint_rvn;
        public decimal exchangeRatePontosRvnToMiningCoin;

        private static DateTime lastUpdated = DateTime.Now.AddMinutes(-10);

        private static readonly int secondsPerAveragehashrateReportInterval = 60 * 10;
        public decimal pointsMultiplier = secondsPerAveragehashrateReportInterval * 16;
        public int hashesToCompare = 1000;

        public void UpdateBalances()
        {
            Task.Run(() =>
            {
                isUpdatingBalances = true;

                lastPayment = DateTime.UtcNow.AddHours(-DateTime.UtcNow.Hour).AddMinutes(-DateTime.UtcNow.Minute).AddSeconds(-DateTime.UtcNow.Second).AddMilliseconds(-DateTime.UtcNow.Millisecond);
                TimeSpan sinceLastPayment = new(DateTime.UtcNow.Ticks - lastPayment.Ticks);
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
                    TruePayment.Nanopool.Objects.HashrateHistory hashrateHystory_xmr_user_raw = new();
                    TruePayment.Nanopool.Objects.HashrateHistory hashrateHystory_xmr_user_raw_new = new();
                    TruePayment.Nanopool.Objects.HashrateHistory hashrateHystory_xmr_tm_raw = new();

                    TruePayment.Nanopool.Objects.HashrateHistory hashrateHystory_rvn_user_raw = new();
                    TruePayment.Nanopool.Objects.HashrateHistory hashrateHystory_rvn_user_raw_new = new();
                    TruePayment.Nanopool.Objects.HashrateHistory hashrateHystory_rvn_tm_raw = new();

                    List<Task<Action>> getAPIsTask = new();

                    getAPIsTask.Add(new Task<Action>(() => { hashrateHystory_xmr_user_raw = TruePayment.Nanopool.NanopoolData.GetHashrateHystory("xmr", SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.CoinTicker.Equals("xmr", StringComparison.OrdinalIgnoreCase)).WalletTm, User.Settings.User.Payment_Wallet); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { hashrateHystory_xmr_user_raw_new = TruePayment.Nanopool.NanopoolData.GetHashrateHystory("xmr", SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.CoinTicker.Equals("xmr", StringComparison.OrdinalIgnoreCase)).WalletTm, User.Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + User.Settings.User.Payment_Wallet); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { hashrateHystory_xmr_tm_raw = TruePayment.Nanopool.NanopoolData.GetHashrateHystory("xmr", SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.CoinTicker.Equals("xmr", StringComparison.OrdinalIgnoreCase)).WalletTm); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { Crex24.XMRBTC_Orderbook = JsonConvert.DeserializeObject<Orderbook>(Tools.HttpGet("https://api.crex24.com/v2/public/orderBook?instrument=XMR-BTC")); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { XMR_nanopool.approximated_earnings = JsonConvert.DeserializeObject<PoolAPI.approximated_earnings>(Tools.HttpGet("https://api.nanopool.org/v1/xmr/approximated_earnings/" + hashesToCompare)); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { XMR_nanopool.sharecoef = JsonConvert.DeserializeObject<PoolAPI.share_coefficient>(Tools.HttpGet("https://api.nanopool.org/v1/xmr/pool/sharecoef")); return null; }));

                    getAPIsTask.Add(new Task<Action>(() => { hashrateHystory_rvn_user_raw_new = TruePayment.Nanopool.NanopoolData.GetHashrateHystory("rvn", SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.CoinTicker.Equals("rvn", StringComparison.OrdinalIgnoreCase)).WalletTm, User.Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + User.Settings.User.Payment_Wallet); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { hashrateHystory_rvn_tm_raw = TruePayment.Nanopool.NanopoolData.GetHashrateHystory("rvn", SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.CoinTicker.Equals("rvn", StringComparison.OrdinalIgnoreCase)).WalletTm); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { decimal rvnPrice = new CoinpaprikaAPI.Client().GetLatestOhlcForCoinAsync("rvn-ravencoin", "BTC").Result.Value.Last().Close; Crex24.RVNBTC_Orderbook = new PoolAPI.Orderbook() { buyLevels = new List<PoolAPI.BuyLevel>() { new PoolAPI.BuyLevel() { price = rvnPrice, volume = 1 } }, sellLevels = new List<PoolAPI.SellLevel>() { new PoolAPI.SellLevel() { price = rvnPrice, volume = 1 } } }; return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { RVN_nanopool.approximated_earnings = JsonConvert.DeserializeObject<PoolAPI.approximated_earnings>(Tools.HttpGet("https://api.nanopool.org/v1/rvn/approximated_earnings/" + hashesToCompare)); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { RVN_nanopool.sharecoef = JsonConvert.DeserializeObject<PoolAPI.share_coefficient>(Tools.HttpGet("https://api.nanopool.org/v1/rvn/pool/sharecoef")); return null; }));

                    getAPIsTask.Add(new Task<Action>(() => { Crex24.PaymentCoinBTC_Orderbook = JsonConvert.DeserializeObject<Orderbook>(Tools.HttpGet("https://api.crex24.com/v2/public/orderBook?instrument=" + User.Settings.User.PayCoin.CoinTicker.ToUpperInvariant() + "-BTC")); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { BitcoinPrice.BTCUSD = Math.Round(Convert.ToDecimal(((dynamic)JsonConvert.DeserializeObject(Tools.HttpGet("https://economia.awesomeapi.com.br/json/last/BTC-USD"))).BTCUSD.ask), 2); return null; }));

                    foreach (Task task in getAPIsTask)
                    {
                        task.Start();
                    }

                    Task.WaitAll(getAPIsTask.ToArray());

                    PoolAPI.XMR_nanopool.hashrateHistory_user.Clear();

                    if (hashrateHystory_xmr_user_raw.data != null)
                    {
                        foreach (TruePayment.Nanopool.Objects.Datum datum in hashrateHystory_xmr_user_raw.data)
                        {
                            if (User.Settings.User.PayCoin.CoinTicker != "DGB" && !PoolAPI.XMR_nanopool.hashrateHistory_user.ContainsKey(datum.date))
                            {
                                try
                                {
                                    PoolAPI.XMR_nanopool.hashrateHistory_user.Add(datum.date, datum.hashrate);
                                }
                                catch { }
                            }
                        }
                    }
                    if (hashrateHystory_xmr_user_raw_new.data != null)
                    {
                        foreach (TruePayment.Nanopool.Objects.Datum datum in hashrateHystory_xmr_user_raw_new.data)
                        {
                            if (!PoolAPI.XMR_nanopool.hashrateHistory_user.ContainsKey(datum.date))
                            {
                                try
                                {
                                    PoolAPI.XMR_nanopool.hashrateHistory_user.Add(datum.date, datum.hashrate);
                                }
                                catch { }
                            }
                            else
                            {
                                try
                                {
                                    PoolAPI.XMR_nanopool.hashrateHistory_user[datum.date] += datum.hashrate;
                                }
                                catch { }
                            }
                        }
                    }
                    if (hashrateHystory_xmr_tm_raw.data != null)
                    {
                        foreach (TruePayment.Nanopool.Objects.Datum datum in hashrateHystory_xmr_tm_raw.data)
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

                    PoolAPI.RVN_nanopool.hashrateHistory_user.Clear();

                    if (hashrateHystory_rvn_user_raw_new.data != null)
                    {
                        foreach (TruePayment.Nanopool.Objects.Datum datum in hashrateHystory_rvn_user_raw_new.data)
                        {
                            if (!PoolAPI.RVN_nanopool.hashrateHistory_user.ContainsKey(datum.date))
                            {
                                try
                                {
                                    PoolAPI.RVN_nanopool.hashrateHistory_user.Add(datum.date, datum.hashrate);
                                }
                                catch { }
                            }
                            else
                            {
                                try
                                {
                                    PoolAPI.RVN_nanopool.hashrateHistory_user[datum.date] += datum.hashrate;
                                }
                                catch { }
                            }
                        }
                    }
                    if (hashrateHystory_rvn_tm_raw.data != null)
                    {
                        foreach (TruePayment.Nanopool.Objects.Datum datum in hashrateHystory_rvn_tm_raw.data)
                        {
                            if (!PoolAPI.RVN_nanopool.hashrateHistory_tm.ContainsKey(datum.date))
                            {
                                try
                                {
                                    PoolAPI.RVN_nanopool.hashrateHistory_tm.Add(datum.date, datum.hashrate);
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { lastUpdated = DateTime.Now.AddSeconds(-10); }

                decimal sumHashrate_user_xmr =
                PoolAPI.XMR_nanopool.hashrateHistory_user
                .Where((KeyValuePair<int, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<int, decimal> value) => value.Value * secondsPerAveragehashrateReportInterval)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                decimal sumHashrate_tm_xmr =
                PoolAPI.XMR_nanopool.hashrateHistory_tm
                .Where((KeyValuePair<int, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<int, decimal> value) => value.Value * secondsPerAveragehashrateReportInterval)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                decimal sumHashrate_user_rvn =
                PoolAPI.RVN_nanopool.hashrateHistory_user
                .Where((KeyValuePair<int, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<int, decimal> value) => value.Value * secondsPerAveragehashrateReportInterval)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                decimal sumHashrate_tm_rvn =
                PoolAPI.RVN_nanopool.hashrateHistory_tm
                .Where((KeyValuePair<int, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<int, decimal> value) => value.Value * secondsPerAveragehashrateReportInterval)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));
                decimal totalXMRmineradoTrueMining = (decimal)XMR_nanopool.approximated_earnings.data.day.coins.SubtractFee(1) /*desconto da fee da pool que não está sendo inserida no cálculo*/ / (decimal)hashesToCompare / (decimal)TimeSpan.FromDays(1).TotalSeconds * (decimal)sumHashrate_tm_xmr;

                decimal XMRpraVirarBTC = (decimal)totalXMRmineradoTrueMining;

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

                decimal totalRVNmineradoTrueMining = (decimal)RVN_nanopool.approximated_earnings.data.day.coins.SubtractFee(1) /*desconto da fee da pool que não está sendo inserida no cálculo*/ / (decimal)hashesToCompare / (decimal)TimeSpan.FromDays(1).TotalSeconds * (decimal)sumHashrate_tm_rvn;

                decimal RVNpraVirarBTC = (decimal)totalRVNmineradoTrueMining;

                decimal RVNfinalPrice = 0;

                for (int i = 0; RVNpraVirarBTC > 0; i++)
                {
                    int I = i;
                    if (Crex24.RVNBTC_Orderbook.buyLevels[I].volume > RVNpraVirarBTC)
                    {
                        RVNpraVirarBTC -= Crex24.RVNBTC_Orderbook.buyLevels[I].volume;
                        RVNfinalPrice = Crex24.RVNBTC_Orderbook.buyLevels[I].price;
                    }
                    else
                    {
                        RVNpraVirarBTC -= Crex24.RVNBTC_Orderbook.buyLevels[I].volume;
                    }
                }

                decimal BTCpraVirarPaymentCoin = ((decimal)totalXMRmineradoTrueMining * XMRfinalPrice) + ((decimal)totalRVNmineradoTrueMining * RVNfinalPrice);

                decimal PaymentCoinFinalPrice = 0;

                for (int i = 0; BTCpraVirarPaymentCoin > 0; i++)
                {
                    int I = i;
                    if (Crex24.PaymentCoinBTC_Orderbook.sellLevels[I].volume > BTCpraVirarPaymentCoin / Crex24.PaymentCoinBTC_Orderbook.sellLevels[I].price)
                    {
                        BTCpraVirarPaymentCoin -= Crex24.PaymentCoinBTC_Orderbook.sellLevels[I].volume;
                        PaymentCoinFinalPrice = Crex24.PaymentCoinBTC_Orderbook.sellLevels[I].price;
                    }
                    else
                    {
                        BTCpraVirarPaymentCoin -= Crex24.PaymentCoinBTC_Orderbook.sellLevels[I].price * Crex24.PaymentCoinBTC_Orderbook.sellLevels[I].volume;
                    }
                }

                HashesPerPoint_xmr = XMR_nanopool.sharecoef.data * pointsMultiplier;
                AccumulatedBalance_Points_xmr = (decimal)sumHashrate_user_xmr / HashesPerPoint_xmr;
                exchangeRatePontosXmrToMiningCoin = XMR_nanopool.approximated_earnings.data.hour.coins.SubtractFee(1) / hashesToCompare / 60 / 60 * XMRfinalPrice / PaymentCoinFinalPrice * HashesPerPoint_xmr;

                HashesPerPoint_rvn = RVN_nanopool.sharecoef.data * pointsMultiplier;
                AccumulatedBalance_Points_rvn = (decimal)sumHashrate_user_rvn / HashesPerPoint_rvn;
                exchangeRatePontosRvnToMiningCoin = RVN_nanopool.approximated_earnings.data.hour.coins.SubtractFee(1) / hashesToCompare / 60 / 60 * RVNfinalPrice / PaymentCoinFinalPrice * HashesPerPoint_rvn;

                AccumulatedBalance_Coins = Decimal.Round((totalXMRmineradoTrueMining * Decimal.Divide(XMRfinalPrice, PaymentCoinFinalPrice) * Decimal.Divide(sumHashrate_user_xmr, sumHashrate_tm_xmr)).SubtractFee(Server.SoftwareParameters.ServerConfig.DynamicFee) + (totalRVNmineradoTrueMining * Decimal.Divide(RVNfinalPrice, PaymentCoinFinalPrice) * Decimal.Divide(sumHashrate_user_rvn, sumHashrate_tm_rvn)).SubtractFee(Server.SoftwareParameters.ServerConfig.DynamicFee), 5);

                string warningMessage = "Balance less than " + SoftwareParameters.ServerConfig.PaymentCoins.Find(x => Equals(x.CoinTicker, User.Settings.User.PayCoin.CoinTicker)).MinPayout.ToString() + User.Settings.User.PayCoin.CoinTicker.ToUpperInvariant() + " will be paid once a week when you reach the minimum amount. Your balance will disappear from the dashboard, but it will still be saved in our system";
                string warningMessage2 = "Mined points take an average of 10-20 minutes to be displayed on the dashboard.";

                if (AccumulatedBalance_Coins == 0)
                {
                    if (!Pages.Dashboard.DashboardWarnings.Contains(warningMessage2)) Janelas.Pages.Dashboard.DashboardWarnings.Add(warningMessage2); Pages.Dashboard.WarningWrapVisibility = Pages.Dashboard.DashboardWarnings.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    if (Pages.Dashboard.DashboardWarnings.Contains(warningMessage2)) Janelas.Pages.Dashboard.DashboardWarnings.Remove(warningMessage2); Pages.Dashboard.WarningWrapVisibility = Pages.Dashboard.DashboardWarnings.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                }

                if (SoftwareParameters.ServerConfig.PaymentCoins.Find(x => Equals(x.CoinTicker, User.Settings.User.PayCoin.CoinTicker)).MinPayout < AccumulatedBalance_Coins)
                {
                    if (!Pages.Dashboard.DashboardWarnings.Contains(warningMessage)) Janelas.Pages.Dashboard.DashboardWarnings.Add(warningMessage); Pages.Dashboard.WarningWrapVisibility = Pages.Dashboard.DashboardWarnings.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    if (Pages.Dashboard.DashboardWarnings.Contains(warningMessage)) Janelas.Pages.Dashboard.DashboardWarnings.Remove(warningMessage); Pages.Dashboard.WarningWrapVisibility = Pages.Dashboard.DashboardWarnings.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                }

                try
                {
                    Pages.Dashboard.ChangeChartZoom(null, null);
                }
                catch { }

                isUpdatingBalances = false;
            });
        }
    }
}