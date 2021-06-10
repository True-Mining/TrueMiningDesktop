using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TrueMiningDesktop.Core;
using TrueMiningDesktop.Server;

namespace TrueMiningDesktop.Janelas
{
    /// <summary>
    /// Lógica interna para UpdateWindow.xaml
    /// </summary>
    public partial class CheckerPopup : Window
    {
        public bool Tape = false;
        private readonly Task TaskChecker = new(() => { });

        public CheckerPopup(string toCheck = "all")
        {
            InitializeComponent();
            DataContext = this;
            Height = 0;
            BorderBrush.Opacity = 0;
            ShowInTaskbar = false;

            Tools.PropertyChanged += Tools_PropertyChanged;

            Tools.NotifyPropertyChanged();

            TaskChecker = new Task(() => Checker(new Uri("https://truemining.online/TrueMiningDesktop.json"), toCheck));
            TaskChecker.Start();
        }

        private void Tools_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                torIcon.Visibility = useTor | Tools.UseTor ? Visibility.Visible : Visibility.Collapsed;
                torIcon.Opacity = Tools.TorSharpEnabled ? 1 : Tools.TorSharpProcessesRunning ? 0.7 : Tools.UseTor || User.Settings.User.UseTorSharpOnMining ? 0.4 : 0;
            }));
        }

        private bool property_finish = false;
        private string property_statusTitle = "Verifiying Files";
        private string property_fileName = "...";
        private int property_progressBar_Value = 0;
        private bool property_progressBar_IsIndeterminate = true;
        private string progressDetails_Content = null;
        private Visibility hostFilesAd_Visibility = Visibility.Collapsed;

        public bool Finish { get { return property_finish; } set { property_finish = value; ProgressDetails = null; } }
        public string StatusTitle { get { return property_statusTitle; } set { property_statusTitle = value; Dispatcher.BeginInvoke((Action)(() => { statusTitle.Content = value; ProgressDetails = null; })); } }
        public string FileName { get { return property_fileName; } set { property_fileName = value; ProgressDetails = null; Dispatcher.BeginInvoke((Action)(() => { fileName.Content = new TextBlock() { Text = value, TextWrapping = TextWrapping.WrapWithOverflow }; })); } }
        public int ProgressBar_Value { get { return property_progressBar_Value; } set { property_progressBar_Value = value; Dispatcher.BeginInvoke((Action)(() => { progressBar.Value = value; })); } }
        public bool ProgressBar_IsIndeterminate { get { return property_progressBar_IsIndeterminate; } set { property_progressBar_IsIndeterminate = value; Dispatcher.BeginInvoke((Action)(() => { progressBar.IsIndeterminate = value; })); } }
        public string ProgressDetails { get { return progressDetails_Content; } set { progressDetails_Content = value; Dispatcher.BeginInvoke((Action)(() => { progressDetails.Content = value; })); } }
        public Visibility HostFilesAd_Visibility { get { return hostFilesAd_Visibility; } set { hostFilesAd_Visibility = value; Dispatcher.BeginInvoke((Action)(() => { HostFilesAd.Visibility = value; })); } }

        private bool needRestart = false;

        public bool allDone = false;

        public void Checker(Uri uri, string toCheck)
        {
            Tape = true;

            StateChanged += CheckerPopup_StateChanged;

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                ShowOrNot();

                Application.Current.MainWindow.IsVisibleChanged += MainWindow_IsVisibleChanged;
                Application.Current.MainWindow.StateChanged += MainWindow_StateChanged;
                Application.Current.MainWindow.Activated += MainWindow_Activated;
            });

            Application.Current.Dispatcher.Invoke(new Action(() => MainWindow.DispararEvento()));

            ProgressBar_Value = 0;
            ProgressBar_IsIndeterminate = true;
            FileName = "Initializing";
            StatusTitle = "Checking Instalation";
            Thread.Sleep(10);

            CheckInternet();

            StatusTitle = "Checking Instalation";

            bool trying = true;

            while (trying)
            {
                try
                {
                    FileName = "Removing old files";
                    string[] arquivosOdl = Directory.GetFiles(Environment.CurrentDirectory, "*.old", SearchOption.AllDirectories);
                    foreach (string arq in arquivosOdl)
                    {
                        if (!Tools.IsFileLocked(new FileInfo(arq))) { File.Delete(arq); }
                    }
                    string[] arquivosDl = Directory.GetFiles(Environment.CurrentDirectory, "*.dl", SearchOption.AllDirectories);
                    foreach (string arq in arquivosDl)
                    {
                        if (!Tools.IsFileLocked(new FileInfo(arq))) { File.Delete(arq); }
                    }

                    FileName = "Updating software parameters";
                    SoftwareParameters.Update(uri);

                    if (!(File.Exists(Environment.CurrentDirectory + @"\DoNotUpdate") || (Core.NextStart.Actions.loadedNextStartInstructions.useThisInstructions && Core.NextStart.Actions.loadedNextStartInstructions.ignoreUpdates)) && (toCheck == "all" || toCheck == "TrueMining"))
                    {
                        FileName = "Checking True Mining Version";
                        Thread.Sleep(20);

                        foreach (FileToDownload file in SoftwareParameters.ServerConfig.TrueMiningFiles.Files)
                        {
                            FileName = "Checking Files";
                            Thread.Sleep(20);

                            file.Path = Tools.FormatPath(file.Path);

                            if (!File.Exists(file.Path + file.FileName) || Tools.FileSHA256(file.Path + file.FileName) != file.Sha256)
                            {
                                Downloader(file.DlLink, file.Path, file.FileName, file.Sha256);
                                needRestart = true;
                            }
                        }

                        if (needRestart)
                        {
                            HostFilesAd_Visibility = Visibility.Collapsed;
                            ProgressBar_Value = 0;
                            FileName = "Restarting";
                            StatusTitle = "Complete update, restart required";
                            User.Settings.SettingsSaver(true);
                            Thread.Sleep(3000);
                            Tape = false;

                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                System.Diagnostics.Process TrueMiningAsAdmin = new()
                                {
                                    StartInfo = new System.Diagnostics.ProcessStartInfo()
                                    {
                                        FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,
                                        UseShellExecute = false,
                                    }
                                };
                                if (Tools.HaveADM) { TrueMiningAsAdmin.StartInfo.Verb = "runas"; }

                                try { TrueMiningAsAdmin.Start(); Miner.StopMiner(); } catch (Exception e) { MessageBox.Show(e.Message); }

                                Application.Current.Shutdown();
                            });
                        }
                    }

                    if (toCheck == "all" || toCheck == "ThirdPartyBinaries")
                    {
                        foreach (FileToDownload file in SoftwareParameters.ServerConfig.ThirdPartyBinaries.Files)
                        {
                            FileName = "Checking Files";
                            Thread.Sleep(20);

                            file.Path = Tools.FormatPath(file.Path);

                            if (!File.Exists(file.Path + file.FileName) || Tools.FileSHA256(file.Path + file.FileName) != file.Sha256)
                            {
                                Downloader(file.DlLink, file.Path, file.FileName, file.Sha256);
                            }
                        }
                    }
                    HostFilesAd_Visibility = Visibility.Collapsed;
                    trying = false;
                }
                catch { }
            }

            FileName = "Complete";
            Thread.Sleep(100);

            Dispatcher.BeginInvoke((Action)(() => { Close(); }));
        }

        private void CheckerPopup_StateChanged(object sender, EventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState;
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => { Focus(); }));
        }

        private void MainWindow_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowOrNot();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            ShowOrNot();
        }

        private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ShowOrNot();
        }

        private int downloaderTryesCount = 0;
        private int webClientTryesCount = 0;

        private bool useTor = false;

        public void Downloader(string url, string path, string fileName, string sha256)
        {
            ProgressBar_IsIndeterminate = true;
            HostFilesAd_Visibility = Visibility.Visible;

            downloaderTryesCount = 0;

            CheckInternet();

            FileName = fileName;
            StatusTitle = "Downloading Files";
            Thread.Sleep(20);

            useTor = false;

            while (!File.Exists(path + fileName) || Tools.FileSHA256(path + fileName) != sha256)
            {
                downloaderTryesCount++;

                ProgressDetails = "checking file";

                if (downloaderTryesCount > 2 || webClientTryesCount > 5) { if (!Tools.HaveADM) { Tools.RestartAsAdministrator(); } else { Tools.AddTrueMiningDestopToWinDefenderExclusions(true); } }
                if (downloaderTryesCount > 3 || webClientTryesCount > 7) { MessageBox.Show("An unexpected error has occurred. Check your internet and add the main folder of True Mining Desktop in the exceptions / exclusions of your antivirus, firewall and windows defender, then restart True Mining"); Application.Current.Dispatcher.Invoke((Action)delegate { Core.Miner.EmergencyExit = true; Application.Current.Shutdown(); Tools.CheckerPopup.Close(); }); }

                ProgressDetails = "Checking network connection";
                while (!Tools.IsConnected()) { ProgressDetails = "Waiting for internet connection..."; Thread.Sleep(2000); }

                ProgressDetails = "Starting download";

                WebClient webClient = new() { Proxy = useTor ? Tools.TorProxy : null, };

                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;

                try
                {
                    webClient.DownloadFileAsync(new Uri(url), path + fileName + ".dl");

                    while (webClient.IsBusy) { Thread.Sleep(100); }

                    ProgressDetails = "Moving file";

                    if (string.Compare(Tools.FileSHA256(path + fileName + ".dl"), sha256, StringComparison.OrdinalIgnoreCase) == 0)
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
                }
                catch { }

                Thread.Sleep(250);

                ProgressDetails = "checking file";

                Thread.Sleep(500);
            }
        }

        private void WebClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
            {
                downloaderTryesCount--;
                webClientTryesCount++;

                if (webClientTryesCount > 1)
                {
                    useTor = true;
                    Tools.UseTor = true;
                    Tools.NotifyPropertyChanged();
                }
            }
        }

        private void CheckInternet()
        {
            FileName = "trying to connect";
            StatusTitle = "Internet Connection";

            int internetErrorTryes = 0;
            while (!Tools.IsConnected()) { internetErrorTryes++; if (internetErrorTryes <= 3) { StatusTitle = "Internet Error"; FileName = "Waiting for Internet Connection. Check your network connection."; } else { StatusTitle = "Internet Error. Waiting for Internet Connection"; FileName = "Try open as ADM and add to Windows Firewall rules."; } Thread.Sleep(3000); }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressBar_Value = e.ProgressPercentage > 0 ? e.ProgressPercentage : 1;
            ProgressBar_IsIndeterminate = true;

            ProgressDetails = "Progress: " + (e.BytesReceived / 1024d / 1024d).ToString("0.00 MB") + " / " + (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00 MB") + " (" + e.ProgressPercentage + "%)";
        }

        private void ShowOrNot()
        {
            if (Application.Current.MainWindow.IsVisible)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    Height = 100;
                    BorderBrush.Opacity = 100;

                    ShowInTaskbar = true;

                    Activate();
                    Focus();
                });
            }
            else
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    Height = 0;
                    BorderBrush.Opacity = 0;

                    ShowInTaskbar = false;
                });
            }

            WindowState = Application.Current.MainWindow.WindowState;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Tape = false;

            Application.Current.MainWindow.IsVisibleChanged -= MainWindow_IsVisibleChanged;
            Application.Current.MainWindow.StateChanged -= MainWindow_StateChanged;
            Application.Current.MainWindow.Activated -= MainWindow_Activated;
            StateChanged -= CheckerPopup_StateChanged;
            Tools.PropertyChanged -= Tools_PropertyChanged;

            Application.Current.Dispatcher.Invoke(new Action(() => MainWindow.DispararEvento()));
        }

        private void ButtonCloseAction(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MessageBoxResult.Yes == MessageBox.Show("Closing this popup, True Mining Desktop will be closed. Are you sure?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly))
            {
                Core.Miner.EmergencyExit = true;
                Application.Current.Shutdown();

                Tools.CheckerPopup.Close();
            }
            else { return; }
        }

        private void HostFilesAd_Click(object sender, RoutedEventArgs e)
        {
            Tools.OpenLinkInBrowser("https://www.utivirtual.com.br/");
        }
    }
}