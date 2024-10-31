using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace TestJsonEnumConverter;


[JsonConverter(typeof(EnumJsonConverter<AccessType>))]
public enum AccessType
{
    Read,
    Write,
    Admin,
}

public record NonNullEnum(AccessType Access1, AccessType Access2, AccessType Access3);
public record NullableEnum(AccessType? Access1, AccessType? Access2, AccessType? Access3);

public class EnumJsonConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString() ?? throw new JsonException("Unexpected value");
        if (Enum.TryParse<TEnum>(value, out var member))
        {
            return member;
        }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"{value}");
    }
}

[TestClass]
public class JsonEnumConvererTests
{
    [TestMethod]
    public void NonNullEnum_Serialize()
    {
        var options = new JsonSerializerOptions();
        var item = new NonNullEnum(AccessType.Read, AccessType.Write, AccessType.Admin);
        var json = JsonSerializer.Serialize(item, options);
        json.Should().BeEquivalentTo("""{"Access1":"Read","Access2":"Write","Access3":"Admin"}""");
    }

    [TestMethod]
    public void NonNullEnum_Deserialize()
    {
        var options = new JsonSerializerOptions();
        var json = """{"Access1":"Read","Access2":"Write","Access3":"Admin"}""";
        var item = JsonSerializer.Deserialize<NonNullEnum>(json, options);
        item.Should().BeEquivalentTo(new NonNullEnum(AccessType.Read, AccessType.Write, AccessType.Admin));
    }

    [TestMethod]
    public void NonNullEnum_Deserialize_Empty()
    {
        var options = new JsonSerializerOptions();
        var json = """{"Access1":"Read","Access2":"","Access3":"Admin"}""";
        FluentActions.Invoking(() => JsonSerializer.Deserialize<NonNullEnum>(json, options)).Should().Throw<Exception>();
    }



    [TestMethod]
    public void NullableEnum_Serialize()
    {
        var options = new JsonSerializerOptions();
        var item = new NullableEnum(AccessType.Read, default, AccessType.Admin);
        var json = JsonSerializer.Serialize(item, options);
        json.Should().BeEquivalentTo("""{"Access1":"Read","Access2":null,"Access3":"Admin"}""");
    }

    [TestMethod]
    public void NullableEnum_Deserialize()
    {
        var options = new JsonSerializerOptions();
        var json = """{"Access1":"Read","Access2":null,"Access3":"Admin"}""";
        var item = JsonSerializer.Deserialize<NullableEnum>(json, options);
        item.Should().BeEquivalentTo(new NullableEnum(AccessType.Read, null, AccessType.Admin));
    }

    [TestMethod]
    public void NullableEnum_Deserialize_Empty()
    {
        var options = new JsonSerializerOptions();
        var json = """{"Access1":"Read","Access2":"","Access3":"Admin"}""";
        var item = JsonSerializer.Deserialize<NullableEnum>(json, options);
        item.Should().BeEquivalentTo(new NullableEnum(AccessType.Read, null, AccessType.Admin));
    }
}