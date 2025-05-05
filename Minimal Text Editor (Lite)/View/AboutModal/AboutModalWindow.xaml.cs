using Minimal_Text_Editor__Lite_.ViewModel.Helpers;
using System;
using System.Collections.Generic;
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

namespace Minimal_Text_Editor__Lite_.View.AboutModal
{
    /// <summary>
    /// Interaction logic for AboutModalWindow.xaml
    /// </summary>
    public partial class AboutModalWindow : Window
    {
        private bool _isCheckingUpdates = false;
        private readonly UpdatesCheck _updatesChecker = new UpdatesCheck();

        public AboutModalWindow()
        {
            InitializeComponent();

            AppId.Text = ((App)Application.Current).ApplicationIdentifier;
            AppV.Text = ((App)Application.Current).ApplicationVersion;
        }

        public async void CheckForUpdates()
        {
            // Se já está checando, ignora chamadas adicionais
            if (_isCheckingUpdates)
                return;

            _isCheckingUpdates = true;
            try
            {
                // Aqui esperamos o término do processo de verificação
                await _updatesChecker.CheckForUpdates(false);
            }
            finally
            {
                // Permite novo clique somente depois de completo
                _isCheckingUpdates = false;
            }
        }

        private void OnCloseIconClicked(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}
