namespace MinimalTextEditorLite.Core.Database;

public sealed class DatabaseHelper(ISqliteConnectionFactory connectionFactory) : IDatabaseHelper
{
    public bool Insert<T>(T item)
    {
        var result = false;
        var conn = connectionFactory.GetConnection();

        conn.RunInTransaction(() =>
        {
            conn.CreateTable<T>();
            result = conn.Insert(item) > 0;
        });

        return result;
    }

    public bool Update<T>(T item)
    {
        var result = false;
        var conn = connectionFactory.GetConnection();

        conn.RunInTransaction(() =>
        {
            conn.CreateTable<T>();
            result = conn.Update(item) > 0;
        });

        return result;
    }

    public bool Delete<T>(T item)
    {
        var result = false;
        var conn = connectionFactory.GetConnection();

        conn.RunInTransaction(() =>
        {
            conn.CreateTable<T>();
            result = conn.Delete(item) > 0;
        });

        return result;
    }

    public List<T> Read<T>() where T : new()
    {
        var conn = connectionFactory.GetConnection();
        var items = new List<T>();

        conn.RunInTransaction(() =>
        {
            conn.CreateTable<T>();
            items = conn.Table<T>().ToList();
        });

        return items;
    }

    public int Execute(string query, params object[] args)
    {
        var conn = connectionFactory.GetConnection();
        var result = 0;

        conn.RunInTransaction(() =>
        {
            result = conn.Execute(query, args);
        });

        return result;
    }

    public T? QuerySingle<T>(string query, params object[] args) where T : new()
    {
        var conn = connectionFactory.GetConnection();
        T? result = default;

        conn.RunInTransaction(() =>
        {
            result = conn.Query<T>(query, args).FirstOrDefault();
        });

        return result;
    }
}
