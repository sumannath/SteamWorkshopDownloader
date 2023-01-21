using System;
using System.Windows;
using Wpf.Ui.Common.Interfaces;

namespace SteamWorkshopDownloader.Views.Pages
{
    /// <summary>
    /// Interaction logic for AuthPage.xaml
    /// </summary>
    public partial class AuthPage : INavigableView<ViewModels.AuthViewModel>
    {
        public ViewModels.AuthViewModel ViewModel
        {
            get;
        }

        public AuthPage(ViewModels.AuthViewModel viewModel)
        {
            ViewModel = viewModel;

            InitializeComponent();
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SteamWorkshopDownloader.Properties.Settings.Default.Save();
            MessageBox.Show("Saved!");
        }
    }
}
