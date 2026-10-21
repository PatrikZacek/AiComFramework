using System.Text.Json;
using AIComFramework.Abstractions.Validation;
using AIComFramework.Core.DependencyInjection;
using AIComFramework.Core.Validation;
using AIComFramework.Samples.Console.Billing;
using Microsoft.Extensions.DependencyInjection;

namespace AIComFramework.Samples.Console.Validation;

/// <summary>Demonstrates that successful parsing and domain checks are separate concerns.</summary>
public static class InvoiceValidationDemo
{
    /// <summary>Runs deterministic examples without model credentials or a database.</summary>
    public static async Task RunAsync()
    {
        var services = new ServiceCollection();
        services.AddAIComFramework()
            .AddAiResultValidator<InvoiceAnalysis, InvoiceAmountValidator>()
            .AddAiResultValidator<InvoiceAnalysis, InvoiceCurrencyValidator>();
        services.AddSingleton<InvoiceLedger>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var pipeline = scope.ServiceProvider.GetRequiredService<AiValidationPipeline>();
        var billing = new BillingAiContext();
        foreach (var (id, total, expected) in new[]
        {
            ("INV-42", 120m, AiValidationStatus.Valid),
            ("INV-42", 999m, AiValidationStatus.Invalid),
            ("UNKNOWN", 120m, AiValidationStatus.Unverifiable)
        })
        {
            using var client = new InvoiceChatClient(JsonSerializer.Serialize(new InvoiceAnalysis { Id = id, Total = total, Currency = "EUR" }));
            var result = await billing.Compose(client).WithRole(billing.Specialist).Prompt("Extract this invoice.")
                .Expect<InvoiceAnalysis>(pipeline: pipeline)
                .ValidateWith<InvoiceAmountValidator>().ValidateWith<InvoiceCurrencyValidator>().SendAsync();
            System.Console.WriteLine($"Invoice {id}: parsed={result.HasResult}; domain={result.Validation.Status}");
            foreach (var finding in result.Validation.Findings)
                System.Console.WriteLine($"  {finding.Finding.Code}: {finding.Finding.Status} — {finding.Finding.Message}");
            if (!result.HasResult || result.Validation.Status != expected)
                throw new InvalidOperationException("Unexpected invoice validation outcome.");
            // Applications explicitly inspect the status before taking any business action.
            if (result.Validation.Status == AiValidationStatus.Valid)
                System.Console.WriteLine($"  Configured checks passed for {result.GetValidatedValue().Id}; no business action is authorized.");
        }

        using var unvalidatedClient = new InvoiceChatClient("""{"Id":"INV-42","Total":999,"Currency":"EUR"}""");
        var unvalidated = await billing.Compose(unvalidatedClient).Prompt("Extract without domain checks.")
            .Expect<InvoiceAnalysis>().SendAsync();
        System.Console.WriteLine($"Typed without checks: parsed={unvalidated.HasResult}; domain={unvalidated.Validation.Status}");
        if (unvalidated.Validation.Status != AiValidationStatus.NotValidated)
            throw new InvalidOperationException("Parsing must not imply domain validation.");

        using var ordinaryClient = new InvoiceChatClient("An ordinary model answer, with no validation claim.");
        System.Console.WriteLine((await billing.Compose(ordinaryClient).Prompt("Explain an invoice.").SendAsync()).Text);
    }
}
