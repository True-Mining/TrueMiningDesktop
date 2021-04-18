using System.Windows;
using System.Windows.Controls;
using True_Mining_Desktop.Core;

namespace True_Mining_Desktop.ViewModel
{
    /// <summary>
    /// Interação lógica para PageCreateWallet.xam
    /// </summary>
    public partial class PageCreateWallet : Window
    {
        public PageCreateWallet()
        {
            InitializeComponent();
        }

        private void button_click(object sender, RoutedEventArgs e)
        {
            string content = null;
            if (sender != null)
            {
                try
                {
                    content = (sender as Button).Name;
                }
                catch { }
            }

            switch (content)
            {
                case "RDCT_github":
                    {
                        Tools.OpenLinkInBrowser("https://github.com/reidocoin/rdctoken/releases");
                    }
                    break;

                case "RDCT_crex24":
                    {
                        Tools.OpenLinkInBrowser("https://crex24.com/?refid=ves323roqu7eousuh5wm");
                    }
                    break;

                case "DOGE_github":
                    {
                        Tools.OpenLinkInBrowser("https://github.com/dogecoin/dogecoin/releases");
                    }
                    break;

                case "DOGE_binance":
                    {
                        Tools.OpenLinkInBrowser("https://www.binance.com/pt-BR/register?ref=15277911");
                    }
                    break;

                case "DOGE_dogechain":
                    {
                        Tools.OpenLinkInBrowser("https://my.dogechain.info");
                    }
                    break;

                case "DOGE_crex24":
                    {
                        Tools.OpenLinkInBrowser("https://crex24.com/?refid=ves323roqu7eousuh5wm");
                    }
                    break;

                default: MessageBox.Show("Something went wrong"); break;
            }
        }
    }
}