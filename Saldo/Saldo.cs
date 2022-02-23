using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TrueMiningDesktop.Core;
using TrueMiningDesktop.Janelas;
using TrueMiningDesktop.ExternalApi;

namespace TrueMiningDesktop.Server
{
    public class Saldo
    {
        private readonly System.Timers.Timer timerUpdateDashboard = new(1000);

        public Saldo()
        {
            Task.Run(() =>
            {
                Server.SoftwareParameters.Update(new Uri("https://truemining.online/config.json"));

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

                    decimal rvnPrice = new CoinpaprikaAPI.Client().GetLatestOhlcForCoinAsync("rvn-ravencoin", "BTC").Result.Value.Last().Close;

                    getAPIsTask.Add(new Task<Action>(() => { hashrateHystory_xmr_user_raw = TruePayment.Nanopool.NanopoolData.GetHashrateHystory("xmr", SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.CoinTicker.Equals("xmr", StringComparison.OrdinalIgnoreCase)).DepositAddressTrueMining, User.Settings.User.Payment_Wallet); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { hashrateHystory_xmr_user_raw_new = TruePayment.Nanopool.NanopoolData.GetHashrateHystory("xmr", SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.CoinTicker.Equals("xmr", StringComparison.OrdinalIgnoreCase)).DepositAddressTrueMining, User.Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + User.Settings.User.Payment_Wallet); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { hashrateHystory_xmr_tm_raw = TruePayment.Nanopool.NanopoolData.GetHashrateHystory("xmr", SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.CoinTicker.Equals("xmr", StringComparison.OrdinalIgnoreCase)).DepositAddressTrueMining); return null; }));


                    getAPIsTask.Add(new Task<Action>(() =>
                    {
                        bool orderbookStisfied = false;
                        try
                        {
                            if (!orderbookStisfied)
                            {
                                string webRequestResult = Tools.HttpGet("https://api.crex24.com/v2/public/orderBook?instrument=XMR-BTC");
                                ExternalApi.Orderbook orderbookObj = JsonConvert.DeserializeObject<ExternalApi.Orderbook>(webRequestResult);

                                ExchangeOrderbooks.XMRBTC = orderbookObj;
                                orderbookStisfied = true;
                            }
                        }
                        catch { }

                        try
                        {
                            if (!orderbookStisfied)
                            {
                                CoinpaprikaAPI.Client coinpaprikaAPI = new();

                                List<CoinpaprikaAPI.Entity.CoinInfo> coinList = coinpaprikaAPI.GetCoinsAsync().Result.Value;

                                decimal coinLastPrice = coinpaprikaAPI.GetLatestOhlcForCoinAsync(coinList.First(cName => cName.Symbol.Equals("xmr", StringComparison.OrdinalIgnoreCase) && cName.Name.Equals("monero", StringComparison.OrdinalIgnoreCase)).Id, "BTC").Result.Value.Last().Close;

                                ExternalApi.Orderbook orderbookObj = new() { buyLevels = new List<ExternalApi.BuyLevel>() { new ExternalApi.BuyLevel() { price = coinLastPrice, volume = 1 } }, sellLevels = new List<ExternalApi.SellLevel>() { new ExternalApi.SellLevel() { price = coinLastPrice, volume = 1 } } };

                                ExchangeOrderbooks.XMRBTC = orderbookObj;
                                orderbookStisfied = true;
                            }
                        }
                        catch { }
    ; return null;
                    }));

                    getAPIsTask.Add(new Task<Action>(() => { XMR_nanopool.approximated_earnings = JsonConvert.DeserializeObject<ExternalApi.approximated_earnings>(Tools.HttpGet("https://api.nanopool.org/v1/xmr/approximated_earnings/" + hashesToCompare), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture }); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { XMR_nanopool.sharecoef = JsonConvert.DeserializeObject<ExternalApi.share_coefficient>(Tools.HttpGet("https://api.nanopool.org/v1/xmr/pool/sharecoef"), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture }); return null; }));

                    getAPIsTask.Add(new Task<Action>(() => { hashrateHystory_rvn_user_raw_new = TruePayment.Nanopool.NanopoolData.GetHashrateHystory("rvn", SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.CoinTicker.Equals("rvn", StringComparison.OrdinalIgnoreCase)).DepositAddressTrueMining, User.Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + User.Settings.User.Payment_Wallet); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { hashrateHystory_rvn_tm_raw = TruePayment.Nanopool.NanopoolData.GetHashrateHystory("rvn", SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.CoinTicker.Equals("rvn", StringComparison.OrdinalIgnoreCase)).DepositAddressTrueMining); return null; }));
                    
