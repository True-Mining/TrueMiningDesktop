using System.Windows.Controls;

namespace TrueMiningDesktop.Janelas
{
    /// <summary>
    /// Interação lógica para Settings.xam
    /// </summary>
    public partial class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();
            DataContext = User.Settings.User;
        }
    }
}