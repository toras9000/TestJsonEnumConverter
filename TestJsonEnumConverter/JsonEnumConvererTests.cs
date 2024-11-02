using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace TestJsonEnumConverter;


public enum AccessType
{
    Read,
    Write,
    Admin,
}

public record NonNullEnum(AccessType Access1, AccessType Access2, AccessType Access3);
public record NullableEnum(AccessType? Access1, AccessType? Access2, AccessType? Access3);

[RequiresDynamicCode("User dynamic instance creation.")]
public class EnumJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert.IsEnum) return true;

        var nullableType = Nullable.GetUnderlyingType(typeToConvert);
        if (nullableType?.IsEnum == true) return true;

        return false;
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert.IsEnum)
        {
            var converterType = typeof(EnumJsonConverter<>).MakeGenericType(typeToConvert);
            var constructor = converterType.GetConstructor([]);
            return constructor?.Invoke(default) as JsonConverter;
        }

        var nullableType = Nullable.GetUnderlyingType(typeToConvert);
        if (nullableType?.IsEnum == true)
        {
            var converterType = typeof(NullableEnumJsonConverter<>).MakeGenericType(nullableType);
            var constructor = converterType.GetConstructor([]);
            return constructor?.Invoke(default) as JsonConverter;
        }

        return null;
    }
}

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

public class NullableEnumJsonConverter<TEnum> : JsonConverter<TEnum?> where TEnum : struct, Enum
{
    public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString() ?? throw new JsonException("Unexpected value");
        if (string.IsNullOrWhiteSpace(value)) return default;   // ÇΩÇæÇ±ÇÍÇ™ÇµÇΩÇ¢ÅB ì¡éÍílÇ©ÇÁ null Ç÷ÇÃïœä∑ÅB
        if (Enum.TryParse<TEnum>(value, out var member))
        {
            return member;
        }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
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
        options.Converters.Add(new EnumJsonConverterFactory());
        var item = new NonNullEnum(AccessType.Read, AccessType.Write, AccessType.Admin);
        var json = JsonSerializer.Serialize(item, options);
        json.Should().BeEquivalentTo("""{"Access1":"Read","Access2":"Write","Access3":"Admin"}""");
    }

    [TestMethod]
    public void NonNullEnum_Deserialize()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new EnumJsonConverterFactory());
        var json = """{"Access1":"Read","Access2":"Write","Access3":"Admin"}""";
        var item = JsonSerializer.Deserialize<NonNullEnum>(json, options);
        item.Should().BeEquivalentTo(new NonNullEnum(AccessType.Read, AccessType.Write, AccessType.Admin));
    }

    [TestMethod]
    public void NonNullEnum_Deserialize_Empty()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new EnumJsonConverterFactory());
        var json = """{"Access1":"Read","Access2":"","Access3":"Admin"}""";
        FluentActions.Invoking(() => JsonSerializer.Deserialize<NonNullEnum>(json, options)).Should().Throw<Exception>();
    }



    [TestMethod]
    public void NullableEnum_Serialize()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new EnumJsonConverterFactory());
        var item = new NullableEnum(AccessType.Read, default, AccessType.Admin);
        var json = JsonSerializer.Serialize(item, options);
        json.Should().BeEquivalentTo("""{"Access1":"Read","Access2":null,"Access3":"Admin"}""");
    }

    [TestMethod]
    public void NullableEnum_Deserialize()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new EnumJsonConverterFactory());
        var json = """{"Access1":"Read","Access2":null,"Access3":"Admin"}""";
        var item = JsonSerializer.Deserialize<NullableEnum>(json, options);
        item.Should().BeEquivalentTo(new NullableEnum(AccessType.Read, null, AccessType.Admin));
    }

    [TestMethod]
    public void NullableEnum_Deserialize_Empty()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new EnumJsonConverterFactory());
        var json = """{"Access1":"Read","Access2":"","Access3":"Admin"}""";
        var item = JsonSerializer.Deserialize<NullableEnum>(json, options);
        item.Should().BeEquivalentTo(new NullableEnum(AccessType.Read, null, AccessType.Admin));
    }
}