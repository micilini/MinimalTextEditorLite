namespace MinimalTextEditorLite.Core.Database;

public interface IDatabaseHelper
{
    bool Insert<T>(T item);
    bool Update<T>(T item);
    bool Delete<T>(T item);
    List<T> Read<T>() where T : new();
    int Execute(string query, params object[] args);
    T? QuerySingle<T>(string query, params object[] args) where T : new();
}
