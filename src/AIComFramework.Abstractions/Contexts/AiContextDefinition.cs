using AIComFramework.Abstractions.Trust;

namespace AIComFramework.Abstractions.Contexts;

/// <summary>Reusable domain data, explicitly selected separately from behavioral instructions.</summary>
public class AiContextDefinition
{
    /// <summary>Creates immutable reusable data with its original untrusted provenance.</summary>
    public AiContextDefinition(string id, Untrusted data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(data);
        Id = id;
        Data = data;
    }

    /// <summary>Gets the stable definition identifier.</summary>
    public string Id { get; }

    /// <summary>Gets reusable data; inspection may reveal sensitive content.</summary>
    public Untrusted Data { get; }
}
