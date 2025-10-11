using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AnkiNet.CollectionFile.Database.Model;
using AnkiNet.CollectionFile.Model;
using AnkiNet.CollectionFile.Model.Json;

namespace AnkiNet.CollectionFile.Mapper;

internal static class CollectionMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    public static Collection FromDb(col col)
    {
        var configuration = JsonSerializer.Deserialize<JsonConfiguration>(col.conf, SerializerOptions);
        var models = JsonSerializer.Deserialize<Dictionary<long, JsonModel>>(col.models, SerializerOptions);
        var decks = JsonSerializer.Deserialize<Dictionary<long, JsonDeck>>(col.decks, SerializerOptions);
        var decksConfiguration = JsonSerializer.Deserialize<Dictionary<long, JsonDeckConfguration>>(col.dconf, SerializerOptions);

        return new Collection(
            col.id,
            col.crt,
            col.mod,
            col.scm,
            col.ver,
            col.dty,
            col.usn,
            col.ls,
            configuration,
            models,
            decks,
            decksConfiguration,
            col.tags
        );
    }

    public static col ToDb(Collection collection)
    {
        var conf = JsonSerializer.Serialize(collection.Configuration, SerializerOptions);
        var models = JsonSerializer.Serialize(collection.Models, SerializerOptions);
        var decks = JsonSerializer.Serialize(collection.Decks, SerializerOptions);
        var dconf = JsonSerializer.Serialize(collection.DecksConfiguration, SerializerOptions);

        return new col(
            collection.Id,
            collection.CreationDateTime,
            collection.LastModifiedDateTime,
            collection.SchemaModificationDateTime,
            collection.Version,
            collection.Dirty,
            collection.UpdateSequenceNumber,
            collection.LastSyncDateTime,
            conf,
            models,
            decks,
            dconf,
            collection.Tags
        );
    }
}