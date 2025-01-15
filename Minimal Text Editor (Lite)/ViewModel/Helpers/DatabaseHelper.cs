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
    public class DatabaseHelper
    {

        public static bool Insert<T>(T item)
        {
            bool result = false;
            var conn = ((App)Application.Current).DBConnection;

            conn.RunInTransaction(() =>
            {
                conn.CreateTable<T>();
                int rows = conn.Insert(item);
                if (rows > 0)
                    result = true;
            });

            return result;
        }

        public static bool Update<T>(T item)
        {
            bool result = false;
            var conn = ((App)Application.Current).DBConnection;

            conn.RunInTransaction(() =>
            {
                conn.CreateTable<T>();
                int rows = conn.Update(item);
                if (rows > 0)
                    result = true;
            });

            return result;
        }

        public static bool Delete<T>(T item)
        {
            bool result = false;
            var conn = ((App)Application.Current).DBConnection;

            conn.RunInTransaction(() =>
            {
                conn.CreateTable<T>();
                int rows = conn.Delete(item);
                if (rows > 0)
                    result = true;
            });

            return result;
        }

        public static List<T> Read<T>() where T : new()
        {
            List<T> items = new List<T>();
            var conn = ((App)Application.Current).DBConnection;

            conn.RunInTransaction(() =>
            {
                conn.CreateTable<T>();
                items = conn.Table<T>().ToList();
            });

            return items;
        }

        public static int Execute(string query, params object[] args)
        {
            int result = 0;
            var conn = ((App)Application.Current).DBConnection;

            conn.RunInTransaction(() =>
            {
                result = conn.Execute(query, args);
            });

            return result;
        }

        public static T QuerySingle<T>(string query, params object[] args) where T : new()
        {
            T result = default;
            var conn = ((App)Application.Current).DBConnection;

            conn.RunInTransaction(() =>
            {
                var queryResult = conn.Query<T>(query, args);
                result = queryResult.FirstOrDefault();
            });

            return result;
        }

    }
}
