using System.Windows;
using System.Windows.Controls;
using True_Mining_v4.Janelas.Popups;
using True_Mining_v4.Server;

namespace True_Mining_v4.Janelas
{
    /// <summary>
    /// Interação lógica para Dashboard.xam
    /// </summary>
    public partial class Dashboard : UserControl
    {
        public Dashboard()
        {
            InitializeComponent();
            DataContext = this;
        }

        private string labelNextPayout;
        private string labelAccumulatedBalance;
        private string labelWarning;
        private Visibility warningWrapVisibility;

        public string LabelNextPayout { get { return labelNextPayout; } set { labelNextPayout = value; xLabelNextPayout.Content = value; } }
        public string LabelAccumulatedBalance { get { return labelAccumulatedBalance; } set { labelAccumulatedBalance = value; xLabelAccumulatedBalance.Content = value; } }
        public string LabelWarning { get { return labelWarning; } set { labelWarning = value; xLabelWarning.Content = value; } }
        public Visibility WarningWrapVisibility { get { return warningWrapVisibility; } set { warningWrapVisibility = value; xWarningWrapVisibility.Visibility = value; } }

        private bool firstTimeLoad = false;

        private Saldo saldo;

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
            new Calculator(saldo.conversionHashrateToPoints, saldo.exchangeRatePontosToMiningCoin).ShowDialog();
        }

        private void Button_ExchangeRates_Popup(object sender, RoutedEventArgs e)
        {
            new ExchangeRates(saldo.exchangeRatePontosToMiningCoin).ShowDialog();
        }
    }
}