using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using True_Mining_v4.Core;
using True_Mining_v4.Server;

namespace True_Mining_v4.Janelas
{
    /// <summary>
    /// Lógica interna para UpdateWindow.xaml
    /// </summary>
    public partial class CheckerPopup : Window
    {
        public CheckerPopup(string toCheck = "all", bool hide = false)
        {
            InitializeComponent();

            new Thread(() => Checker(new Uri("https://truemining.online/v4.json"), toCheck, hide)).Start();
        }

        private bool property_finish = false;
        private string property_statusTitle = "Verifiying Files";
        private string property_fileName = "...";
        private int property_progressBar_Value = 0;
        private bool property_progressBar_IsIndeterminate = true;

        public bool Finish { get { return property_finish; } set { property_finish = value; } }
        public string StatusTitle { get { return property_statusTitle; } set { property_statusTitle = value; Dispatcher.BeginInvoke((Action)(() => { statusTitle.Content = value; })); } }
        public string FileName { get { return property_fileName; } set { property_fileName = value; Dispatcher.BeginInvoke((Action)(() => { fileName.Content = value; })); } }
        public int ProgressBar_Value { get { return property_progressBar_Value; } set { property_progressBar_Value = value; Dispatcher.BeginInvoke((Action)(() => { progressBar.Value = value; })); } }
        public bool ProgressBar_IsIndeterminate { get { return property_progressBar_IsIndeterminate; } set { property_progressBar_IsIndeterminate = value; Dispatcher.BeginInvoke((Action)(() => { progressBar.IsIndeterminate = value; })); } }
        private bool needRestart = false;

        public bool allDone = false;

        public void Checker(Uri uri, string toCheck, bool hide)
        {
            if (hide)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    this.Height = 0;
                    BorderBrush.Opacity = 0;
                });
            }

            ProgressBar_Value = 0;
            ProgressBar_IsIndeterminate = true;
            FileName = "Initializing";
            StatusTitle = "Checking Instalation";
            Thread.Sleep(100);

            while (!Tools.IsConnected()) { Thread.Sleep(2000); }
            try
            {
                string[] arquivos = Directory.GetFiles(Environment.CurrentDirectory, "*.old", SearchOption.AllDirectories);
                foreach (var arq in arquivos)
                {
                    if (!Tools.IsFileLocked(new FileInfo(arq))) { File.Delete(arq); }
                }

                SoftwareParameters.Update(uri);

                if (toCheck == "all" || toCheck == "TrueMining")
                {
                    FileName = "Checking True Mining Version";
                    Thread.Sleep(200);

                    var versionRunning = new Version(Convert.ToString(typeof(String).Assembly.GetName().Version));
                    var versionUpdate = new Version(SoftwareParameters.ServerConfig.TrueMiningFiles.assemblyVersion);

                    if (versionRunning.CompareTo(versionUpdate) < 0)
                    {
                        foreach (FileDetails file in SoftwareParameters.ServerConfig.TrueMiningFiles.files)
                        {
                            FileName = "Checking Files";
                            Thread.Sleep(200);

                            file.path = Tools.FormatPath(file.path);

                            if (!File.Exists(file.path + file.fileName) || Tools.FileSHA256(file.path + file.fileName) != file.sha256)
                            {
                                Downloader(file.dlLink, file.path, file.fileName, file.sha256);
                                needRestart = true;
                            }
                        }
                    }

                    if (needRestart)
                    {
                        ProgressBar_Value = 0;
                        FileName = "Restarting";
                        StatusTitle = "Complete update, restart required";
                        Thread.Sleep(3000);

                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                            Application.Current.Shutdown();
                        });
                    }
                }

                if (toCheck == "all" || toCheck == "ThirdPartyBinaries")
                {
                    foreach (FileDetails file in SoftwareParameters.ServerConfig.ThirdPartyBinaries.files)
                    {
                        FileName = "Checking Files";
                        Thread.Sleep(200);

                        file.path = Tools.FormatPath(file.path);
                        if (!File.Exists(file.path + file.fileName) || Tools.FileSHA256(file.path + file.fileName) != file.sha256)
                        {
                            Downloader(file.dlLink, file.path, file.fileName, file.sha256);
                        }
                    }
                }
            }
            catch { }

            FileName = "Complete";
            Thread.Sleep(500);

            Dispatcher.BeginInvoke((Action)(() => { this.Close(); }));
        }

        public void Downloader(string url, string path, string fileName, string sha256)
        {
            FileName = fileName;
            StatusTitle = "Downloading necessary files";
            Thread.Sleep(50);

            ProgressBar_IsIndeterminate = true;

            int count = 0;

            while (!File.Exists(path + fileName) || Tools.FileSHA256(path + fileName) != sha256)
            {
                if (count > 2) { new Task(() => MessageBox.Show("An unexpected error has occurred. Check your internet and add the main folder of True Mining in the exceptions / exclusions of your antivirus, firewall and windows defender")).Start(); return; }
                count++;

                while (!Tools.IsConnected()) { Thread.Sleep(2000); }

                WebClient webClient = new WebClient();

                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;

                webClient.DownloadFileAsync(new Uri(url), System.IO.Path.GetTempPath() + fileName);

                while (webClient.IsBusy) { Thread.Sleep(1000); }

                if (String.Compare(Tools.FileSHA256(System.IO.Path.GetTempPath() + fileName), sha256, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (Tools.IsFileLocked(new FileInfo(path + fileName)))
                    {
                        File.Move(path + fileName, path + fileName + ".old", true);
                    }
                    File.Move(System.IO.Path.GetTempPath() + fileName, path + fileName, true);
                }
                if (!File.Exists(path + fileName) || Tools.FileSHA256(path + fileName) != sha256) { /* mensagem de erro; Notify.Invoke(null, null);*/ }
            }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressBar_Value = e.ProgressPercentage;
            ProgressBar_IsIndeterminate = true;
        }
    }
}