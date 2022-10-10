﻿using CoinpaprikaAPI.Entity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TrueMiningDesktop.Core;
using TrueMiningDesktop.ExternalApi;
using TrueMiningDesktop.Janelas;

namespace TrueMiningDesktop.Server
{
    public class Saldo
    {
        private readonly System.Timers.Timer timerUpdateDashboard = new(1000);

        public Saldo()
        {
            Task.Run(() =>
            {
                Server.SoftwareParameters.Update(new Uri("https://truemining.online/config.json"), new Uri("https://www.utivirtual.com.br/Truemining/config.json"));

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
                    if (AccumulatedBalance_Points_xmr > 0) { listPointslabel.Add(Math.Round(AccumulatedBalance_Points_xmr, 0) + " RandomX-Points"); }
                    if (AccumulatedBalance_Points_rvn > 0) { listPointslabel.Add(Math.Round(AccumulatedBalance_Points_rvn, 0) + " KawPow-Points"); }
                    if (AccumulatedBalance_Points_etc > 0) { listPointslabel.Add(Math.Round(AccumulatedBalance_Points_etc, 0) + " Etchash-Points"); }

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
        public decimal AccumulatedBalance_Points_etc = 0;
        public decimal AccumulatedBalance_Coins = 0;

        private HashrateHistory hashrateHystory_xmr_user_raw = new();
        private HashrateHistory hashrateHystory_xmr_tm_raw = new();

        private HashrateHistory hashrateHystory_rvn_user_raw = new();
        private HashrateHistory hashrateHystory_rvn_tm_raw = new();

        private HashrateHistory hashrateHystory_etc_user_raw = new();
        private HashrateHistory hashrateHystory_etc_tm_raw = new();

        public decimal HashesPerPoint_xmr;
        public decimal exchangeRatePontosXmrToMiningCoin;
        public decimal HashesPerPoint_rvn;
        public decimal exchangeRatePontosRvnToMiningCoin;
        public decimal HashesPerPoint_etc;
        public decimal exchangeRatePontosEtcToMiningCoin;

        private decimal sumHashrate_tm_xmr = 0;
        private decimal sumHashrate_user_xmr = 0;
        private decimal sumHashrate_tm_rvn = 0;
        private decimal sumHashrate_user_rvn = 0;
        private decimal sumHashrate_tm_etc = 0;
        private decimal sumHashrate_user_etc = 0;

        private decimal totalXMRmineradoTrueMining = 0;
        private decimal totalRVNmineradoTrueMining = 0;
        private decimal totalETCmineradoTrueMining = 0;

        private decimal BTCpraVirarPaymentCoin = 0;

        private decimal PaymentCoinFinalPrice = 0;

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
                    List<Task<Action>> getAPIsTask = new();

                    //    decimal rvnPrice = new CoinpaprikaAPI.Client().GetLatestOhlcForCoinAsync("rvn-ravencoin", "BTC").Result.Value.Last().Close;

                    getAPIsTask.Add(new Task<Action>(() => { hashrateHystory_xmr_user_raw = NanopoolData.GetHashrateHystory("xmr", SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.CoinTicker.Equals("xmr", StringComparison.OrdinalIgnoreCase)).DepositAddressTrueMining, User.Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + User.Settings.User.Payment_Wallet); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { hashrateHystory_xmr_tm_raw = NanopoolData.GetHashrateHystory("xmr", SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.CoinTicker.Equals("xmr", StringComparison.OrdinalIgnoreCase)).DepositAddressTrueMining); return null; }));

                    getAPIsTask.Add(new Task<Action>(() =>
                    {
                        bool orderbookStisfied = false;
                        try
                        {
                            if (!orderbookStisfied)
                            {
								Dictionary<string, CoinData> simplePrices = JsonConvert.DeserializeObject<Dictionary<string, CoinData>>(Tools.HttpGet("https://api.coingecko.com/api/v3/simple/price?ids=" + "monero" + "&vs_currencies=btc&include_market_cap=false&include_24hr_vol=false&include_24hr_change=true&include_last_updated_at=true&precision=full"), ExternalApi.Converter.Settings);
								CoinData simplePrice = simplePrices["monero"];
								simplePrice.Btc24HChange = simplePrice.Btc24HChange < 0 ? simplePrice.Btc24HChange * -1 : simplePrice.Btc24HChange;

								ExternalApi.Orderbook orderbookObj = new() { buyLevels = new List<ExternalApi.BuyLevel>() { new ExternalApi.BuyLevel() { price = simplePrice.Btc * ((100 - simplePrice.Btc24HChange ?? 5 / 4) / 100), volume = 1 } }, sellLevels = new List<ExternalApi.SellLevel>() { new ExternalApi.SellLevel() { price = simplePrice.Btc * ((100 + simplePrice.Btc24HChange ?? 5 / 4) / 100), volume = 1 } } };

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

								OhlcValue coinLatestOhlc = coinpaprikaAPI.GetLatestOhlcForCoinAsync("xmr-monero", "BTC").Result.Value.Last();
								decimal coinLastPrice = coinLatestOhlc.Close;
								decimal coinPriceVarPercent = (coinLatestOhlc.Open - coinLatestOhlc.Close) / coinLatestOhlc.Close * 100;
								coinPriceVarPercent = coinPriceVarPercent < 0 ? coinPriceVarPercent * -1 : coinPriceVarPercent;

								ExternalApi.Orderbook orderbookObj = new() { buyLevels = new List<ExternalApi.BuyLevel>() { new ExternalApi.BuyLevel() { price = coinLastPrice * ((100 - coinPriceVarPercent / 4) / 100), volume = 1 } }, sellLevels = new List<ExternalApi.SellLevel>() { new ExternalApi.SellLevel() { price = coinLastPrice * ((100 + coinPriceVarPercent / 4) / 100), volume = 1 } } };

								ExchangeOrderbooks.XMRBTC = orderbookObj;
                                orderbookStisfied = true;
                            }
                        }
                        catch { }
                        return null;
                    }));

                    getAPIsTask.Add(new Task<Action>(() => { NanopoolData.XMR_nanopool.approximated_earnings = JsonConvert.DeserializeObject<ExternalApi.approximated_earnings>(Tools.HttpGet("https://api.nanopool.org/v1/xmr/approximated_earnings/" + hashesToCompare), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture }); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { NanopoolData.XMR_nanopool.sharecoef = JsonConvert.DeserializeObject<ExternalApi.share_coefficient>(Tools.HttpGet("https://api.nanopool.org/v1/xmr/pool/sharecoef"), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture }); return null; }));

                    getAPIsTask.Add(new Task<Action>(() => { hashrateHystory_rvn_user_raw = NanopoolData.GetHashrateHystory("rvn", SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.CoinTicker.Equals("rvn", StringComparison.OrdinalIgnoreCase)).DepositAddressTrueMining, User.Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + User.Settings.User.Payment_Wallet); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { hashrateHystory_rvn_tm_raw = NanopoolData.GetHashrateHystory("rvn", SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.CoinTicker.Equals("rvn", StringComparison.OrdinalIgnoreCase)).DepositAddressTrueMining); return null; }));

