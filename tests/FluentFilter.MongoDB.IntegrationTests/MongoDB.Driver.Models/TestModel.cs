using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver.Models;

public class TestModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public String Id { get; set; }

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

    public Byte[][] BinaryArray { get; set; }

    public Boolean[] BooleanArray { get; set; }

    public DateTime[] DateArray { get; set; }

    public Decimal[] DecimalArray { get; set; }

    public Int32[] IntegerArray { get; set; }

    public Guid[] GuidArray { get; set; }

    public Object[] ObjectArray { get; set; }

    public String[] StringArray { get; set; }

    public ComplexModel Inner { get; set; }

    public SimpleModel[] InnerArray { get; set; }
}
