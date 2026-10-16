using AIComFramework.Core.Contexts;

namespace AIComFramework.Tests.Fixtures;

internal sealed class NamedDomainContext(string domainId) : AiContext
{
    public override string DomainId => domainId;
    protected override void OnConfiguring(AiContextBuilder builder) { }
}
