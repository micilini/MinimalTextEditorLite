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

namespace Minimal_Text_Editor__Lite_.View.Components
{
    /// <summary>
    /// Interaction logic for HeaderControl.xaml
    /// </summary>
    public partial class HeaderControl : UserControl
    {
        public MainScreenWindowVM MainScreenWindow { get; set; }

        public HeaderControl(MainScreenWindowVM MS)
        {
            InitializeComponent();
            MainScreenWindow = MS;

            this.DataContext = new HeaderControlVM(MS);
        }
    }
}
