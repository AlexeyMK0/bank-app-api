using System.ComponentModel.DataAnnotations;

namespace BankApp.Grpc;

public sealed partial class CreateUserSessionRequest : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PinCode.Length == 0)
        {
            yield return new ValidationResult(
                "PinCode cannot be empty",
                [nameof(PinCode)]);
        }
    }
}