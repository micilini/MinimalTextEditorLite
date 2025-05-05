using Minimal_Text_Editor__Lite_.View;
using Minimal_Text_Editor__Lite_.View.ExportModal;
using Minimal_Text_Editor__Lite_.View.SettingsModal;
using Minimal_Text_Editor__Lite_.ViewModel.Commands;
using Minimal_Text_Editor__Lite_.ViewModel.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minimal_Text_Editor__Lite_.ViewModel
{
    public class HeaderControlVM : INotifyPropertyChanged
    {
        //Configurações Essências da Classe de ViewModel
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //Propriedades da Classe
        MainScreenWindowVM mainScreenWindow { get; set; }

        //Configurações dos Comandos (ICommands)
        public OpenNoteCommand OpenNoteCommand { get; set; }
        public SaveNoteCommand SaveNoteCommand { get; set; }
        public RemoveNoteCommand RemoveNoteCommand { get; set; }
        public SettingsCommand SettingsCommand { get; set; }
        public ExportCommand ExportCommand { get; set; }
        public SearchNoteCommand SearchNoteCommand { get; set; }

        //Configurações do Construtor da Classe
        public HeaderControlVM(MainScreenWindowVM mainScreen)
        {
            mainScreenWindow = mainScreen;

            OpenNoteCommand = new OpenNoteCommand(this);
            SaveNoteCommand = new SaveNoteCommand(this);
            RemoveNoteCommand = new RemoveNoteCommand(this);
            SettingsCommand = new SettingsCommand(this);
            ExportCommand = new ExportCommand(this);
            SearchNoteCommand = new SearchNoteCommand(this);
        }

        //Interações com Comandos

        public void OpenNote()
        {
            mainScreenWindow.OpenNewNote();
        }

        public void SaveNote()
        {
            mainScreenWindow.SaveNote();
        }

        public void RemoveNote()
        {
            mainScreenWindow.RemoveNote();
        }

        public void OpenSettings()
        {
            mainScreenWindow.OpenSettingsDialog();
        }

        public void ExportNote()
        {
            mainScreenWindow.ExportNoteDialog();
        }

        public void SearchNote()
        {
            mainScreenWindow.SearchNote();
        }

    }
}
