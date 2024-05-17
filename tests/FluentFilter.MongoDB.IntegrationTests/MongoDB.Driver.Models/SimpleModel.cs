using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver.Models;

public class SimpleModel
{
    [BsonRepresentation(BsonType.Binary)]
    public Byte[] Binary { get; set; }

    [BsonRepresentation(BsonType.Boolean)]
    public Boolean? Boolean { get; set; }

    [BsonRepresentation(BsonType.DateTime)]
    public DateTime? Date { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public Decimal? Decimal { get; set; }

    [BsonRepresentation(BsonType.Int32)]
    public Int32? Integer { get; set; }

    public Guid? Guid { get; set; }

    [BsonRepresentation(BsonType.Boolean)]
    [BsonIgnoreIfNull(true)]
    public Boolean? Missing { get; set; }

    public Object Object { get; set; }

    [BsonRepresentation(BsonType.String)]
    public String String { get; set; }
}
