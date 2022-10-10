using System.Windows;
using System.Windows.Controls;
using TrueMiningDesktop.Core;

namespace TrueMiningDesktop.ViewModel
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
				case "RVN_binance":
					{
						Tools.OpenLinkInBrowser("https://www.binance.com/pt-BR/register?ref=15277911");
					}
					break;

				case "RVN_github_core":
					{
						Tools.OpenLinkInBrowser("https://github.com/RavenProject/Ravencoin/releases");
					}
					break;

				case "RVN_github_electrum":
					{
						Tools.OpenLinkInBrowser("https://github.com/Electrum-RVN-SIG/Electrum-Ravencoin/releases");
					}
					break;

				case "RVN_dot_org_site":
					{
						Tools.OpenLinkInBrowser("https://ravencoin.org/wallet/");
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

				case "DOGE_okex":
					{
						Tools.OpenLinkInBrowser("https://www.okex.com/join/9619437");
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

				case "DGB_github":
					{
						Tools.OpenLinkInBrowser("https://github.com/DigiByte-Core/digibyte/releases");
					}
					break;

				case "DGB_binance":
					{
						Tools.OpenLinkInBrowser("https://www.binance.com/pt-BR/register?ref=15277911");
					}
					break;

				case "DGB_okex":
					{
						Tools.OpenLinkInBrowser("https://www.okex.com/join/9619437");
					}
					break;

				case "DGB_crex24":
					{
						Tools.OpenLinkInBrowser("https://crex24.com/?refid=ves323roqu7eousuh5wm");
					}
					break;

				default: MessageBox.Show("Something went wrong"); break;
			}
		}
	}
}