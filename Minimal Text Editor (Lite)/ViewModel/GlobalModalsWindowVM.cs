using Minimal_Text_Editor__Lite_.Model;
using Minimal_Text_Editor__Lite_.View.GlobalModals;
using Minimal_Text_Editor__Lite_.ViewModel.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using Minimal_Text_Editor__Lite_.ViewModel.Helpers;

namespace Minimal_Text_Editor__Lite_.ViewModel
{
    public class GlobalModalsWindowVM : INotifyPropertyChanged
    {
        //Métodos do INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //Propriedades da Classe
        private GlobalModalsWindow globalModalsWindow;
        public GlobalModalsWindow GlobalModalsWindow
        {
            get => globalModalsWindow;
            set
            {
                if (globalModalsWindow != value)
                {
                    globalModalsWindow = value;
                    OnPropertyChanged("GlobalMoldasWindow");
                }
            }
        }

        private GlobalModalModel globalModalModel;
        public GlobalModalModel GlobalModalModel
        {
            get => globalModalModel;
            set
            {
                if (globalModalModel != value)
                {
                    globalModalModel = value;
                    OnPropertyChanged("GlobalMoldasWindow");
                }
            }
        }

        public string InputResult { get; set; }
        public bool IsInputModal { get; set; }

        //Comandos
        public CancelCommand<GlobalModalsWindowVM> CancelCommand { get; set; }
        public NextCommand NextCommand { get; set; }
        public OkCommand OkCommand { get; set; }

        //Método Construtor da Classe
        public GlobalModalsWindowVM(GlobalModalsWindow globalModals, GlobalModalModel gmm, bool inputModal)
        {
            GlobalModalsWindow = globalModals;
            GlobalModalModel = gmm;

            IsInputModal = inputModal;
            InputResult = string.Empty;

            CancelCommand = new CancelCommand<GlobalModalsWindowVM>(this);
            NextCommand = new NextCommand(this);
            OkCommand = new OkCommand(this);
        }

        //Métodos da Classe
        public void NextButton()
        {
            GlobalModalsWindow.DialogResult = true;
        }

        public void CancelButton()
        {
            GlobalModalsWindow.DialogResult = false;
        }

        public void OkButton()
        {
            GlobalModalsWindow.DialogResult = false;
        }

        public void UpdateShowOpenNoteMessage(bool value)
        {
            try
            {
                var query = "SELECT * FROM Settings WHERE Id = ?";
                var settings = DatabaseHelper.QuerySingle<SettingsModel>(query, 1);

                if (settings != null)
                {
                    settings.ShowOpenNoteMessage = value;
                    settings.UpdatedAt = DateTime.UtcNow;

                    DatabaseHelper.Update(settings);

                    ((App)Application.Current).ShowOpenNoteMessage = value;
                }
                else
                {
                    ModalMessages.showErrorModal(App.Localization.Translate("Error_Update_Note"));
                }
            }
            catch (Exception ex)
            {
                // Tratar exceções
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Update_Note"));
            }
        }

        public void UpdateShowBackupSizeMessage(bool value)
        {
            try
            {
                var query = "SELECT * FROM Settings WHERE Id = ?";
                var settings = DatabaseHelper.QuerySingle<SettingsModel>(query, 1);

                if (settings != null)
                {
                    settings.ShowBackupSizeLimiteMessage = value;
                    settings.UpdatedAt = DateTime.UtcNow;

                    DatabaseHelper.Update(settings);

                    ((App)Application.Current).ShowBackupSizeLimiteMessage = value;
                }
                else
                {
                    ModalMessages.showErrorModal(App.Localization.Translate("Error_Update_BackupSize"));
                }
            }
            catch (Exception ex)
            {
                // Tratar exceções
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Update_BackupSize"));
            }
        }

    }
}