                    getAPIsTask.Add(new Task<Action>(() =>
                    {
						bool orderbookStisfied = false;
						try
						{
							if (!orderbookStisfied)
							{
								Dictionary<string, CoinData> simplePrices = JsonConvert.DeserializeObject<Dictionary<string, CoinData>>(Tools.HttpGet("https://api.coingecko.com/api/v3/simple/price?ids=" + "ravencoin" + "&vs_currencies=btc&include_market_cap=false&include_24hr_vol=false&include_24hr_change=true&include_last_updated_at=true&precision=full"), ExternalApi.Converter.Settings);
								CoinData simplePrice = simplePrices["ravencoin"];
								simplePrice.Btc24HChange = simplePrice.Btc24HChange < 0 ? simplePrice.Btc24HChange * -1 : simplePrice.Btc24HChange;

								ExternalApi.Orderbook orderbookObj = new() { buyLevels = new List<ExternalApi.BuyLevel>() { new ExternalApi.BuyLevel() { price = simplePrice.Btc * ((100 - simplePrice.Btc24HChange ?? 5 / 4) / 100), volume = 1 } }, sellLevels = new List<ExternalApi.SellLevel>() { new ExternalApi.SellLevel() { price = simplePrice.Btc * ((100 + simplePrice.Btc24HChange ?? 5 / 4) / 100), volume = 1 } } };

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

								OhlcValue coinLatestOhlc = coinpaprikaAPI.GetLatestOhlcForCoinAsync("rvn-ravencoin", "BTC").Result.Value.Last();
								decimal coinLastPrice = coinLatestOhlc.Close;
								decimal coinPriceVarPercent = (coinLatestOhlc.Open - coinLatestOhlc.Close) / coinLatestOhlc.Close * 100;
								coinPriceVarPercent = coinPriceVarPercent < 0 ? coinPriceVarPercent * -1 : coinPriceVarPercent;

								ExternalApi.Orderbook orderbookObj = new() { buyLevels = new List<ExternalApi.BuyLevel>() { new ExternalApi.BuyLevel() { price = coinLastPrice * ((100 - coinPriceVarPercent / 4) / 100), volume = 1 } }, sellLevels = new List<ExternalApi.SellLevel>() { new ExternalApi.SellLevel() { price = coinLastPrice * ((100 + coinPriceVarPercent / 4) / 100), volume = 1 } } };

								ExchangeOrderbooks.RVNBTC = orderbookObj;
								orderbookStisfied = true;
							}
						}
						catch { }
						return null;
					}));
                    getAPIsTask.Add(new Task<Action>(() => { NanopoolData.RVN_nanopool.approximated_earnings = JsonConvert.DeserializeObject<ExternalApi.approximated_earnings>(Tools.HttpGet("https://api.nanopool.org/v1/rvn/approximated_earnings/" + hashesToCompare), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture }); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { NanopoolData.RVN_nanopool.sharecoef = JsonConvert.DeserializeObject<ExternalApi.share_coefficient>(Tools.HttpGet("https://api.nanopool.org/v1/rvn/pool/sharecoef"), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture }); return null; }));

