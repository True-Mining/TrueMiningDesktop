using MaterialDesignThemes.Wpf;
using System;
using System.Linq;
using TrueMiningDesktop.Core;
using TrueMiningDesktop.ViewModel;

namespace TrueMiningDesktop.Janelas
{
    public class DeviceInfo : OverviewDeviceSimplified
    {
        public OverviewDeviceSimplified OverviewDeviceSimplified = new();

        public event EventHandler PropertieChangedDevInfo;

        private readonly Hashrate_timer hashrate_timer;

        public DeviceInfo(string backendName, string name, string miningAlgo = null, bool isSelected = false, decimal hashrate = -1, PackIconKind iconKind = PackIconKind.Cpu64Bit)
        {
            BackendName = backendName;
            DeviceName = name;
            MiningAlgo = miningAlgo;
            HashrateValue_raw = hashrate;
            IsSelected = isSelected;
            IconKind = iconKind;

            Janelas.Pages.Home.listDevicesOverview.Children.Add(OverviewDeviceSimplified);

            OverviewDeviceSimplified.PropertieChanged += new EventHandler(OVchanged);

            OverviewDeviceSimplified.RefreshDataContext(this);

            hashrate_timer = new Hashrate_timer(this);
            hashrate_timer.HashrateUpdated += HashrateUpdated;
        }

        private void HashrateUpdated(object sender, EventArgs e)
        {
            HashrateValue_raw = hashrate_timer.Hashrate;
        }

        protected virtual void OnChangedDevInfo(EventArgs e)
        {
            PropertieChangedDevInfo?.Invoke(this, e);
        }

        private void OVchanged(object sender, EventArgs e)
        {
            IsSelected = (bool)OverviewDeviceSimplified.ovDeviceIsSelected.IsChecked;
        }

        public void what()
        {
            // bixo sei lá, só sei que deixa desse jeito pra funcionar
        }

        public string BackendName { get; private set; }
        public string DeviceName { get; private set; }
        public string miningAlgo;

        public string MiningAlgo
        {
            get => miningAlgo;
            set
            {
                miningAlgo = value;
                _ = Dispatcher.BeginInvoke((Action)(() => { OverviewDeviceSimplified.RefreshDataContext(this); }));
            }
        }

        private decimal hashrateValue_raw = -1;

        public decimal HashrateValue_raw
        {
            get => hashrateValue_raw;
            set
            {
                hashrateValue_raw = value;
                if (Server.SoftwareParameters.ServerConfig != null && Server.SoftwareParameters.ServerConfig.MiningCoins.Any(coin => coin.Algorithm.Equals(MiningAlgo, StringComparison.OrdinalIgnoreCase)))
                {
                    Server.MiningCoin miningCoin = Server.SoftwareParameters.ServerConfig.MiningCoins.First(coin => coin.Algorithm.Equals(MiningAlgo, StringComparison.OrdinalIgnoreCase));

                    HashrateValue = value / miningCoin.DefaultHashMuCoef;
                    HashrateString = Math.Round(value / miningCoin.DefaultHashMuCoef, 2) + " " + miningCoin.DefaultHashMuString;
                }

                _ = Dispatcher.BeginInvoke((Action)(() => { OverviewDeviceSimplified.RefreshDataContext(this); }));
            }
        }

        private decimal hashrateValue = -1;

        public decimal HashrateValue
        { get => hashrateValue; set { hashrateValue = value; Dispatcher.BeginInvoke((Action)(() => { OverviewDeviceSimplified.RefreshDataContext(this); })); } }

        private string hashrateString = "0 H/s";

        public string HashrateString
        { get => hashrateString; set { hashrateString = value; Dispatcher.BeginInvoke((Action)(() => { OverviewDeviceSimplified.RefreshDataContext(this); })); } }

        private bool isSelected = true;

        public bool IsSelected
        { get => isSelected; set { isSelected = value; OnChanged(null); } }

        private bool isMining = true;

        public bool IsMining
        { get => isMining; set { isMining = value; if (isMining) { hashrate_timer.Start(); } else { hashrate_timer.Stop(); } } }

        public PackIconKind IconKind { get; set; }
    }
}