﻿using Minimal_Text_Editor__Lite_.Model;
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

namespace Minimal_Text_Editor__Lite_.View.GlobalModals
{
    /// <summary>
    /// Interaction logic for GlobalModalsWindow.xaml
    /// </summary>
    public partial class GlobalModalsWindow : Window
    {
        GlobalModalsWindowVM GlobalModalsWindowVM { get; set; }

        public GlobalModalsWindow(GlobalModalModel gmm, bool inputModal)
        {
            InitializeComponent();

            GlobalModalsWindowVM = new GlobalModalsWindowVM(this, gmm, inputModal);
            this.DataContext = GlobalModalsWindowVM;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).ShowOpenNoteMessage = false;
            GlobalModalsWindowVM.UpdateShowOpenNoteMessage(false);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).ShowOpenNoteMessage = true;
            GlobalModalsWindowVM.UpdateShowOpenNoteMessage(true);
        }

    }
}
