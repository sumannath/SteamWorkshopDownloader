using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;

namespace SteamWorkshopDownloader.Views.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INavigationWindow
    {
        private readonly BackgroundWorker steamCmdDownloadWorker = new();

        public ViewModels.MainWindowViewModel ViewModel
        {
            get;
        }

        public MainWindow(ViewModels.MainWindowViewModel viewModel, IPageService pageService, INavigationService navigationService)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
            SetPageService(pageService);

            steamCmdDownloadWorker.DoWork += steamCmdDownloadWorker_DoWork;
            steamCmdDownloadWorker.RunWorkerCompleted += steamCmdDownloadWorker_RunWorkerCompleted;

            navigationService.SetNavigationControl(RootNavigation);

            Wpf.Ui.Appearance.Theme.Apply(Wpf.Ui.Appearance.ThemeType.Light);
        }

        #region INavigationWindow methods

        public Frame GetFrame()
            => RootFrame;

        public INavigation GetNavigation()
            => RootNavigation;

        public bool Navigate(Type pageType)
            => RootNavigation.Navigate(pageType);

        public void SetPageService(IPageService pageService)
            => RootNavigation.PageService = pageService;

        public void ShowWindow()
            => Show();

        public void CloseWindow()
            => Close();

        #endregion INavigationWindow methods

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Make sure that closing this window will begin the process of closing the application.
            Application.Current.Shutdown();
        }

        private void UiWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (SteamWorkshopDownloader.Properties.Settings.Default.SteamCMDAutoDownload)
                if (!File.Exists(SteamWorkshopDownloader.Properties.Settings.Default.SteamCMDPath))
                    steamCmdDownloadWorker.RunWorkerAsync();
        }

        private void steamCmdDownloadWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            DirectoryInfo cmdDirectory;
            string zipFileName = "steam_cmd_archive.zip";
            string zipFilePath;
            using (var client = new HttpClient())
            {
                using (var s = client.GetStreamAsync(SteamWorkshopDownloader.Properties.Settings.Default.SteamCMDUrl))
                {
                    cmdDirectory = Directory.CreateDirectory(SteamWorkshopDownloader.Properties.Settings.Default.SteamCMDPath);
                    zipFilePath = Path.Combine(cmdDirectory.FullName, zipFileName);
                    using (var fs = new FileStream(zipFilePath, FileMode.OpenOrCreate))
                    {
                        s.Result.CopyTo(fs);
                    }
                }
            }
            System.IO.Compression.ZipFile.ExtractToDirectory(zipFilePath, cmdDirectory.FullName, true);
            File.Delete(zipFilePath);

            SteamWorkshopDownloader.Properties.Settings.Default.SteamCMDPath = Path.Combine(cmdDirectory.FullName, "steamcmd.exe");
        }

        private void steamCmdDownloadWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            //update ui once worker complete his work
        }

        private void UiWindow_Closed(object sender, EventArgs e)
        {
            SteamWorkshopDownloader.Properties.Settings.Default.Save();
        }
    }
}