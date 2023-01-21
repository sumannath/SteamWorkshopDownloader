using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using SteamWorkshopDownloader.ViewModels;
using SteamWorkshopDownloader.Views.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows;
using Wpf.Ui.Common.Interfaces;
using Wpf.Ui.Controls;

namespace SteamWorkshopDownloader.Views.Pages
{
    /// <summary>
    /// Interaction logic for DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : INavigableView<ViewModels.DashboardViewModel>
    {
        private readonly BackgroundWorker steamCommDownloadWorker = new();
        ProgressWindowViewModel vm = new();

        public ViewModels.DashboardViewModel ViewModel
        {
            get;
        }

        public DashboardPage(ViewModels.DashboardViewModel viewModel)
        {
            ViewModel = viewModel;

            InitializeComponent();

            steamCommDownloadWorker.DoWork += steamCommDownloadWorker_DoWork;
            steamCommDownloadWorker.RunWorkerCompleted += steamCommDownloadWorker_RunWorkerCompleted;

        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is not Wpf.Ui.Controls.Button button)
                return;

            var tag = button.Tag as string;

            if (String.IsNullOrWhiteSpace(tag))
                return;

            switch (tag)
            {
                case "btnSave":
                    SaveListAsFile();
                    break;
                case "btnLoad":
                    LoadListFromFile();
                    break;
                case "btnRun":
                    BeginProcessing();
                    break;
            }
        }

        private void BeginProcessing()
        {
            if (txtURLs.Text.Length == 0)
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    ButtonLeftName = "Ok"
                };

                messageBox.ButtonLeftClick += MessageBox_LeftButtonClick;
                messageBox.ButtonRightClick += MessageBox_RightButtonClick;

                messageBox.Title = "Steam Workshop Downloader";
                messageBox.Content = "Please enter URLs";
                messageBox.ShowDialog();
            }
            else
            {
                steamCommDownloadWorker.RunWorkerAsync();

                ProgressWindow progressWindow = new(vm)
                {
                    Owner = Application.Current.MainWindow
                };
                progressWindow.ShowDialog();
            }
        }

        private void MessageBox_LeftButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            (sender as Wpf.Ui.Controls.MessageBox)?.Close();
        }

        private void MessageBox_RightButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            (sender as Wpf.Ui.Controls.MessageBox)?.Close();
        }

        private void LoadListFromFile()
        {
            OpenFileDialog opDialog = new()
            {
                Filter = "Steam Downloader list files (*.sdlst)|*.sdlst|All files (*.*)|*.*"
            };

            if (opDialog.ShowDialog().GetValueOrDefault())
            {
                txtURLs.Text = File.ReadAllText(opDialog.FileName);
                txtURLs.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            }
        }

        private void SaveListAsFile()
        {
            SaveFileDialog svDialog = new()
            {
                Filter = "Steam Downloader list files (*.sdlst)|*.sdlst|All files (*.*)|*.*"
            };

            if (svDialog.ShowDialog().GetValueOrDefault())
            {
                File.WriteAllText(svDialog.FileName, txtURLs.Text);
            }
        }

        private void steamCommDownloadWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            vm.DisplayText = "Downloading...";
            string[] artifacts = ViewModel.SteamURLs.Split("\n");
            if (artifacts.Length > 0)
            {
                ProcessDownloads(ViewModel.SameGame, artifacts);
            }
        }

        private void ProcessDownloads(bool sameGame, string[] artifacts)
        {
            string steamCMDCommand = CreateCommand(sameGame, artifacts);

            var p = new Process
            {
                StartInfo =
                        {
                             FileName = "cmd.exe",
                             UseShellExecute = false,
                             CreateNoWindow = true,
                             RedirectStandardOutput = true,
                             WorkingDirectory = Path.GetDirectoryName(Properties.Settings.Default.SteamCMDPath),
                             Arguments = string.Format("/c steamcmd {0}", steamCMDCommand)
                        }
            };
            p.OutputDataReceived += new DataReceivedEventHandler(OutputDataHandler);
            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();
        }

        private void OutputDataHandler(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine(e.Data);
            vm.DisplayText = e.Data;
        }

        private string CreateCommand(bool sameGame, string[] artifacts)
        {
            int appID = -1;
            string loginStr = "\"+login {0} {1}\" ";

            StringBuilder stringBuilder = new();

            if (Properties.Settings.Default.UseSteamCredentials)
            {
                loginStr = string.Format(
                    loginStr,
                    Properties.Settings.Default.SteamUsername,
                    Properties.Settings.Default.SteamPassword
                    );
            }
            else
            {
                loginStr = string.Format(
                    loginStr,
                    "anonymous",
                    ""
                    );
            }

            stringBuilder.Append(loginStr);

            foreach (string artifact in artifacts)
            {
                Uri myUri = new Uri(artifact);
                string? contentId = HttpUtility.ParseQueryString(myUri.Query).Get(name: "id");

                if (contentId == null)
                {
                    vm.DisplayText = "URL does not have contentId. Please use valid URL.";
                }
                else
                {
                    if (appID == -1 || !sameGame)
                    {
                        var pairs = new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string, string>("itemcount", "1"),
                            new KeyValuePair<string, string>("publishedfileids[0]", contentId)
                        };

                        var content = new FormUrlEncodedContent(pairs);
                        var client = new HttpClient { BaseAddress = new Uri("https://api.steampowered.com") };

                        var response = client.PostAsync("/ISteamRemoteStorage/GetPublishedFileDetails/v1/", content).Result;

                        if (response.IsSuccessStatusCode)
                        {
                            string responseJson = response.Content.ReadAsStringAsync().Result;

                            JObject joResponse = JObject.Parse(responseJson);
                            JObject ojObject = (JObject)joResponse["response"];
                            JArray array = (JArray)ojObject["publishedfiledetails"];
                            appID = Convert.ToInt32(array[0]["consumer_app_id"].ToString());
                        }
                        else
                        {
                            vm.DisplayText = "Cannot get appId. Please check network connection.";
                        }
                    }
                    if(appID == -1)
                    {
                        vm.DisplayText = "Cannot get appId. Please check network connection.";
                    } 
                    else
                    {
                        Uri myUri1 = new Uri(artifact);
                        string? contentId1 = HttpUtility.ParseQueryString(myUri1.Query).Get(name: "id");

                        stringBuilder.Append(string.Format("\"+workshop_download_item {0} {1}\" ", appID, contentId1));
                    }
                }
            }

            stringBuilder.Append("+quit");
            return stringBuilder.ToString();
        }

        private void steamCommDownloadWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            vm.DisplayText = "Download Complete!";
        }
    }
}