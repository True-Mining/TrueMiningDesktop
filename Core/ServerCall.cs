namespace True_Mining_v4.Core
{
    internal class ServerCall
    {
        /*
        new WebClient webClient;

        TimeSpan UltimaUpdateParametros = new TimeSpan(0, 0, 0);
        public void PegarParametros()
        {
            if (UltimaUpdateParametros == new TimeSpan(0, 0, 0) || UltimaUpdateParametros.Ticks < DateTime.UtcNow.AddMinutes(-10).Ticks)
            {
                while (!IsConnected() || pegandoParametros) { Thread.Sleep(5000); }

                pegandoParametros = true;
                string parametrosServerTemp;
                tabHardware.Select();

                while (webClient.IsBusy) { Thread.Sleep(500); }
                try
                {
                    webClient.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.90 Safari/537.36");

                    parametrosServerTemp = webClient.DownloadString(new Uri("https://truemining.online/TMparametros"));
                    dynamic parametrosTempDynamic = JsonConvert.DeserializeObject(parametrosServerTemp);

                    string versionTemp = parametrosTempDynamic.mainSoftware.version;

                    if (String.IsNullOrEmpty(labelAtualVersion.Text)) { labelAtualVersion.Text = "Versão " + Application.ProductVersion; }

                    parametrosServer = parametrosServerTemp;
                    dynamicParametros = JsonConvert.DeserializeObject(parametrosServer);

                    if (!String.Equals(versionTemp, Application.ProductVersion))
                    {
                        BuscarUpdate();
                    }

                    UltimaUpdateParametros = new TimeSpan(DateTime.UtcNow.Ticks);
                }
                catch (Exception)
                {
                    hashrateLbl.Text = "Esperando por internet";
                    label1.Text = "Aguardando\nconexão com\ninternet";
                }
                pegandoParametros = false;
                webClientSendoUsado = false;
            }
        }

        private struct DadosAPI
        {
            public static dynamic APIpool_allWorkers;
            public static dynamic APIpool_thisMiner;
            public static dynamic APIpool_approximated = null;
            public static dynamic APIcrex24_XMRBTC_orderbook;
            public static dynamic APIcrex24_COINBTC_orderbook;
            public static dynamic APIbitcoinprice;
        }

        private void ConsultarAPI(dynamic parametrosGerais, string coin, int horas)
        {
            webClient.Encoding = System.Text.Encoding.UTF8;

            while (!IsConnected()) { Thread.Sleep(5000); }
            try
            {
                DadosAPI.APIpool_allWorkers = JsonConvert.DeserializeObject(webClient.DownloadString("https://api.nanopool.org/v1/xmr/avghashratelimited/" + parametrosGerais.pool.walletXMR.ToString() + '/' + horas));
                DadosAPI.APIpool_thisMiner = JsonConvert.DeserializeObject(webClient.DownloadString("https://api.nanopool.org/v1/xmr/avghashratelimited/" + parametrosGerais.pool.walletXMR.ToString() + '/' + carteira.Text + '/' + horas));
                DadosAPI.APIbitcoinprice = JsonConvert.DeserializeObject(webClient.DownloadString("https://blockchain.info/ticker"));
                DadosAPI.APIcrex24_XMRBTC_orderbook = JsonConvert.DeserializeObject(webClient.DownloadString(new Uri("https://api.crex24.com/v2/public/orderBook?instrument=XMR-BTC")));
                DadosAPI.APIcrex24_COINBTC_orderbook = JsonConvert.DeserializeObject(webClient.DownloadString(new Uri("https://api.crex24.com/v2/public/orderBook?instrument=" + coin + "-BTC")));

                DadosAPI.APIpool_approximated = JsonConvert.DeserializeObject(webClient.DownloadString(new Uri("https://api.nanopool.org/v1/xmr/approximated_earnings/35")));

                if (DadosAPI.APIpool_approximated == null || String.Equals(DadosAPI.APIpool_approximated.status.ToString(), "false", StringComparison.OrdinalIgnoreCase))
                {
                    DadosAPI.APIpool_approximated = JsonConvert.DeserializeObject("{ 'data': { 'hour': { 'coins': '" + parametrosGerais.hour_rental + "' } } }");
                }
            }
            catch (Exception e)
            {
                TrocarProxy();
                ConsultarAPI(parametrosGerais, coin, horas);
            }
        }
        */
    }
}