using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minimal_Text_Editor__Lite_.ViewModel.Helpers
{
    public class BackgroundService : IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Task _backgroundTask;
        private int _intervalInMinutes;

        public Action TaskToRun { get; set; }

        public BackgroundService(Action taskToRun, int intervalInMinutes)
        {
            TaskToRun = taskToRun ?? throw new ArgumentNullException(nameof(taskToRun));
            _intervalInMinutes = intervalInMinutes;
        }

        public void UpdateInterval(int intervalInMinutes)
        {
            if (intervalInMinutes != _intervalInMinutes)
            {
                _intervalInMinutes = intervalInMinutes;
                RestartService(); // Reinicia o serviço apenas se o intervalo mudar
            }
        }

        public void Start()
        {
            if (_backgroundTask != null && !_backgroundTask.IsCompleted)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            _backgroundTask = Task.Run(() => RunBackgroundTask(_cancellationTokenSource.Token));
        }

        private async Task RunBackgroundTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TaskToRun?.Invoke();
                    await Task.Delay(TimeSpan.FromMinutes(_intervalInMinutes), token);
                }
                catch (TaskCanceledException)
                {
                    // Task cancellation handled here.
                }
            }
        }

        public void Stop()
        {
            if (_cancellationTokenSource != null)
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel(); // Cancela a tarefa
                }

                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null; // Libera o recurso
            }

            _backgroundTask = null; // Garante que a tarefa seja reinicializada
        }

        public void RestartService()
        {
            Stop();
            Start();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
