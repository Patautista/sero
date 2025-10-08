using AnkiNet.CollectionFile.Database.Model;
using Microsoft.Data.Sqlite;

namespace AnkiNet.CollectionFile.Database;

internal class GraveRepository : SqliteRepository<grave>
{
    public GraveRepository(SqliteConnection connection) : base(connection)
    {
    }

    protected override string TableName => "[graves]";

    protected override IReadOnlyList<string> Columns { get; } =
        ["[usn]", "[oid]", "[type]"];

    protected override IReadOnlyList<object> GetValues(grave i)
    {
        return [i.usn, i.oid, i.type];
    }

    protected override grave Map(SqliteDataReader reader)
    {
        return new grave(
            reader.Get<int>("usn"),
            reader.Get<int>("oid"),
            reader.Get<int>("type")
        );
    }
}
