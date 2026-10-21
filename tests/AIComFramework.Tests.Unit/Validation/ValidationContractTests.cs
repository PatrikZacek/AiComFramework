using AIComFramework.Abstractions.Results;
using AIComFramework.Abstractions.Validation;
using AIComFramework.Tests.Shared.Fixtures;

namespace AIComFramework.Tests.Unit.Validation;

/// <summary>Pure validation contract invariants require neither provider nor service container.</summary>
public sealed class ValidationContractTests
{
    /// <summary>Validators cannot claim that a selected required check was not requested.</summary>
    [Theory]
    [InlineData(AiValidationStatus.NotValidated)]
    [InlineData((AiValidationStatus)99)]
    public void FindingRejectsUnsupportedStates(AiValidationStatus status) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new AiValidationFinding(status, "code", "message"));

    /// <summary>Diagnostics require a usable code.</summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void FindingRequiresCode(string code) =>
        Assert.Throws<ArgumentException>(() => new AiValidationFinding(AiValidationStatus.Valid, code, "message"));

    /// <summary>Reusing a contract does not mutate its required checks or reference data.</summary>
    [Fact]
    public void ContractSelectionIsImmutableAndOrdered()
    {
        var original = new AiResultContract<InvoiceResult>();
        var selected = original.ValidateWith<InvoiceCurrencyValidator>().ValidateWith<InvoiceAmountValidator>()
            .ValidateWith<InvoiceCurrencyValidator>().WithReference("invoice", "first");
        var changed = selected.WithReference("invoice", "second");
        Assert.False(original.ValidationRequested);
        Assert.True(selected.ValidationRequested);
        Assert.Empty(original.ReferenceData);
        Assert.Equal("first", selected.ReferenceData["invoice"]);
        Assert.Equal("second", changed.ReferenceData["invoice"]);
        Assert.Equal(new[] { typeof(InvoiceCurrencyValidator), typeof(InvoiceAmountValidator) }, selected.ValidatorTypes.ToArray());
    }
}
