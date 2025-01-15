using Microsoft.Web.WebView2.Core;
using Minimal_Text_Editor__Lite_.Model;
using Minimal_Text_Editor__Lite_.ViewModel;
using Minimal_Text_Editor__Lite_.ViewModel.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Minimal_Text_Editor__Lite_.View
{
    /// <summary>
    /// Interaction logic for EditorControl.xaml
    /// </summary>
    public partial class EditorControl : UserControl
    {
        EditorControlVM EditorControlVM { get; set; }

        public EditorControl(MainScreenWindowVM mainScreen)
        {
            InitializeComponent();

            EditorControlVM = new EditorControlVM(mainScreen, myWebView);
            this.DataContext = EditorControlVM;
        }

        //Método de carregamento de Nota Atual
        public void LoadCurrentNote()
        {
            EditorControlVM.LoadCurrentNote();
        }

        //Método de recebimento de ação do HeaderControl
        public void DoAction(string action)
        {
            EditorControlVM.DoAction(action);
        }

    }
}
