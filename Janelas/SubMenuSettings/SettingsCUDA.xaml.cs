using System.Windows;
using System.Windows.Controls;

namespace TrueMiningDesktop.Janelas.SubMenuSettings
{
    /// <summary>
    /// Interação lógica para SettingsCUDA.xam
    /// </summary>
    public partial class SettingsCUDA : UserControl
    {
        public SettingsCUDA()
        {
            InitializeComponent();
            DataContext = User.Settings.Device.cuda;
            AlgorithmComboBox.SelectedValue = User.Settings.Device.cuda.Algorithm;
            DisableTempControlCheckBox.IsChecked = User.Settings.Device.cuda.DisableTempControl;
            ChipFansFullspeedTempTxt.Text = User.Settings.Device.cuda.ChipFansFullspeedTemp > 0 ? User.Settings.Device.cuda.ChipFansFullspeedTemp.ToString() + " °C" : "auto";
            MemFansFullspeedTempTxt.Text = User.Settings.Device.cuda.MemFansFullspeedTemp > 0 ? User.Settings.Device.cuda.MemFansFullspeedTemp.ToString() + " °C" : "auto";
            ChipPauseMiningTempTxt.Text = User.Settings.Device.cuda.ChipPauseMiningTemp > 0 ? User.Settings.Device.cuda.ChipPauseMiningTemp.ToString() + " °C" : "auto";
            MemPauseMiningTempTxt.Text = User.Settings.Device.cuda.MemPauseMiningTemp > 0 ? User.Settings.Device.cuda.MemPauseMiningTemp.ToString() + " °C" : "auto";
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            WrapPanel_ManualConfig.IsEnabled = false;
            User.CudaSettings defaultSettings = new User.CudaSettings();
            User.Settings.Device.cuda.Algorithm = defaultSettings.Algorithm;
            User.Settings.Device.cuda.ChipFansFullspeedTemp = defaultSettings.ChipFansFullspeedTemp;
            User.Settings.Device.cuda.MemFansFullspeedTemp = defaultSettings.MemFansFullspeedTemp;
            User.Settings.Device.cuda.ChipPauseMiningTemp = defaultSettings.ChipPauseMiningTemp;
            User.Settings.Device.cuda.MemPauseMiningTemp = defaultSettings.MemPauseMiningTemp;

            AlgorithmComboBox.SelectedValue = User.Settings.Device.cuda.Algorithm;
            ChipFansFullspeedTempTxt.Text = User.Settings.Device.cuda.ChipFansFullspeedTemp > 0 ? User.Settings.Device.cuda.ChipFansFullspeedTemp.ToString() + " °C" : "auto";
            MemFansFullspeedTempTxt.Text = User.Settings.Device.cuda.MemFansFullspeedTemp > 0 ? User.Settings.Device.cuda.MemFansFullspeedTemp.ToString() + " °C" : "auto";
            ChipPauseMiningTempTxt.Text = User.Settings.Device.cuda.ChipPauseMiningTemp > 0 ? User.Settings.Device.cuda.ChipPauseMiningTemp.ToString() + " °C" : "auto";
            MemPauseMiningTempTxt.Text = User.Settings.Device.cuda.MemPauseMiningTemp > 0 ? User.Settings.Device.cuda.MemPauseMiningTemp.ToString() + " °C" : "auto";
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            WrapPanel_ManualConfig.IsEnabled = true;
        }
        private void CheckBoxDisableTempControl_Checked(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.cuda.DisableTempControl = true;
        }

        private void CheckBoxDisableTempControl_Unchecked(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.cuda.DisableTempControl = false;
        }

        private void ChipFansFullspeedTempPlusNumber_Click(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.cuda.ChipFansFullspeedTemp++;

            if (User.Settings.Device.cuda.ChipFansFullspeedTemp > 100) { User.Settings.Device.cuda.ChipFansFullspeedTemp = 100; }

            ChipFansFullspeedTempTxt.Text = User.Settings.Device.cuda.ChipFansFullspeedTemp > 0 ? User.Settings.Device.cuda.ChipFansFullspeedTemp.ToString() + " °C" : "auto";
        }

