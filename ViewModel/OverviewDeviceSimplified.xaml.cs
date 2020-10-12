using System;
using System.Windows;
using System.Windows.Media;
using True_Mining_Desktop.Core;
using True_Mining_Desktop.Janelas;

namespace True_Mining_Desktop.ViewModel
{
    /// <summary>
    /// Interação lógica para OverviewDeviceSimplified.xam
    /// </summary>
    public partial class OverviewDeviceSimplified
    {
        public event EventHandler PropertieChanged;

        protected virtual void OnChanged(EventArgs e)
        {
            PropertieChanged?.Invoke(this, e);
        }

        public OverviewDeviceSimplified()
        {
            InitializeComponent();
        }

        public void RefreshDataContext(DeviceInfo data)
        {
            ovIcon.Kind = data.IconKind;
            ovDeviceName.Content = data.DeviceName;
            ovMiningAlgo.Content = data.MiningAlgo;
            ovDeviceIsSelected.IsChecked = data.IsSelected;

            if (data.Hashrate > 0) { ovHashrate.Content = data.Hashrate.ToString() + " H/s"; }
            else { ovHashrate.Content = "-"; }
        }

        private void DeviceIsSelected_Checked(object sender, RoutedEventArgs e)
        {
            OnChanged(null);

            ovIcon.Foreground = Brushes.Black;

            if (Miner.IsMining)
            {
                Miner.StopMiner();
                Miner.StartMiner();
            }
        }

        private void DeviceIsSelected_Unchecked(object sender, RoutedEventArgs e)
        {
            OnChanged(null);

            ovIcon.Foreground = Brushes.Gray;

            if (Miner.IsMining)
            {
                Miner.StopMiner();
                Miner.StartMiner();
            }
        }
    }
}