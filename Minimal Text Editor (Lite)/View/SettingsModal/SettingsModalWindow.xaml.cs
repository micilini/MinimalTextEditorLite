using Minimal_Text_Editor__Lite_.Model;
using Minimal_Text_Editor__Lite_.ViewModel;
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

namespace Minimal_Text_Editor__Lite_.View.SettingsModal
{
    /// <summary>
    /// Interaction logic for SettingsModalWindow.xaml
    /// </summary>
    public partial class SettingsModalWindow : Window
    {
        SettingsModalWindowVM SettingsModalWindowVM { get; set; }

        public SettingsModalWindow()
        {
            InitializeComponent();

            SettingsModalWindowVM = new SettingsModalWindowVM(this);
            this.DataContext = SettingsModalWindowVM;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SettingsModalWindowVM.AllowUserInteract = true;
        }
    }
}
