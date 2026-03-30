using System.ComponentModel.DataAnnotations;

namespace Lab1.Application.Options;

public sealed class PasswordOptions
{
    [Required]
    [MinLength(1)]
    public required string Password { get; set; }
}