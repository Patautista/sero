using Microsoft.Data.Sqlite;

namespace AnkiNet.CollectionFile.Database;

internal abstract class SqliteRepository<T>
{
    private readonly SqliteConnection _connection;

    protected abstract string TableName { get; }
    protected abstract IReadOnlyList<string> Columns { get; }

    protected abstract IReadOnlyList<object> GetValues(T item);


    protected abstract T Map(SqliteDataReader reader);

    protected SqliteRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public async Task<List<T>> ReadAll()
    {
        var result = new List<T>();

        var readAllSqlQuery = $"SELECT {string.Join(",", Columns)} FROM {TableName}";

        try
        {
            await using var command = new SqliteCommand(readAllSqlQuery, _connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var item = Map(reader);
                result.Add(item);
            }
        }
        catch (Exception e)
        {
            throw new IOException($"Cannot ReadAll {typeof(T).Name}", e);
        }

        return result;
    }

    public async Task Add(IReadOnlyList<T> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        string writeSqlQuery;
        {
            var values = Enumerable.Range(0, items.Count).Select(itemIndex =>
            {
                var @params = Enumerable.Range(0, Columns.Count).Select(paramIndex => ParamName(itemIndex, paramIndex));

                return $"({string.Join(',', @params)})";
            });

            writeSqlQuery = $"INSERT INTO {TableName} ({string.Join(",", Columns)}) VALUES {string.Join(',', values)}";
        }

        try
        {
            await using var command = new SqliteCommand(writeSqlQuery, _connection);

            foreach (var (item, itemIndex) in items.Select((item, itemIndex) => (item, itemIndex)))
            {
                var itemValues = GetValues(item);
                foreach (var (itemValue, paramIndex) in itemValues.Select((itemValue, paramIndex) => (itemValue, paramIndex)))
                {
                    var paramName = ParamName(itemIndex, paramIndex);
                    command.Parameters.AddWithValue(paramName, itemValue);
                }
            }

            var numberOfItemsInserted = await command.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            throw new IOException($"Cannot Add {typeof(T).Name}", e);
        }

        #region Helper function(s)

        static string ParamName(int itemIndex, int paramIndex) => $"@p{itemIndex}_{paramIndex}";

        #endregion
    }
}