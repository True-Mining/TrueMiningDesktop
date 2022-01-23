using System.Windows;
using System.Windows.Controls;

namespace TrueMiningDesktop.Janelas.SubMenuSettings
{
    /// <summary>
    /// Interação lógica para SettingsOPENCL.xam
    /// </summary>
    public partial class SettingsOPENCL : UserControl
    {
        public SettingsOPENCL()
        {
            InitializeComponent();
            DataContext = User.Settings.Device.opencl;
            AlgorithmComboBox.SelectedValue = User.Settings.Device.opencl.Algorithm;
            ChipFansFullspeedTempTxt.Text = User.Settings.Device.opencl.ChipFansFullspeedTemp > 0 ? User.Settings.Device.opencl.ChipFansFullspeedTemp.ToString() + " °C" : "auto";
            MemFansFullspeedTempTxt.Text = User.Settings.Device.opencl.MemFansFullspeedTemp > 0 ? User.Settings.Device.opencl.MemFansFullspeedTemp.ToString() + " °C" : "auto";
            ChipPauseMiningTempTxt.Text = User.Settings.Device.opencl.ChipPauseMiningTemp > 0 ? User.Settings.Device.opencl.ChipPauseMiningTemp.ToString() + " °C" : "auto";
            MemPauseMiningTempTxt.Text = User.Settings.Device.opencl.MemPauseMiningTemp > 0 ? User.Settings.Device.opencl.MemPauseMiningTemp.ToString() + " °C" : "auto";
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            WrapPanel_ManualConfig.IsEnabled = false;
            User.OpenClSettings defaultSettings = new User.OpenClSettings();
            User.Settings.Device.opencl.Algorithm = defaultSettings.Algorithm;
            User.Settings.Device.opencl.ChipFansFullspeedTemp = defaultSettings.ChipFansFullspeedTemp;
            User.Settings.Device.opencl.MemFansFullspeedTemp = defaultSettings.MemFansFullspeedTemp;
            User.Settings.Device.opencl.ChipPauseMiningTemp = defaultSettings.ChipPauseMiningTemp;
            User.Settings.Device.opencl.MemPauseMiningTemp = defaultSettings.MemPauseMiningTemp;

            AlgorithmComboBox.SelectedValue = User.Settings.Device.opencl.Algorithm;
            ChipFansFullspeedTempTxt.Text = User.Settings.Device.opencl.ChipFansFullspeedTemp > 0 ? User.Settings.Device.opencl.ChipFansFullspeedTemp.ToString() + " °C" : "auto";
            MemFansFullspeedTempTxt.Text = User.Settings.Device.opencl.MemFansFullspeedTemp > 0 ? User.Settings.Device.opencl.MemFansFullspeedTemp.ToString() + " °C" : "auto";
            ChipPauseMiningTempTxt.Text = User.Settings.Device.opencl.ChipPauseMiningTemp > 0 ? User.Settings.Device.opencl.ChipPauseMiningTemp.ToString() + " °C" : "auto";
            MemPauseMiningTempTxt.Text = User.Settings.Device.opencl.MemPauseMiningTemp > 0 ? User.Settings.Device.opencl.MemPauseMiningTemp.ToString() + " °C" : "auto";
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            WrapPanel_ManualConfig.IsEnabled = true;
        }

        private void ChipFansFullspeedTempPlusNumber_Click(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.opencl.ChipFansFullspeedTemp++;

            if (User.Settings.Device.opencl.ChipFansFullspeedTemp < 0) { User.Settings.Device.opencl.ChipFansFullspeedTemp = 0; }
            if (User.Settings.Device.opencl.ChipFansFullspeedTemp > 100) { User.Settings.Device.opencl.ChipFansFullspeedTemp = 100; }

            ChipFansFullspeedTempTxt.Text = User.Settings.Device.opencl.ChipFansFullspeedTemp > 0 ? User.Settings.Device.opencl.ChipFansFullspeedTemp.ToString() + " °C" : "auto";
        }

        private void ChipFansFullspeedTempMinusNumber_Click(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.opencl.ChipFansFullspeedTemp--;

            if (User.Settings.Device.opencl.ChipFansFullspeedTemp < 0) { User.Settings.Device.opencl.ChipFansFullspeedTemp = 0; }
            if (User.Settings.Device.opencl.ChipFansFullspeedTemp > 100) { User.Settings.Device.opencl.ChipFansFullspeedTemp = 100; }

            ChipFansFullspeedTempTxt.Text = User.Settings.Device.opencl.ChipFansFullspeedTemp > 0 ? User.Settings.Device.opencl.ChipFansFullspeedTemp.ToString() + " °C" : "auto";
        }

