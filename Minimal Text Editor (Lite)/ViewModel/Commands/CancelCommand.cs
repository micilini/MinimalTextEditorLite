using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Minimal_Text_Editor__Lite_.ViewModel.Commands
{
    public class CancelCommand<T> : ICommand where T : class
    {
        public T ViewModel { get; set; }
        public event EventHandler CanExecuteChanged;

        public CancelCommand(T vm)
        {
            ViewModel = vm;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var cancelMethod = ViewModel?.GetType().GetMethod("CancelButton");
            if (cancelMethod != null)
            {
                cancelMethod.Invoke(ViewModel, null);
            }
        }
    }
}
