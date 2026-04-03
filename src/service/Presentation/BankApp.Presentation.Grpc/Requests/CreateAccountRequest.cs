using System.ComponentModel.DataAnnotations;

namespace BankApp.Grpc;

public sealed partial class CreateAccountRequest : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PinCode.Length == 0)
        {
            yield return new ValidationResult(
                "PinCode cannot be empty",
                [nameof(PinCode)]);
        }

        if (Guid.TryParse(SessionId, out Guid guid) is false)
        {
            yield return new ValidationResult(
                $"SessionId is incorrect",
                [nameof(SessionId)]);
        }
    }
}