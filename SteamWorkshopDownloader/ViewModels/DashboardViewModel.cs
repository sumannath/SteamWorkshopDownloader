using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamWorkshopDownloader.Views.Windows;
using System;
using System.Collections.ObjectModel;
using Wpf.Ui.Common;
using Wpf.Ui.Common.Interfaces;
using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Controls;
using Wpf.Ui.Mvvm.Contracts;

namespace SteamWorkshopDownloader.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _steamURLs = String.Empty;

        [ObservableProperty]
        private bool _sameGame = false;

        public DashboardViewModel(INavigationService navigationService)
        {
            if (!_isInitialized)
                InitializeViewModel();
        }
        private void InitializeViewModel()
        {
            _isInitialized = true;
        }
    }
}
