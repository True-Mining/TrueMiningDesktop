using System;
using System.Windows;
using System.Windows.Media;
using TrueMiningDesktop.Core;
using TrueMiningDesktop.Janelas;

namespace TrueMiningDesktop.ViewModel
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

            if ((bool)ovDeviceIsSelected.IsChecked)
            {
                if (data.Hashrate > 0)
                {
                    ovHashrate.FontSize = 18;
                    ovHashrate.Content = data.Hashrate.ToString() + " H/s";
                }
                else
                {
                    if (Miner.IsMining)
                    {
                        if (Miner.StartedSince > DateTime.UtcNow.AddMinutes(-1))
                        {
                            ovHashrate.FontSize = 17;
                            ovHashrate.Content = "starting";
                        }
                        else
                        {
                            ovHashrate.FontSize = 17;
                            ovHashrate.Content = "error";
                        }
                    }
                    else
                    {
                        ovHashrate.FontSize = 18;
                        ovHashrate.Content = "-";
                    }
                }
            }
            else
            {
                ovHashrate.Content = "-";
            }
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