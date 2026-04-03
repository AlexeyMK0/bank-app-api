using Lab1.Infrastructure.Persistence.Model;

namespace Lab1.Infrastructure.Persistence.Options;

public class OperationParserOptions
{
    public required IOperationLink OperationParser { get; set; }
}