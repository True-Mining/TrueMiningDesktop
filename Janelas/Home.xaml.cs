using System.Windows;
using System.Windows.Controls;
using True_Mining_v4.Core;

namespace True_Mining_v4.Janelas
{
    /// <summary>
    /// Interação lógica para Home.xam
    /// </summary>
    public partial class Home : UserControl
    {
        public Home()
        {
            InitializeComponent();
            DataContext = User.Settings.User;
        }

        public void StartStopMining_Click(object sender, RoutedEventArgs e)
        {
            if (Miner.IsMining)
            {
                Miner.StopMiner();
            }
            else
            {
                Miner.StartMiner();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}