                    getAPIsTask.Add(new Task<Action>(() =>
                    {
                        bool orderbookStisfied = false;
                        try
                        {
                            if (!orderbookStisfied)
                            {
                                string webRequestResult = Tools.HttpGet("https://api.crex24.com/v2/public/orderBook?instrument=RVN-BTC");
                                ExternalApi.Orderbook orderbookObj = JsonConvert.DeserializeObject<ExternalApi.Orderbook>(webRequestResult);

                                ExchangeOrderbooks.RVNBTC = orderbookObj;
                                orderbookStisfied = true;
                            }
                        }
                        catch { }

                        try
                        {
                            if (!orderbookStisfied)
                            {
                                CoinpaprikaAPI.Client coinpaprikaAPI = new();

                                List<CoinpaprikaAPI.Entity.CoinInfo> coinList = coinpaprikaAPI.GetCoinsAsync().Result.Value;

                                decimal coinLastPrice = coinpaprikaAPI.GetLatestOhlcForCoinAsync(coinList.First(cName => cName.Symbol.Equals("rvn", StringComparison.OrdinalIgnoreCase) && cName.Name.Equals("ravencoin", StringComparison.OrdinalIgnoreCase)).Id, "BTC").Result.Value.Last().Close;

                                ExternalApi.Orderbook orderbookObj = new() { buyLevels = new List<ExternalApi.BuyLevel>() { new ExternalApi.BuyLevel() { price = coinLastPrice, volume = 1 } }, sellLevels = new List<ExternalApi.SellLevel>() { new ExternalApi.SellLevel() { price = coinLastPrice, volume = 1 } } };

                                ExchangeOrderbooks.RVNBTC = orderbookObj;
                                orderbookStisfied = true;
                            }
                        }
                        catch { }
; return null;
                    }));
                    getAPIsTask.Add(new Task<Action>(() => { RVN_nanopool.approximated_earnings = JsonConvert.DeserializeObject<ExternalApi.approximated_earnings>(Tools.HttpGet("https://api.nanopool.org/v1/rvn/approximated_earnings/" + hashesToCompare), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture }); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { RVN_nanopool.sharecoef = JsonConvert.DeserializeObject<ExternalApi.share_coefficient>(Tools.HttpGet("https://api.nanopool.org/v1/rvn/pool/sharecoef"), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture }); return null; }));

                    getAPIsTask.Add(new Task<Action>(() =>
                    {
                        bool orderbookStisfied = false;
                        try
                        {
                            if (!orderbookStisfied && User.Settings.User.PayCoin.MarketDataSources.Any(nameDataSource => nameDataSource.Equals("Crex24", StringComparison.OrdinalIgnoreCase)))
                            {
                                string webRequestResult = Tools.HttpGet("https://api.crex24.com/v2/public/orderBook?instrument=" + User.Settings.User.PayCoin.CoinTicker.ToUpper() + "-BTC");
                                ExternalApi.Orderbook orderbookObj = JsonConvert.DeserializeObject<ExternalApi.Orderbook>(webRequestResult);

                                ExchangeOrderbooks.PaymentCoinBTC = orderbookObj;
                                orderbookStisfied = true;
                            }
                        }
                        catch { }

                        try
                        {
                            if (!orderbookStisfied && User.Settings.User.PayCoin.MarketDataSources.Any(nameDataSource => nameDataSource.Equals("Coinpaprika", StringComparison.OrdinalIgnoreCase)))
                            {
                                CoinpaprikaAPI.Client coinpaprikaAPI = new();

                                List<CoinpaprikaAPI.Entity.CoinInfo> coinList = coinpaprikaAPI.GetCoinsAsync().Result.Value;

                                decimal coinLastPrice = coinpaprikaAPI.GetLatestOhlcForCoinAsync(coinList.First(cName => cName.Symbol.Equals(User.Settings.User.PayCoin.CoinTicker, StringComparison.OrdinalIgnoreCase) && cName.Name.Equals(User.Settings.User.PayCoin.CoinName, StringComparison.OrdinalIgnoreCase)).Id, "BTC").Result.Value.Last().Close;

                                ExternalApi.Orderbook orderbookObj = new() { buyLevels = new List<ExternalApi.BuyLevel>() { new ExternalApi.BuyLevel() { price = coinLastPrice, volume = 1 } }, sellLevels = new List<ExternalApi.SellLevel>() { new ExternalApi.SellLevel() { price = coinLastPrice, volume = 1 } } };

                                ExchangeOrderbooks.PaymentCoinBTC = orderbookObj;
                                orderbookStisfied = true;
                            }
                        }
                        catch { }
                        ; return null;
                    }));

