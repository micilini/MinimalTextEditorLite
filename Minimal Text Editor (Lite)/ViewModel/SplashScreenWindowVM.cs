using Minimal_Text_Editor__Lite_.ViewModel.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minimal_Text_Editor__Lite_.ViewModel
{
    public class SplashScreenWindowVM : INotifyPropertyChanged
    {
        //Métodos do INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //Propriedades da Classe
        public event Action OnLoadingComplete;

        private int _progressValue;
        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                if (_progressValue != value)
                {
                    _progressValue = value;
                    OnPropertyChanged(nameof(ProgressValue));
                }
            }
        }

        //Métodos da Classe
        public async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                // Verifica e cria o banco de dados e tabelas
                StartupAppConfiguration startupConfig = new StartupAppConfiguration();
                startupConfig.CheckAndCreateDatabase();

                // Simulação de carregamento
                for (int i = 0; i <= 100; i++)
                {
                    ProgressValue = i;
                    Thread.Sleep(20); // Simula trabalho
                }
            });

            // Notifica que o carregamento foi concluído
            OnLoadingComplete?.Invoke();
        }

    }
}
