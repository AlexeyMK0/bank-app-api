using System.ComponentModel.DataAnnotations;

namespace BankApp.Grpc;

public sealed partial class GetOutgoingInvoicesRequest : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (InvoiceStatus state in InvoiceStatuses)
        {
            if (state is InvoiceStatus.Unspecified)
            {
                yield return new ValidationResult(
                    $"Unknown invoice state {state}",
                    [nameof(state)]);
            }
        }

        if (PageSize < 1)
        {
            yield return new ValidationResult(
                "PageSize must be positive",
                [nameof(PageSize)]);
        }

        if (Guid.TryParse(SessionId, out Guid guid) is false)
        {
            yield return new ValidationResult(
                $"SessionId is incorrect",
                [nameof(SessionId)]);
        }
    }
}
