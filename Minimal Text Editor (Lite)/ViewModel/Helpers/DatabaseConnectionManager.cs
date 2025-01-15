using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Minimal_Text_Editor__Lite_.ViewModel.Helpers
{
    public static class DatabaseConnectionManager
    {
        private static SQLiteConnection _connection;
        private static readonly string DbFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MinimalTextEditorLite",
            ((App)Application.Current).DatabaseFileName
        );
        private static readonly string EncryptionKey = ((App)Application.Current).KeyDatabase;

        public static SQLiteConnection GetConnection()
        {
            if (_connection == null)
            {
                var connectionString = new SQLiteConnectionString(DbFile, true, EncryptionKey);
                _connection = new SQLiteConnection(connectionString);
            }

            return _connection;
        }

        public static void CloseConnection()
        {
            _connection?.Close();
            _connection = null;
        }
    }
}