                    getAPIsTask.Add(new Task<Action>(() => { hashrateHystory_etc_user_raw = NanopoolData.GetHashrateHystory("etc", SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.CoinTicker.Equals("etc", StringComparison.OrdinalIgnoreCase)).DepositAddressTrueMining, User.Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + User.Settings.User.Payment_Wallet); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { hashrateHystory_etc_tm_raw = NanopoolData.GetHashrateHystory("etc", SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.CoinTicker.Equals("etc", StringComparison.OrdinalIgnoreCase)).DepositAddressTrueMining); return null; }));

                    getAPIsTask.Add(new Task<Action>(() =>
                    {
						bool orderbookStisfied = false;
						try
						{
							if (!orderbookStisfied)
							{
								Dictionary<string, CoinData> simplePrices = JsonConvert.DeserializeObject<Dictionary<string, CoinData>>(Tools.HttpGet("https://api.coingecko.com/api/v3/simple/price?ids=" + "ethereum-classic" + "&vs_currencies=btc&include_market_cap=false&include_24hr_vol=false&include_24hr_change=true&include_last_updated_at=true&precision=full"), ExternalApi.Converter.Settings);
								CoinData simplePrice = simplePrices["ethereum-classic"];
								simplePrice.Btc24HChange = simplePrice.Btc24HChange < 0 ? simplePrice.Btc24HChange * -1 : simplePrice.Btc24HChange;

								ExternalApi.Orderbook orderbookObj = new() { buyLevels = new List<ExternalApi.BuyLevel>() { new ExternalApi.BuyLevel() { price = simplePrice.Btc * ((100 - simplePrice.Btc24HChange ?? 5 / 4) / 100), volume = 1 } }, sellLevels = new List<ExternalApi.SellLevel>() { new ExternalApi.SellLevel() { price = simplePrice.Btc * ((100 + simplePrice.Btc24HChange ?? 5 / 4) / 100), volume = 1 } } };

								ExchangeOrderbooks.ETCBTC = orderbookObj;
								orderbookStisfied = true;
							}
						}
						catch { }

						try
						{
							if (!orderbookStisfied)
							{
								CoinpaprikaAPI.Client coinpaprikaAPI = new();

								OhlcValue coinLatestOhlc = coinpaprikaAPI.GetLatestOhlcForCoinAsync("etc-ethereum-classic", "BTC").Result.Value.Last();
								decimal coinLastPrice = coinLatestOhlc.Close;
								decimal coinPriceVarPercent = (coinLatestOhlc.Open - coinLatestOhlc.Close) / coinLatestOhlc.Close * 100;
								coinPriceVarPercent = coinPriceVarPercent < 0 ? coinPriceVarPercent * -1 : coinPriceVarPercent;

								ExternalApi.Orderbook orderbookObj = new() { buyLevels = new List<ExternalApi.BuyLevel>() { new ExternalApi.BuyLevel() { price = coinLastPrice * ((100 - coinPriceVarPercent / 4) / 100), volume = 1 } }, sellLevels = new List<ExternalApi.SellLevel>() { new ExternalApi.SellLevel() { price = coinLastPrice * ((100 + coinPriceVarPercent / 4) / 100), volume = 1 } } };

								ExchangeOrderbooks.ETCBTC = orderbookObj;
								orderbookStisfied = true;
							}
						}
						catch { }
						return null;
					}));
                    getAPIsTask.Add(new Task<Action>(() => { NanopoolData.ETC_nanopool.approximated_earnings = JsonConvert.DeserializeObject<ExternalApi.approximated_earnings>(Tools.HttpGet("https://api.nanopool.org/v1/etc/approximated_earnings/" + hashesToCompare), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture }); return null; }));
                    getAPIsTask.Add(new Task<Action>(() => { NanopoolData.ETC_nanopool.sharecoef = JsonConvert.DeserializeObject<ExternalApi.share_coefficient>(Tools.HttpGet("https://api.nanopool.org/v1/etc/pool/sharecoef"), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture }); return null; }));

                    getAPIsTask.Add(new Task<Action>(() =>
                    {
                        bool orderbookStisfied = false;
                        try
                        {
                            if (!orderbookStisfied && User.Settings.User.PayCoin.MarketDataSources.Any(nameDataSource => nameDataSource.Equals("coingecko", StringComparison.OrdinalIgnoreCase)))
                            {
								Dictionary<string, CoinData> simplePrices = JsonConvert.DeserializeObject<Dictionary<string, CoinData>>(Tools.HttpGet("https://api.coingecko.com/api/v3/simple/price?ids=" + User.Settings.User.PayCoin.CoinName.ToLower().Replace(' ', '-') + "&vs_currencies=btc&include_market_cap=false&include_24hr_vol=false&include_24hr_change=true&include_last_updated_at=true&precision=full"), ExternalApi.Converter.Settings);
								CoinData simplePrice = simplePrices[User.Settings.User.PayCoin.CoinName.ToLower().Replace(' ', '-')];
								simplePrice.Btc24HChange = simplePrice.Btc24HChange < 0 ? simplePrice.Btc24HChange * -1 : simplePrice.Btc24HChange;

								ExternalApi.Orderbook orderbookObj = new() { buyLevels = new List<ExternalApi.BuyLevel>() { new ExternalApi.BuyLevel() { price = simplePrice.Btc * ((100 - simplePrice.Btc24HChange ?? 5 / 4) / 100), volume = 1 } }, sellLevels = new List<ExternalApi.SellLevel>() { new ExternalApi.SellLevel() { price = simplePrice.Btc * ((100 + simplePrice.Btc24HChange ?? 5 / 4) / 100), volume = 1 } } };

								ExchangeOrderbooks.PaymentCoinBTC = orderbookObj;
                                orderbookStisfied = true;
                            }
                        }
                        catch { }

                        try
                        {
                            if (!orderbookStisfied && User.Settings.User.PayCoin.MarketDataSources.Any(nameDataSource => nameDataSource.Equals("coinpaprika", StringComparison.OrdinalIgnoreCase)))
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
                        return null;
                    }));

                    getAPIsTask.Add(new Task<Action>(() => { BitcoinPrice.BTCUSD = Math.Round(new CoinpaprikaAPI.Client().GetLatestOhlcForCoinAsync("btc-bitcoin", "USD").Result.Value.Last().Close, 2); return null; }));

                    getAPIsTask.ForEach(task => task.Start());
                    getAPIsTask.ForEach(task => task.Wait(60000));

                    NanopoolData.XMR_nanopool.hashrateHistory_user.Clear();
					NanopoolData.XMR_nanopool.hashrateHistory_tm.Clear();

                    hashrateHystory_xmr_user_raw.Data.ForEach(data =>
                    {
                        try
                        {
							NanopoolData.XMR_nanopool.hashrateHistory_user.Add(data.Date, data.Hashrate);
                        }
                        catch { }
                    });
                    hashrateHystory_xmr_tm_raw.Data.ForEach(data =>
                    {
                        try
                        {
							NanopoolData.XMR_nanopool.hashrateHistory_tm.Add(data.Date, data.Hashrate);
                        }
                        catch { }
                    });

					NanopoolData.RVN_nanopool.hashrateHistory_user.Clear();
					NanopoolData.RVN_nanopool.hashrateHistory_tm.Clear();

                    hashrateHystory_rvn_user_raw.Data.ForEach(data =>
                    {
                        try
                        {
							NanopoolData.RVN_nanopool.hashrateHistory_user.Add(data.Date, data.Hashrate);
                        }
                        catch { }
                    });
                    hashrateHystory_rvn_tm_raw.Data.ForEach(data =>
                    {
                        try
                        {
							NanopoolData.RVN_nanopool.hashrateHistory_tm.Add(data.Date, data.Hashrate);
                        }
                        catch { }
                    });

					NanopoolData.ETC_nanopool.hashrateHistory_user.Clear();
					NanopoolData.ETC_nanopool.hashrateHistory_tm.Clear();

                    hashrateHystory_etc_user_raw.Data.ForEach(data =>
                    {
                        try
                        {
							NanopoolData.ETC_nanopool.hashrateHistory_user.Add(data.Date, data.Hashrate);
                        }
                        catch { }
                    });
                    hashrateHystory_etc_tm_raw.Data.ForEach(data =>
                    {
                        try
                        {
							NanopoolData.ETC_nanopool.hashrateHistory_tm.Add(data.Date, data.Hashrate);
                        }
                        catch { }
                    });
                }
                catch { lastUpdated = DateTime.Now.AddSeconds(-10); }

                sumHashrate_user_xmr =
				NanopoolData.XMR_nanopool.hashrateHistory_user
                .Where((KeyValuePair<long, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<long, decimal> value) => value.Value * secondsPerAveragehashrateReportInterval)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                sumHashrate_tm_xmr =
				NanopoolData.XMR_nanopool.hashrateHistory_tm
                .Where((KeyValuePair<long, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<long, decimal> value) => value.Value * secondsPerAveragehashrateReportInterval)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                sumHashrate_user_rvn =
				NanopoolData.RVN_nanopool.hashrateHistory_user
                .Where((KeyValuePair<long, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<long, decimal> value) => value.Value * secondsPerAveragehashrateReportInterval)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                sumHashrate_tm_rvn =
				NanopoolData.RVN_nanopool.hashrateHistory_tm
                .Where((KeyValuePair<long, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<long, decimal> value) => value.Value * secondsPerAveragehashrateReportInterval)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                sumHashrate_user_etc =
				NanopoolData.ETC_nanopool.hashrateHistory_user
                .Where((KeyValuePair<long, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<long, decimal> value) => value.Value * secondsPerAveragehashrateReportInterval)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                sumHashrate_tm_etc =
				NanopoolData.ETC_nanopool.hashrateHistory_tm
                .Where((KeyValuePair<long, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<long, decimal> value) => value.Value * secondsPerAveragehashrateReportInterval)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                HashesPerPoint_xmr = NanopoolData.XMR_nanopool.sharecoef.data * 16;
                HashesPerPoint_rvn = NanopoolData.RVN_nanopool.sharecoef.data * 6;
                HashesPerPoint_etc = NanopoolData.ETC_nanopool.sharecoef.data * 2;

				NanopoolData.XMR_nanopool.pointsHistory_user = NanopoolData.XMR_nanopool.hashrateHistory_user.Select(x => new KeyValuePair<long, decimal>(x.Key, x.Value / NanopoolData.XMR_nanopool.sharecoef.data / 16)).ToDictionary((KeyValuePair<long, decimal> y) => y.Key, y => y.Value);
				NanopoolData.RVN_nanopool.pointsHistory_user = NanopoolData.RVN_nanopool.hashrateHistory_user.Select(x => new KeyValuePair<long, decimal>(x.Key, x.Value / NanopoolData.RVN_nanopool.sharecoef.data / 6)).ToDictionary((KeyValuePair<long, decimal> y) => y.Key, y => y.Value);
				NanopoolData.ETC_nanopool.pointsHistory_user = NanopoolData.ETC_nanopool.hashrateHistory_user.Select(x => new KeyValuePair<long, decimal>(x.Key, x.Value / NanopoolData.ETC_nanopool.sharecoef.data / 2)).ToDictionary((KeyValuePair<long, decimal> y) => y.Key, y => y.Value);

                totalXMRmineradoTrueMining = (decimal)NanopoolData.XMR_nanopool.approximated_earnings.data.day.coins.SubtractFee(1) / (decimal)hashesToCompare / (decimal)TimeSpan.FromDays(1).TotalSeconds * (decimal)sumHashrate_tm_xmr;
                totalRVNmineradoTrueMining = (decimal)NanopoolData.RVN_nanopool.approximated_earnings.data.day.coins.SubtractFee(1) / (decimal)hashesToCompare / (decimal)TimeSpan.FromDays(1).TotalSeconds * (decimal)sumHashrate_tm_rvn;
                totalETCmineradoTrueMining = (decimal)NanopoolData.ETC_nanopool.approximated_earnings.data.day.coins.SubtractFee(1) / (decimal)hashesToCompare / (decimal)TimeSpan.FromDays(1).TotalSeconds * (decimal)sumHashrate_tm_etc;

                BTCpraVirarPaymentCoin = (totalXMRmineradoTrueMining * new Tools.LiquidityPrices(ExchangeOrderbooks.XMRBTC, totalXMRmineradoTrueMining).SellPrice) + (totalRVNmineradoTrueMining * new Tools.LiquidityPrices(ExchangeOrderbooks.RVNBTC, totalRVNmineradoTrueMining).SellPrice) + (totalETCmineradoTrueMining * new Tools.LiquidityPrices(ExchangeOrderbooks.ETCBTC, totalETCmineradoTrueMining).SellPrice);

                PaymentCoinFinalPrice = new Tools.LiquidityPrices(ExchangeOrderbooks.PaymentCoinBTC, BTCpraVirarPaymentCoin).BuyPrice;

                AccumulatedBalance_Points_xmr =
				NanopoolData.XMR_nanopool.pointsHistory_user
                .Where((KeyValuePair<long, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<long, decimal> value) => value.Value)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                exchangeRatePontosXmrToMiningCoin = HashesPerPoint_xmr * secondsPerAveragehashrateReportInterval * (NanopoolData.XMR_nanopool.approximated_earnings.data.hour.coins.SubtractFee(1) / hashesToCompare / 60 / 60);

                AccumulatedBalance_Points_rvn =
				NanopoolData.RVN_nanopool.pointsHistory_user
                .Where((KeyValuePair<long, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<long, decimal> value) => value.Value)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                exchangeRatePontosRvnToMiningCoin = HashesPerPoint_rvn * secondsPerAveragehashrateReportInterval * (NanopoolData.RVN_nanopool.approximated_earnings.data.hour.coins.SubtractFee(1) / hashesToCompare / 60 / 60);

                AccumulatedBalance_Points_etc =
				NanopoolData.ETC_nanopool.pointsHistory_user
                .Where((KeyValuePair<long, decimal> value) =>
                value.Key >= ((DateTimeOffset)lastPayment).ToUnixTimeSeconds())
                .Select((KeyValuePair<long, decimal> value) => value.Value)
                .Aggregate(0, (Func<decimal, decimal, decimal>)((acc, now) =>
                {
                    return acc + now;
                }));

                exchangeRatePontosEtcToMiningCoin = HashesPerPoint_etc * secondsPerAveragehashrateReportInterval * (NanopoolData.ETC_nanopool.approximated_earnings.data.hour.coins.SubtractFee(1) / hashesToCompare / 60 / 60);

                AccumulatedBalance_Coins = Decimal.Round(
                (totalXMRmineradoTrueMining * new Tools.LiquidityPrices(ExchangeOrderbooks.XMRBTC, totalXMRmineradoTrueMining).SellPrice / PaymentCoinFinalPrice * (sumHashrate_tm_xmr > 0 ? Decimal.Divide(sumHashrate_user_xmr, sumHashrate_tm_xmr) : 0)).SubtractFee(Server.SoftwareParameters.ServerConfig.DynamicFee) +
                (totalRVNmineradoTrueMining * new Tools.LiquidityPrices(ExchangeOrderbooks.RVNBTC, totalRVNmineradoTrueMining).SellPrice / PaymentCoinFinalPrice * (sumHashrate_tm_rvn > 0 ? Decimal.Divide(sumHashrate_user_rvn, sumHashrate_tm_rvn) : 0)).SubtractFee(Server.SoftwareParameters.ServerConfig.DynamicFee) +
                (totalETCmineradoTrueMining * new Tools.LiquidityPrices(ExchangeOrderbooks.ETCBTC, totalETCmineradoTrueMining).SellPrice / PaymentCoinFinalPrice * (sumHashrate_tm_etc > 0 ? Decimal.Divide(sumHashrate_user_etc, sumHashrate_tm_etc) : 0)).SubtractFee(Server.SoftwareParameters.ServerConfig.DynamicFee), 5);

                string warningMessage = "Balance less than " + SoftwareParameters.ServerConfig.PaymentCoins.Find(x => Equals(x.CoinTicker, User.Settings.User.PayCoin.CoinTicker)).MinPayout + " " + User.Settings.User.PayCoin.CoinTicker.ToUpperInvariant() + " will be paid once a week when you reach the minimum amount. Your balance will disappear from the dashboard, but it will still be saved in our system";
                string warningMessage2 = "Mined points take an average of 10-20 minutes to be displayed on the dashboard.";

                Janelas.Pages.Dashboard.DashboardWarnings.Clear();

                if (AccumulatedBalance_Coins <= 0)
                {
                    if (!Pages.Dashboard.DashboardWarnings.Contains(warningMessage2)) Janelas.Pages.Dashboard.DashboardWarnings.Add(warningMessage2); Pages.Dashboard.WarningWrapVisibility = Pages.Dashboard.DashboardWarnings.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    if (Pages.Dashboard.DashboardWarnings.Contains(warningMessage2)) Janelas.Pages.Dashboard.DashboardWarnings.Remove(warningMessage2); Pages.Dashboard.WarningWrapVisibility = Pages.Dashboard.DashboardWarnings.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                }

                if (AccumulatedBalance_Coins < SoftwareParameters.ServerConfig.PaymentCoins.Find(x => Equals(x.CoinTicker, User.Settings.User.PayCoin.CoinTicker)).MinPayout)
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