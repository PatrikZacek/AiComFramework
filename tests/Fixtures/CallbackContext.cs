using AIComFramework.Core.Contexts;

namespace AIComFramework.Tests.Fixtures;

internal sealed class CallbackContext(Action<AiContextBuilder> configure) : AiContext
{
    protected override void OnConfiguring(AiContextBuilder builder) => configure(builder);
}
