using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIComFramework.Tests.Shared.Fixtures;

internal sealed class ThrowingInvoiceConverter : JsonConverter<InvoiceResult>
{
    internal InvalidOperationException Failure { get; } = new("Unexpected converter failure.");
    public override InvoiceResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw Failure;
    public override void Write(Utf8JsonWriter writer, InvoiceResult value, JsonSerializerOptions options) => throw Failure;
}
