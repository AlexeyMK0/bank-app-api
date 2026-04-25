using System.ComponentModel.DataAnnotations;

namespace BankApp.Grpc;

public sealed partial class CreateAccountRequest : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Guid.TryParse(UserExternalId, out Guid guid) is false)
        {
            yield return new ValidationResult(
                $"UserId is incorrect",
                [nameof(UserExternalId)]);
        }
    }
}