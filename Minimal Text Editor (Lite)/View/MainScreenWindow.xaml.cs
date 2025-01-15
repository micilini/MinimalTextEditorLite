using MaterialDesignThemes.Wpf;
using Minimal_Text_Editor__Lite_.View.Components;
using Minimal_Text_Editor__Lite_.View.SettingsModal;
using Minimal_Text_Editor__Lite_.ViewModel;
using Minimal_Text_Editor__Lite_.ViewModel.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
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
    /// Interaction logic for MainScreenWindow.xaml
    /// </summary>
    public partial class MainScreenWindow : Window
    {
        public MainScreenWindow()
        {
            InitializeComponent();
            this.DataContext = new MainScreenWindowVM(this);
        }
    }
}
