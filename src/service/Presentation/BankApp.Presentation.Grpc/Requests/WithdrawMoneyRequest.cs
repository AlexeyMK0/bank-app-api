using System.ComponentModel.DataAnnotations;

namespace BankApp.Grpc;

public sealed partial class WithdrawMoneyRequest : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Amount is null)
        {
            yield return new ValidationResult("Money object is missing", [nameof(Amount)]);
            yield break;
        }

        // Check if units and nano have different sign
        if (Amount.Units * Amount.Nanos < 0)
        {
            yield return new ValidationResult(
                "Bad money format: units and nano must have same sign",
                [nameof(Amount)]);
        }

        if (Amount.Nanos < -999_999_999 || Amount.Nanos > 999_999_999)
        {
            yield return new ValidationResult(
                "Invalid nanos value",
                [nameof(Amount)]);
        }

        if (Amount.DecimalValue < 0)
        {
            yield return new ValidationResult(
                "Withdraw amount must be positive");
        }

        if (Guid.TryParse(SessionId, out Guid guid) is false)
        {
            yield return new ValidationResult(
                $"SessionId is incorrect",
                [nameof(SessionId)]);
        }
    }
}