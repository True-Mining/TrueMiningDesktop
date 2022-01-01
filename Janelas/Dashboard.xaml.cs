using OxyPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using TrueMiningDesktop.Janelas.Popups;
using TrueMiningDesktop.Server;

namespace TrueMiningDesktop.Janelas
{
    /// <summary>
    /// Interação lógica para Dashboard.xam
    /// </summary>
    public partial class Dashboard : INotifyPropertyChanged
    {
        public Dashboard()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string labelNextPayout;
        private string labelAccumulatedBalance;
        private List<string> dashboardWarnings = new();
        public Visibility warningWrapVisibility = Visibility.Visible;

        public string LabelNextPayout
        { get { return labelNextPayout; } set { labelNextPayout = value; xLabelNextPayout.Content = value; } }

        public string LabelAccumulatedBalance
        { get { return labelAccumulatedBalance; } set { labelAccumulatedBalance = value; xLabelAccumulatedBalance.Content = value; } }

        public List<string> DashboardWarnings
        { get { return dashboardWarnings; } set { dashboardWarnings = value; NotifyPropertyChanged(); } }

        public Visibility WarningWrapVisibility
        { get { return warningWrapVisibility; } set { warningWrapVisibility = value; NotifyPropertyChanged(); } }

        public static string WalletAddress
        { get { return User.Settings.User.Payment_Wallet; } set { } }

        private bool firstTimeLoad = false;

        private Saldo saldo;

        private PlotModel chartModel;
        private OxyPlot.Series.ColumnSeries columnChartSeries;
        private PlotController chartControler;
        private Visibility chartVisibility = Visibility.Hidden;

        public PlotModel ChartModel
        { get { return chartModel; } set { chartModel = value; NotifyPropertyChanged(); } }

        public OxyPlot.Series.ColumnSeries ColumnChartSeries
        { get { return columnChartSeries; } set { columnChartSeries = value; NotifyPropertyChanged(); } }

        public PlotController ChartController
        { get { return chartControler; } set { chartControler = value; NotifyPropertyChanged(); } }

        public Visibility ChartVisibility
        { get { return chartVisibility; } set { chartVisibility = value; NotifyPropertyChanged(); } }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!firstTimeLoad)
            {
                loadingVisualElement.Visibility = Visibility.Visible;
                DashboardContent.IsEnabled = false;
                saldo = new Saldo();
                firstTimeLoad = true;
            }
        }

        private void Button_Calculator_Popup(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.Title == "Calculator") { window.Close(); }
                }

                new Calculator(saldo.HashesPerPoint_xmr, saldo.exchangeRatePontosXmrToMiningCoin) { Title = "Calculator" }.Show();
            }
            catch { }
        }

        private void Button_ExchangeRates_Popup(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.Title == "Exchange Rates") { window.Close(); }
                }

                new ExchangeRates(saldo.exchangeRatePontosXmrToMiningCoin, saldo.exchangeRatePontosRvnToMiningCoin) { Title = "Exchange Rates" }.Show();
            }
            catch { }
        }

        public void ChangeChartZoom(object sender, RoutedEventArgs e)
        {
            string content = null;
            if (sender != null)
            {
                try
                {
                    content = (sender as Button).Content.ToString();
                }
                catch { }
            }

            switch (content)
            {
                case "12h":
                    {
                        chart_zoom_interval = new TimeSpan(0, 12, 0, 0);
                    }
                    break;

                case "1d":
                    {
                        chart_zoom_interval = new TimeSpan(1, 0, 0, 0);
                    }
                    break;

                case "5d":
                    {
                        chart_zoom_interval = new TimeSpan(5, 0, 0, 0);
                    }
                    break;

                default:
                    {
                        //deixa como está, só atualiza o gráfico
                    }
                    break;
            }

            ViewModel.DashboardChart.UpdateAxes(new[] { PoolAPI.XMR_nanopool.pointsHistory_user, PoolAPI.RVN_nanopool.pointsHistory_user }.SelectMany(d => d).GroupBy( kvp => kvp.Key, (key, kvps) => new { Key = key, Value = kvps.Sum(kvp => kvp.Value) }).ToDictionary(x => x.Key, x => x.Value), (int)Pages.Dashboard.chart_zoom_interval.TotalSeconds);
        }

        public TimeSpan chart_zoom_interval { get; set; } = new TimeSpan(0, 24, 0, 0);

        private void PackIcon_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            saldo.UpdateBalances();
        }

        private void ShowWarnings(object sender, RoutedEventArgs e)
        {
            foreach (string warning in DashboardWarnings)
            {
                MessageBox.Show(warning);
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            Core.Tools.OpenLinkInBrowser("https://gist.github.com/matheusbach/462558744709625db1149b7a1e5d384e");
        }
    }
}