using Minimal_Text_Editor__Lite_.ViewModel;
using Minimal_Text_Editor__Lite_.ViewModel.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Minimal_Text_Editor__Lite_.View
{
    /// <summary>
    /// Interaction logic for SplashScreenWindow.xaml
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        private readonly SplashScreenWindowVM viewModel;

        public SplashScreenWindow()
        {
            InitializeComponent();
            viewModel = new SplashScreenWindowVM();
            this.DataContext = viewModel;

            viewModel.OnLoadingComplete += OnLoadingComplete;
        }

        private async void Window_ContentRendered(object sender, EventArgs e)
        {
            await viewModel.InitializeAsync();
        }

        private void OnLoadingComplete()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MainScreenWindow mainScreen = new MainScreenWindow();
                mainScreen.Show();
                Close();
            });
        }

    }
}
