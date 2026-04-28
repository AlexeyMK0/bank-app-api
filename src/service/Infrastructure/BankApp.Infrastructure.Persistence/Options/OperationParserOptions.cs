using BankApp.Infrastructure.Persistence.Model;

namespace BankApp.Infrastructure.Persistence.Options;

public class OperationParserOptions
{
    public required IOperationLink OperationParser { get; set; }
}