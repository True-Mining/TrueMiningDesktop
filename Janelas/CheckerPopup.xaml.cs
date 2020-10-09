using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using True_Mining_Desktop.Core;
using True_Mining_Desktop.Server;

namespace True_Mining_Desktop.Janelas
{
    /// <summary>
    /// Lógica interna para UpdateWindow.xaml
    /// </summary>
    public partial class CheckerPopup : Window
    {
        public bool Tape = false;
        private Thread ThreadChecker = new Thread(() => { });

        public CheckerPopup(string toCheck = "all")
        {
            InitializeComponent();
            this.Height = 0;
            this.BorderBrush.Opacity = 0;
            this.ShowInTaskbar = false;

            ThreadChecker = new Thread(() => Checker(new Uri("https://truemining.online/TrueMiningDesktop.json"), toCheck));
            ThreadChecker.Start();
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

        public void Checker(Uri uri, string toCheck)
        {
            Tape = true;

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                ShowOrNot();

                Application.Current.MainWindow.PreviewMouseDown += MainWindow_GotMouseCapture; ;

                Application.Current.MainWindow.IsVisibleChanged += MainWindow_IsVisibleChanged;
                Application.Current.MainWindow.StateChanged += MainWindow_StateChanged;
                Application.Current.MainWindow.Activated += MainWindow_Activated; ;
            });

            Application.Current.Dispatcher.Invoke(new Action(() => MainWindow.DispararEvento()));

            ProgressBar_Value = 0;
            ProgressBar_IsIndeterminate = true;
            FileName = "Initializing";
            StatusTitle = "Checking Instalation";
            Thread.Sleep(10);

            while (!Tools.IsConnected()) { Thread.Sleep(2000); }
            try
            {
                string[] arquivosOdl = Directory.GetFiles(Environment.CurrentDirectory, "*.old", SearchOption.AllDirectories);
                foreach (var arq in arquivosOdl)
                {
                    if (!Tools.IsFileLocked(new FileInfo(arq))) { File.Delete(arq); }
                }
                string[] arquivosDl = Directory.GetFiles(Environment.CurrentDirectory, "*.dl", SearchOption.AllDirectories);
                foreach (var arq in arquivosDl)
                {
                    if (!Tools.IsFileLocked(new FileInfo(arq))) { File.Delete(arq); }
                }

                SoftwareParameters.Update(uri);

                if (!File.Exists(Environment.CurrentDirectory + @"\DoNotUpdate") && (toCheck == "all" || toCheck == "TrueMining"))
                {
                    FileName = "Checking True Mining Version";
                    Thread.Sleep(20);

                    foreach (FileDetails file in SoftwareParameters.ServerConfig.TrueMiningFiles.files)
                    {
                        FileName = "Checking Files";
                        Thread.Sleep(20);

                        file.path = Tools.FormatPath(file.path);

                        if (!File.Exists(file.path + file.fileName) || Tools.FileSHA256(file.path + file.fileName) != file.sha256)
                        {
                            Downloader(file.dlLink, file.path, file.fileName, file.sha256);
                            needRestart = true;
                        }
                    }

                    if (needRestart)
                    {
                        ProgressBar_Value = 0;
                        FileName = "Restarting";
                        StatusTitle = "Complete update, restart required";
                        User.Settings.SettingsSaver(true);
                        Thread.Sleep(3000);
                        Tape = false;

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
                        Thread.Sleep(20);

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
            Thread.Sleep(100);

            Dispatcher.BeginInvoke((Action)(() => { this.Close(); }));
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => { this.Focus(); }));
        }

        private void MainWindow_GotMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.Activate();
            this.Focus();
        }

        private void MainWindow_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Activate();
            this.Focus();
            Application.Current.Dispatcher.Invoke(new Action(() => MainWindow.clicado = false));
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            ShowOrNot();
        }

        private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ShowOrNot();
        }

        public void Downloader(string url, string path, string fileName, string sha256)
        {
            FileName = fileName;
            StatusTitle = "Downloading necessary files";
            Thread.Sleep(20);

            ProgressBar_IsIndeterminate = true;

            int count = 0;

            while (!File.Exists(path + fileName) || Tools.FileSHA256(path + fileName) != sha256)
            {
                if (count > 2) { new Task(() => MessageBox.Show("An unexpected error has occurred. Check your internet and add the main folder of True Mining in the exceptions / exclusions of your antivirus, firewall and windows defender")).Start(); return; }
                count++;

                while (!Tools.IsConnected()) { Thread.Sleep(2000); }

                WebClient webClient = new WebClient();

                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;

                webClient.DownloadFileAsync(new Uri(url), path + fileName + ".dl");

                while (webClient.IsBusy) { Thread.Sleep(100); }

                if (String.Compare(Tools.FileSHA256(path + fileName + ".dl"), sha256, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (Tools.IsFileLocked(new FileInfo(path + fileName)))
                    {
                        File.Move(path + fileName, path + fileName + ".old", true);
                    }
                    try
                    {
                        File.Move(path + fileName + ".dl", path + fileName, true);
                    }
                    catch { File.Move(path + fileName, path + fileName + ".old", true); File.Move(path + fileName + ".dl", path + fileName, true); }
                }
                if (!File.Exists(path + fileName) || Tools.FileSHA256(path + fileName) != sha256) { /* mensagem de erro; Notify.Invoke(null, null);*/ }
            }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressBar_Value = e.ProgressPercentage;
            ProgressBar_IsIndeterminate = true;
        }

        private void ShowOrNot()
        {
            if (Application.Current.MainWindow.IsVisible)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    this.Height = 100;
                    BorderBrush.Opacity = 100;

                    this.ShowInTaskbar = true;

                    this.Activate();
                    this.Focus();
                });
            }
            else
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    this.Height = 0;
                    BorderBrush.Opacity = 0;

                    this.ShowInTaskbar = false;
                });
            }

            this.WindowState = Application.Current.MainWindow.WindowState;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Tape = false;
            Application.Current.Dispatcher.Invoke(new Action(() => MainWindow.DispararEvento()));
            ThreadChecker.Interrupt();
        }
    }
}