        private void MemFansFullspeedTempPlusNumber_Click(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.opencl.MemFansFullspeedTemp++;

            if (User.Settings.Device.opencl.MemFansFullspeedTemp < 0) { User.Settings.Device.opencl.MemFansFullspeedTemp = 0; }
            if (User.Settings.Device.opencl.MemFansFullspeedTemp > 100) { User.Settings.Device.opencl.MemFansFullspeedTemp = 100; }

            MemFansFullspeedTempTxt.Text = User.Settings.Device.opencl.MemFansFullspeedTemp > 0 ? User.Settings.Device.opencl.MemFansFullspeedTemp.ToString() + " °C" : "auto";
        }

        private void MemFansFullspeedTempMinusNumber_Click(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.opencl.MemFansFullspeedTemp--;

            if (User.Settings.Device.opencl.MemFansFullspeedTemp < 0) { User.Settings.Device.opencl.MemFansFullspeedTemp = 0; }
            if (User.Settings.Device.opencl.MemFansFullspeedTemp > 100) { User.Settings.Device.opencl.MemFansFullspeedTemp = 100; }

            MemFansFullspeedTempTxt.Text = User.Settings.Device.opencl.MemFansFullspeedTemp > 0 ? User.Settings.Device.opencl.MemFansFullspeedTemp.ToString() + " °C" : "auto";
        }

        private void ChipPauseMiningTempPlusNumber_Click(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.opencl.ChipPauseMiningTemp++;

            if (User.Settings.Device.opencl.ChipPauseMiningTemp < 0) { User.Settings.Device.opencl.ChipPauseMiningTemp = 0; }
            if (User.Settings.Device.opencl.ChipPauseMiningTemp > 100) { User.Settings.Device.opencl.ChipPauseMiningTemp = 100; }

            ChipPauseMiningTempTxt.Text = User.Settings.Device.opencl.ChipPauseMiningTemp > 0 ? User.Settings.Device.opencl.ChipPauseMiningTemp.ToString() + " °C" : "auto";
        }

        private void ChipPauseMiningTempMinusNumber_Click(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.opencl.ChipPauseMiningTemp--;

            if (User.Settings.Device.opencl.ChipPauseMiningTemp < 0) { User.Settings.Device.opencl.ChipPauseMiningTemp = 0; }
            if (User.Settings.Device.opencl.ChipPauseMiningTemp > 100) { User.Settings.Device.opencl.ChipPauseMiningTemp = 100; }

            ChipPauseMiningTempTxt.Text = User.Settings.Device.opencl.ChipPauseMiningTemp > 0 ? User.Settings.Device.opencl.ChipPauseMiningTemp.ToString() + " °C" : "auto";
        }

        private void MemPauseMiningTempPlusNumber_Click(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.opencl.MemPauseMiningTemp++;

            if (User.Settings.Device.opencl.MemPauseMiningTemp < 0) { User.Settings.Device.opencl.MemPauseMiningTemp = 0; }
            if (User.Settings.Device.opencl.MemPauseMiningTemp > 100) { User.Settings.Device.opencl.MemPauseMiningTemp = 100; }

            MemPauseMiningTempTxt.Text = User.Settings.Device.opencl.MemPauseMiningTemp > 0 ? User.Settings.Device.opencl.MemPauseMiningTemp.ToString() + " °C" : "auto";
        }

        private void MemPauseMiningTempMinusNumber_Click(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.opencl.MemPauseMiningTemp--;

            if (User.Settings.Device.opencl.MemPauseMiningTemp < 0) { User.Settings.Device.opencl.MemPauseMiningTemp = 0; }
            if (User.Settings.Device.opencl.MemPauseMiningTemp > 100) { User.Settings.Device.opencl.MemPauseMiningTemp = 100; }

            MemPauseMiningTempTxt.Text = User.Settings.Device.opencl.MemPauseMiningTemp > 0 ? User.Settings.Device.opencl.MemPauseMiningTemp.ToString() + " °C" : "auto";
        }
    }
}