                    getAPIsTask.Add(new Task<Action>(() => { BitcoinPrice.BTCUSD = Math.Round(Convert.ToDecimal(((dynamic)JsonConvert.DeserializeObject(Tools.HttpGet("https://economia.awesomeapi.com.br/json/last/BTC-USD"), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture })).BTCUSD.ask), 2); return null; }));

                    foreach (Task task in getAPIsTask)
                    {
                        task.Start();
                    }

                    Task.WaitAll(getAPIsTask.ToArray());

                    ExternalApi.XMR_nanopool.hashrateHistory_user.Clear();

                    if (hashrateHystory_xmr_user_raw.data != null)
                    {
                        foreach (TruePayment.Nanopool.Objects.Datum datum in hashrateHystory_xmr_user_raw.data)
                        {
                            if (User.Settings.User.PayCoin.CoinTicker != "DGB" && !ExternalApi.XMR_nanopool.hashrateHistory_user.ContainsKey(datum.date))
                            {
                                try
                                {
                                    ExternalApi.XMR_nanopool.hashrateHistory_user.Add(datum.date, datum.hashrate);
                                }
                                catch { }
                            }
                        }
                    }
                    if (hashrateHystory_xmr_user_raw_new.data != null)
                    {
                        foreach (TruePayment.Nanopool.Objects.Datum datum in hashrateHystory_xmr_user_raw_new.data)
                        {
                            if (!ExternalApi.XMR_nanopool.hashrateHistory_user.ContainsKey(datum.date))
                            {
                                try
                                {
                                    ExternalApi.XMR_nanopool.hashrateHistory_user.Add(datum.date, datum.hashrate);
                                }
                                catch { }
                            }
                            else
                            {
                                try
                                {
                                    ExternalApi.XMR_nanopool.hashrateHistory_user[datum.date] += datum.hashrate;
                                }
                                catch { }
                            }
                        }
                    }
                    if (hashrateHystory_xmr_tm_raw.data != null)
                    {
                        foreach (TruePayment.Nanopool.Objects.Datum datum in hashrateHystory_xmr_tm_raw.data)
                        {
                            if (!ExternalApi.XMR_nanopool.hashrateHistory_tm.ContainsKey(datum.date))
                            {
                                try
                                {
                                    ExternalApi.XMR_nanopool.hashrateHistory_tm.Add(datum.date, datum.hashrate);
                                }
                                catch { }
                            }
                        }
                    }

                    ExternalApi.RVN_nanopool.hashrateHistory_user.Clear();

                    if (hashrateHystory_rvn_user_raw_new.data != null)
                    {
                        foreach (TruePayment.Nanopool.Objects.Datum datum in hashrateHystory_rvn_user_raw_new.data)
                        {
                            if (!ExternalApi.RVN_nanopool.hashrateHistory_user.ContainsKey(datum.date))
                            {
                                try
                                {
                                    ExternalApi.RVN_nanopool.hashrateHistory_user.Add(datum.date, datum.hashrate);
                                }
                                catch { }
                            }
                            else
                            {
                                try
                                {
                                    ExternalApi.RVN_nanopool.hashrateHistory_user[datum.date] += datum.hashrate;
                                }
                                catch { }
                            }
                        }
                    }
                    if (hashrateHystory_rvn_tm_raw.data != null)
                    {
                        foreach (TruePayment.Nanopool.Objects.Datum datum in hashrateHystory_rvn_tm_raw.data)
                        {
                            if (!ExternalApi.RVN_nanopool.hashrateHistory_tm.ContainsKey(datum.date))
                            {
                                try
                                {
                                    ExternalApi.RVN_nanopool.hashrateHistory_tm.Add(datum.date, datum.hashrate);
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { lastUpdated = DateTime.Now.AddSeconds(-10); }

                decimal sumHashrate_user_xmr =
                ExternalApi.XMR_nanopool.hashrateHistory_user
                .Where((KeyValuePair<int, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<int, decimal> value) => value.Value * secondsPerAveragehashrateReportInterval)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                decimal sumHashrate_tm_xmr =
                ExternalApi.XMR_nanopool.hashrateHistory_tm
                .Where((KeyValuePair<int, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<int, decimal> value) => value.Value * secondsPerAveragehashrateReportInterval)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                decimal sumHashrate_user_rvn =
                ExternalApi.RVN_nanopool.hashrateHistory_user
                .Where((KeyValuePair<int, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<int, decimal> value) => value.Value * secondsPerAveragehashrateReportInterval)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                decimal sumHashrate_tm_rvn =
                ExternalApi.RVN_nanopool.hashrateHistory_tm
                .Where((KeyValuePair<int, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<int, decimal> value) => value.Value * secondsPerAveragehashrateReportInterval)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                XMR_nanopool.pointsHistory_user = XMR_nanopool.hashrateHistory_user.Select(x => new KeyValuePair<int, decimal>(x.Key, x.Value / XMR_nanopool.sharecoef.data / 16)).ToDictionary((KeyValuePair<int, decimal> y) => y.Key, y => y.Value);
                RVN_nanopool.pointsHistory_user = RVN_nanopool.hashrateHistory_user.Select(x => new KeyValuePair<int, decimal>(x.Key, x.Value / RVN_nanopool.sharecoef.data / 6)).ToDictionary((KeyValuePair<int, decimal> y) => y.Key, y => y.Value);

                HashesPerPoint_xmr = XMR_nanopool.sharecoef.data * 16;
                HashesPerPoint_rvn = RVN_nanopool.sharecoef.data * 6;

                decimal totalXMRmineradoTrueMining = (decimal)XMR_nanopool.approximated_earnings.data.day.coins.SubtractFee(1) /*desconto da fee da pool que não está sendo inserida no cálculo*/ / (decimal)hashesToCompare / (decimal)TimeSpan.FromDays(1).TotalSeconds * (decimal)sumHashrate_tm_xmr;
                decimal totalRVNmineradoTrueMining = (decimal)RVN_nanopool.approximated_earnings.data.day.coins.SubtractFee(1) /*desconto da fee da pool que não está sendo inserida no cálculo*/ / (decimal)hashesToCompare / (decimal)TimeSpan.FromDays(1).TotalSeconds * (decimal)sumHashrate_tm_rvn;

                decimal BTCpraVirarPaymentCoin = (totalXMRmineradoTrueMining * new Tools.LiquidityPrices(ExchangeOrderbooks.XMRBTC, totalXMRmineradoTrueMining).SellPrice) + (totalRVNmineradoTrueMining * new Tools.LiquidityPrices(ExchangeOrderbooks.RVNBTC, totalRVNmineradoTrueMining).SellPrice);

                decimal PaymentCoinFinalPrice = new Tools.LiquidityPrices(ExchangeOrderbooks.PaymentCoinBTC, BTCpraVirarPaymentCoin).BuyPrice;

                AccumulatedBalance_Points_xmr =
                ExternalApi.XMR_nanopool.pointsHistory_user
                .Where((KeyValuePair<int, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<int, decimal> value) => value.Value)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                exchangeRatePontosXmrToMiningCoin = HashesPerPoint_xmr * secondsPerAveragehashrateReportInterval * (XMR_nanopool.approximated_earnings.data.hour.coins.SubtractFee(1) / hashesToCompare / 60 / 60);

                AccumulatedBalance_Points_rvn =
                ExternalApi.RVN_nanopool.pointsHistory_user
                .Where((KeyValuePair<int, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<int, decimal> value) => value.Value)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                exchangeRatePontosRvnToMiningCoin = HashesPerPoint_rvn * secondsPerAveragehashrateReportInterval * (RVN_nanopool.approximated_earnings.data.hour.coins.SubtractFee(1) / hashesToCompare / 60 / 60);

                AccumulatedBalance_Coins = Decimal.Round((totalXMRmineradoTrueMining * Decimal.Divide(new Tools.LiquidityPrices(ExchangeOrderbooks.XMRBTC, totalXMRmineradoTrueMining).SellPrice, PaymentCoinFinalPrice) * Decimal.Divide(sumHashrate_user_xmr, sumHashrate_tm_xmr)).SubtractFee(Server.SoftwareParameters.ServerConfig.DynamicFee) + (totalRVNmineradoTrueMining * Decimal.Divide(new Tools.LiquidityPrices(ExchangeOrderbooks.RVNBTC, totalRVNmineradoTrueMining).SellPrice, PaymentCoinFinalPrice) * Decimal.Divide(sumHashrate_user_rvn, sumHashrate_tm_rvn)).SubtractFee(Server.SoftwareParameters.ServerConfig.DynamicFee), 5);

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