        private void ChipFansFullspeedTempMinusNumber_Click(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.cuda.ChipFansFullspeedTemp--;

            if (User.Settings.Device.cuda.ChipFansFullspeedTemp < 0) { User.Settings.Device.cuda.ChipFansFullspeedTemp = 0; }

            ChipFansFullspeedTempTxt.Text = User.Settings.Device.cuda.ChipFansFullspeedTemp > 0 ? User.Settings.Device.cuda.ChipFansFullspeedTemp.ToString() + " °C" : "auto";
        }

        private void MemFansFullspeedTempPlusNumber_Click(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.cuda.MemFansFullspeedTemp++;

            if (User.Settings.Device.cuda.MemFansFullspeedTemp > 100) { User.Settings.Device.cuda.MemFansFullspeedTemp = 100; }

            MemFansFullspeedTempTxt.Text = User.Settings.Device.cuda.MemFansFullspeedTemp > 0 ? User.Settings.Device.cuda.MemFansFullspeedTemp.ToString() + " °C" : "auto";
        }

        private void MemFansFullspeedTempMinusNumber_Click(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.cuda.MemFansFullspeedTemp--;

            if (User.Settings.Device.cuda.MemFansFullspeedTemp < 0) { User.Settings.Device.cuda.MemFansFullspeedTemp = 0; }

            MemFansFullspeedTempTxt.Text = User.Settings.Device.cuda.MemFansFullspeedTemp > 0 ? User.Settings.Device.cuda.MemFansFullspeedTemp.ToString() + " °C" : "auto";
        }

        private void ChipPauseMiningTempPlusNumber_Click(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.cuda.ChipPauseMiningTemp++;

            if (User.Settings.Device.cuda.ChipPauseMiningTemp > 100) { User.Settings.Device.cuda.ChipPauseMiningTemp = 100; }

            ChipPauseMiningTempTxt.Text = User.Settings.Device.cuda.ChipPauseMiningTemp > 0 ? User.Settings.Device.cuda.ChipPauseMiningTemp.ToString() + " °C" : "auto";
        }

        private void ChipPauseMiningTempMinusNumber_Click(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.cuda.ChipPauseMiningTemp--;

            if (User.Settings.Device.cuda.ChipPauseMiningTemp < 0) { User.Settings.Device.cuda.ChipPauseMiningTemp = 0; }

            ChipPauseMiningTempTxt.Text = User.Settings.Device.cuda.ChipPauseMiningTemp > 0 ? User.Settings.Device.cuda.ChipPauseMiningTemp.ToString() + " °C" : "auto";
        }

        private void MemPauseMiningTempPlusNumber_Click(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.cuda.MemPauseMiningTemp++;

            if (User.Settings.Device.cuda.MemPauseMiningTemp > 100) { User.Settings.Device.cuda.MemPauseMiningTemp = 100; }

            MemPauseMiningTempTxt.Text = User.Settings.Device.cuda.MemPauseMiningTemp > 0 ? User.Settings.Device.cuda.MemPauseMiningTemp.ToString() + " °C" : "auto";
        }

        private void MemPauseMiningTempMinusNumber_Click(object sender, RoutedEventArgs e)
        {
            User.Settings.Device.cuda.MemPauseMiningTemp--;

            if (User.Settings.Device.cuda.MemPauseMiningTemp < 0) { User.Settings.Device.cuda.MemPauseMiningTemp = 0; }

            MemPauseMiningTempTxt.Text = User.Settings.Device.cuda.MemPauseMiningTemp > 0 ? User.Settings.Device.cuda.MemPauseMiningTemp.ToString() + " °C" : "auto";
        }
    }
}