using MaterialDesignThemes.Wpf;
using System;
using TrueMiningDesktop.Core;
using TrueMiningDesktop.ViewModel;

namespace TrueMiningDesktop.Janelas
{
    public class DeviceInfo : OverviewDeviceSimplified
    {
        public OverviewDeviceSimplified OverviewDeviceSimplified = new OverviewDeviceSimplified();

        public event EventHandler PropertieChangedDevInfo;

        private readonly Hashrate_timer hashrate_timer;

        public DeviceInfo(string alias, string name, string miningAlgo = null, bool isSelected = false, decimal hashrate = -1, PackIconKind iconKind = PackIconKind.Cpu64Bit)
        {
            Alias = alias;
            DeviceName = name;
            MiningAlgo = miningAlgo;
            Hashrate = hashrate;
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
            Hashrate = hashrate_timer.Hashrate;
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

        public string Alias { get; private set; }
        public string DeviceName { get; private set; }
        public string MiningAlgo { get; set; }

        private decimal hashrate = -1;
        public decimal Hashrate { get { return hashrate; } set { hashrate = value; Dispatcher.BeginInvoke((Action)(() => { OverviewDeviceSimplified.RefreshDataContext(this); })); } }

        private bool isSelected = true;
        public bool IsSelected { get { return isSelected; } set { isSelected = value; OnChanged(null); } }

        private bool isMining = true;
        public bool IsMining { get { return isMining; } set { isMining = value; if (isMining) { hashrate_timer.Start(); } else { hashrate_timer.Stop(); } } }
        public PackIconKind IconKind { get; set; }
    }
}