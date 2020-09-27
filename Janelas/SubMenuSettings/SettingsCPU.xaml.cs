using System.Windows;
using System.Windows.Controls;

namespace True_Mining_v4.Janelas.SubMenuSettings
{
    /// <summary>
    /// Interação lógica para SettingsCPU.xam
    /// </summary>
    public partial class SettingsCPU : UserControl
    {
        public SettingsCPU()
        {
            InitializeComponent();
            DataContext = User.Settings.Device.cpu;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            WrapPanel_ManualConfig.IsEnabled = false;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            WrapPanel_ManualConfig.IsEnabled = true;
        }
    }
}