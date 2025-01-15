using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Minimal_Text_Editor__Lite_.ViewModel.Commands
{
    public class OpenBackupFolderCommand: ICommand
    {
        public SettingsModalWindowVM ViewModel { get; set; }
        public event EventHandler CanExecuteChanged;

        public OpenBackupFolderCommand(SettingsModalWindowVM vm)
        {
            ViewModel = vm;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            ViewModel.OpenBackupFolder();
        }
    }
}
