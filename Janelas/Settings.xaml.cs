using System.Windows.Controls;

namespace True_Mining_Desktop.Janelas
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