using Minimal_Text_Editor__Lite_.ViewModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Minimal_Text_Editor__Lite_.View
{
    /// <summary>
    /// Interaction logic for AppMenuControl.xaml
    /// </summary>
    public partial class AppMenuControl : UserControl
    {
        MainScreenWindowVM mainScreenWindowVM { get; set; }

        public AppMenuControl(MainScreenWindowVM mainScreen)
        {
            InitializeComponent();

            this.mainScreenWindowVM = mainScreen;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem mi) || mi.Tag is null) return;

            this.mainScreenWindowVM.MenuClick(mi.Tag.ToString());
        }

    }
}
