using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public CheckerPopup(ToCheck toCheck = ToCheck.All)
        {
            InitializeComponent();
            DataContext = this;
            Height = 0;
            BorderBrush.Opacity = 0;
            ShowInTaskbar = false;

            Tools.PropertyChanged += Tools_PropertyChanged;

            Tools.NotifyPropertyChanged();

            TaskChecker = new Task(() => Checker(new Uri("https://truemining.online/config.json"), toCheck));
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

        public bool Finish
        { get { return property_finish; } set { property_finish = value; ProgressDetails = null; } }

        public string StatusTitle
        { get { return property_statusTitle; } set { property_statusTitle = value; Dispatcher.BeginInvoke((Action)(() => { statusTitle.Content = value; })); } }

        public string FileName
        { get { return property_fileName; } set { property_fileName = value; Dispatcher.BeginInvoke((Action)(() => { fileName.Content = new TextBlock() { Text = value, TextWrapping = TextWrapping.WrapWithOverflow }; })); } }

        public int ProgressBar_Value
        { get { return property_progressBar_Value; } set { if (property_progressBar_Value != value) { property_progressBar_Value = value; Dispatcher.BeginInvoke((Action)(() => { progressBar.Value = value; })); } } }

        public bool ProgressBar_IsIndeterminate
        { get { return property_progressBar_IsIndeterminate; } set { if (property_progressBar_IsIndeterminate != value) { property_progressBar_IsIndeterminate = value; Dispatcher.BeginInvoke((Action)(() => { progressBar.IsIndeterminate = value; })); } } }

        public string ProgressDetails
        { get { return progressDetails_Content; } set { progressDetails_Content = value; Dispatcher.BeginInvoke((Action)(() => { progressDetails.Content = value; })); } }

        public Visibility HostFilesAd_Visibility
        { get { return hostFilesAd_Visibility; } set { hostFilesAd_Visibility = value; Dispatcher.BeginInvoke((Action)(() => { HostFilesAd.Visibility = value; })); } }

        private bool needRestartIfChanges = false;
        private bool changes = false;

        public bool allDone = false;

        public enum ToCheck
        {
            All,
            AppFiles,
            Tools,
            AllBackendMiners,
            SelectedBackendMiners,
            BackendCpu,
            BackendOpencl,
            BackendCuda
        }

        public void Checker(Uri uri, ToCheck toCheck)
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

            FileName = "trying to connect";
            StatusTitle = "Internet Connection";
            CheckInternet();

            StatusTitle = "Checking Instalation";

            bool trying = true;

            while (trying)
            {
                try
                {
                    FileName = "Updating software parameters";
                    SoftwareParameters.Update(uri);

                    if ((File.Exists("DoNotUpdateNothing") && DateTime.UtcNow < new DateTime(2022, 03, 02)) || (Core.NextStart.Actions.loadedNextStartInstructions.useThisInstructions && Core.NextStart.Actions.loadedNextStartInstructions.ignoreUpdates))
                    {
                        FileName = "Force do not update";
                        Thread.Sleep(1000);
                        Dispatcher.BeginInvoke((Action)(() => { Close(); }));
                    }
                    else
                    {
                        FileName = "Checking Files";

                        // cria lista de arquivos esperados que serão verificados
                        List<Server.FileInfo> filesToCheck = new List<Server.FileInfo>();

                        // adiciona os arquivos esperados na lista, com base em qual categoria de arquivos foi solicitada
                        if (toCheck == ToCheck.All && !(File.Exists("DoNotUpdateAll") && DateTime.UtcNow < new DateTime(2022, 03, 02)))
                        {
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.AppFiles);
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.Tools);
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Common);
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Cpu);
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Opencl);
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Cuda);

                            needRestartIfChanges = true;
                        }
                        else if (toCheck == ToCheck.AppFiles && !(File.Exists("DoNotUpdateAppFiles") && DateTime.UtcNow < new DateTime(2022, 03, 02)))
                        {
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.AppFiles);

                            needRestartIfChanges = true;
                        }
                        else if (toCheck == ToCheck.Tools)
                        {
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.Tools);
                        }
                        else if (toCheck == ToCheck.SelectedBackendMiners)
                        {
                            if (Device.DevicesList.Any(device => device.IsSelected)) { filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Common); }

                            if (Device.Cpu.IsSelected) { filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Cpu); }
                            if (Device.Opencl.IsSelected) { filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Opencl); }
                            if (Device.Cuda.IsSelected) { filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Cuda); }
                        }
                        else if (toCheck == ToCheck.AllBackendMiners)
                        {
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Common);

                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Cpu);
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Opencl);
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Cuda);
                        }
                        else if (toCheck == ToCheck.BackendCpu)
                        {
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Common);
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Cpu);
                        }
                        else if (toCheck == ToCheck.BackendOpencl)
                        {
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Common);
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Opencl);
                        }
                        else if (toCheck == ToCheck.BackendCuda)
                        {
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Common);
                            filesToCheck.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Cuda);
                        }

                        // cria lsita de arquivos que precisam ser baixados
                        List<Server.FileInfo> filesToDowload = new List<Server.FileInfo>();

                        // adiciona na lista de downloads os arquivos que não existirem ou tiverem o chcksum sha256 diferente
                        filesToCheck.ForEach(file =>
                        {
                            file.Directory = Tools.FormatPath(file.Directory);

                            if (!File.Exists(file.Directory + file.FileName) || Tools.FileSHA256(file.Directory + file.FileName) != file.Sha256)
                            {
                                filesToDowload.Add(file);
                            }
                        });

                        // faz downlaod de cada arquivo da lista de downloads
                        filesToDowload.ForEach(file =>
                        {
                            while (!Downloader(file, "(" + filesToDowload.IndexOf(file) + "/" + filesToDowload.Count + ")")) { Thread.Sleep(300); };
                        });

                        // aplica cada arquivo, movendo o arquivo existente (ou não) para uma localização de descarte e movendo o arquivo baixado para a localização do arquivo antigo
                        filesToDowload.ForEach(file =>
                        {
                            while (!ApplyDownloadedFile(file, "(" + filesToDowload.IndexOf(file) + "/" + filesToDowload.Count + ")")) { Thread.Sleep(300); };
                            changes = true;
                        });
                    }

                    // remove os arquivos marcados como RemovedFiles
                    SoftwareParameters.ServerConfig.RemovedFiles.ForEach(removeFile =>
                    {
                        try
                        {
                            removeFile.Directory = Tools.FormatPath(removeFile.Directory);

                            if (File.Exists(removeFile.Directory + removeFile.FileName))
                            {
                                FileName = "Marking old files";

                                File.Move(removeFile.Directory + removeFile.FileName, removeFile.Directory + removeFile.FileName + ".old", true);
                            }
                        }
                        catch { }
                    });

                    if (needRestartIfChanges && changes)
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
                            System.Diagnostics.Process TrueMiningAsAdmin = new();
                            TrueMiningAsAdmin.StartInfo = new System.Diagnostics.ProcessStartInfo()
                            {
                                FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,
                                UseShellExecute = false,
                            };
                            if (Tools.HaveADM) { TrueMiningAsAdmin.StartInfo.Verb = "runas"; }

                            try { TrueMiningAsAdmin.Start(); Miner.StopMiners(); } catch (Exception e) { MessageBox.Show(e.Message); }

                            Application.Current.Shutdown();
                        });
                    }

                    HostFilesAd_Visibility = Visibility.Collapsed;
                    trying = false;
                }
                catch { }
            }

            Task removeOldFiles = new(() =>
            {
                try
                {
                    FileName = "Removing old files";
                    string[] filesDotOld = Directory.GetFiles(Environment.CurrentDirectory, "*.old", SearchOption.AllDirectories);
                    foreach (var file in filesDotOld)
                    {
                        try
                        {
                            if (!Tools.IsFileLocked(new System.IO.FileInfo(file))) { File.Delete(file); }
                        }
                        catch { };
                    }

                    string[] FilesDotDl = Directory.GetFiles(Environment.CurrentDirectory, "*.dl", SearchOption.AllDirectories);

                    List<Server.FileInfo> AllSoftwareFilesOrigin = new List<Server.FileInfo>();

                    if (FilesDotDl.Length > 0)
                    {
                        AllSoftwareFilesOrigin.AddRange(SoftwareParameters.ServerConfig.AppFiles);
                        AllSoftwareFilesOrigin.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.Tools);
                        AllSoftwareFilesOrigin.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Common);
                        AllSoftwareFilesOrigin.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Cpu);
                        AllSoftwareFilesOrigin.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Opencl);
                        AllSoftwareFilesOrigin.AddRange(SoftwareParameters.ServerConfig.ExtraFiles.BackendMiners.Cuda);
                    }

                    foreach (string file in FilesDotDl)
                    {
                        string arqSha256 = Tools.FileSHA256(file);

                        if (!AllSoftwareFilesOrigin.Exists(x => string.Equals(arqSha256, x.Sha256, StringComparison.OrdinalIgnoreCase)))
                        {
                            try
                            {
                                if (!Tools.IsFileLocked(new System.IO.FileInfo(file))) { File.Delete(file); }
                            }
                            catch { };
                        }
                    }
                }
                catch { };
            });
            removeOldFiles.Start();
            removeOldFiles.Wait(4000);

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

        public bool Downloader(Server.FileInfo file, string progress = null)
        {
            try
            {
                ProgressBar_IsIndeterminate = true;
                ProgressBar_Value = 0;
                HostFilesAd_Visibility = Visibility.Visible;

                if ((File.Exists(file.Directory + file.FileName + ".dl") && Tools.FileSHA256(file.Directory + file.FileName + ".dl") == file.Sha256) || (File.Exists(file.Directory + file.FileName) && Tools.FileSHA256(file.Directory + file.FileName) == file.Sha256)) { return true; }

                downloaderTryesCount = 0;
                webClientTryesCount = 0;

                CheckInternet();

                FileName = file.FileName;
                StatusTitle = "Downloading Files " + progress;

                useTor = false;

                while (!File.Exists(file.Directory + file.FileName + ".dl") || Tools.FileSHA256(file.Directory + file.FileName + ".dl") != file.Sha256)
                {
                    downloaderTryesCount++;

                    if (downloaderTryesCount > 2 || webClientTryesCount > 5) { if (!Tools.HaveADM) { Tools.RestartApp(); } else { Tools.AddTrueMiningDestopToWinDefenderExclusions(true); } }
                    if (downloaderTryesCount > 3 || webClientTryesCount > 7) { MessageBox.Show("An unexpected error has occurred. Check your internet and add the main folder of True Mining Desktop in the exceptions / exclusions of your antivirus, firewall and windows defender, then restart True Mining"); Application.Current.Dispatcher.Invoke((Action)delegate { Core.Miner.EmergencyExit = true; Application.Current.Shutdown(); Tools.CheckerPopup.Close(); }); }

                    while (!Tools.IsConnected()) { ProgressDetails = "Waiting for internet connection..."; Thread.Sleep(2000); }

                    ProgressDetails = "Progress: starting download";

                    WebClient webClient = new() { Proxy = useTor ? Tools.TorProxy : null };
                    webClient.DownloadProgressChanged -= WebClient_DownloadProgressChanged;
                    webClient.DownloadFileCompleted -= WebClient_DownloadFileCompleted;
                    webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                    webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
                    webClient.DownloadFileAsync(new Uri(file.DlLink), file.Directory + file.FileName + ".dl");

                    long currentDownloadOLastTotalBytesReceived = currentDownloadBytesReceived;
                    DateTime currentDownloadLastProgressUpdated = DateTime.UtcNow.AddSeconds(useTor ? 25 : 5);

                    while (webClient.IsBusy)
                    {
                        if (Equals(currentDownloadBytesReceived, currentDownloadOLastTotalBytesReceived))
                        {
                            if (DateTime.UtcNow > currentDownloadLastProgressUpdated.AddSeconds(10))
                            {
                                ProgressDetails = "Progress: restarting...";
                                FileName += " => fail. restarting app...";
                                StatusTitle = "Fail > Restarting Application";
                                Thread.Sleep(30000);

                                webClient.CancelAsync();

                                Dispatcher.BeginInvoke((Action)(() => { Tools.RestartApp(false); Close(); }));
                            }
                        }
                        else
                        {
                            currentDownloadLastProgressUpdated = DateTime.UtcNow;
                            currentDownloadOLastTotalBytesReceived = currentDownloadBytesReceived;
                        }
                        Thread.Sleep(150);
                    }
                    //    webClient.Dispose();
                }
                if ((File.Exists(file.Directory + file.FileName + ".dl") && Tools.FileSHA256(file.Directory + file.FileName + ".dl") == file.Sha256) || (File.Exists(file.Directory + file.FileName) && Tools.FileSHA256(file.Directory + file.FileName) == file.Sha256)) { return true; } else { return false; }
            }
            catch { }
            return false;
        }

        public bool ApplyDownloadedFile(Server.FileInfo file, string progress = null)
        {
            FileName = file.FileName;
            StatusTitle = "Moving files " + progress;
            ProgressDetails = "";

            int TryCount = 0;

            while (!File.Exists(file.Directory + file.FileName) || String.Compare(Tools.FileSHA256(file.Directory + file.FileName), file.Sha256, StringComparison.OrdinalIgnoreCase) != 0)
            {
                TryCount++;

                if (TryCount > 3) { if (!Tools.HaveADM) { Tools.RestartApp(); } else { Tools.AddTrueMiningDestopToWinDefenderExclusions(true); } }
                if (TryCount > 4) { MessageBox.Show("An unexpected error has occurred. Check your internet and add the main folder of True Mining Desktop in the exceptions / exclusions of your antivirus, firewall and windows defender, then restart True Mining"); Application.Current.Dispatcher.Invoke((Action)delegate { Core.Miner.EmergencyExit = true; Application.Current.Shutdown(); Tools.CheckerPopup.Close(); }); }

                try
                {
                    if (String.Compare(Tools.FileSHA256(file.Directory + file.FileName + ".dl"), file.Sha256, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (Tools.IsFileLocked(new System.IO.FileInfo(file.Directory + file.FileName)))
                        {
                            File.Move(file.Directory + file.FileName, file.Directory + file.FileName + ".old", true);
                        }
                        try
                        {
                            File.Move(file.Directory + file.FileName + ".dl", file.Directory + file.FileName, true);
                        }
                        catch { File.Move(file.Directory + file.FileName, file.Directory + file.FileName + ".old", true); File.Move(file.Directory + file.FileName + ".dl", file.Directory + file.FileName, true); }
                    }

                    if (File.Exists(file.Directory + file.FileName) && String.Compare(Tools.FileSHA256(file.Directory + file.FileName), file.Sha256, StringComparison.OrdinalIgnoreCase) == 0) { return true; }
                }
                catch { }
            }

            if (File.Exists(file.Directory + file.FileName) && String.Compare(Tools.FileSHA256(file.Directory + file.FileName), file.Sha256, StringComparison.OrdinalIgnoreCase) == 0) { return true; }

            return false;
        }

        private void WebClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
            {
                ProgressDetails = "fail / error";
                downloaderTryesCount--;
                webClientTryesCount++;
                currentDownloadBytesReceived = 0;

                if (webClientTryesCount > 2)
                {
                    useTor = true;
                    Tools.UseTor = true;
                    Tools.NotifyPropertyChanged();
                }
            }
            else { webClientTryesCount = 0; }
        }

        private void CheckInternet()
        {
            int internetErrorTryes = 0;
            while (!Tools.IsConnected()) { internetErrorTryes++; if (internetErrorTryes <= 3) { StatusTitle = "Internet Error"; FileName = "Waiting for Internet Connection. Check your network connection."; } else { StatusTitle = "Internet Error. Waiting for Internet Connection"; FileName = "Try open as ADM and add to Windows Firewall rules."; } Thread.Sleep(3000); }
        }

        private long currentDownloadBytesReceived = 0;

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            currentDownloadBytesReceived = e.BytesReceived;

            if (e.TotalBytesToReceive / 1024d > 50) // menos de 100kb não altera o progress para exibir o progresso
            {
                ProgressBar_Value = e.ProgressPercentage > 0 ? e.ProgressPercentage : 1;

                ProgressBar_IsIndeterminate = true;

                ProgressDetails = "Progress: " + (e.BytesReceived / 1024d / 1024d).ToString("0.00 MB") + " / " + (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00 MB") + " (" + e.ProgressPercentage + "%)";
            }
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