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
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            WrapPanel_ManualConfig.IsEnabled = false;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            WrapPanel_ManualConfig.IsEnabled = true;
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (User.Settings.Device.cuda.AlgorithmsList.Contains(AlgorithmComboBox.Text)) { User.Settings.Device.cuda.Algorithm = AlgorithmComboBox.Text; }
        }
    }
}