using System.ComponentModel.DataAnnotations;

namespace Lab1.Infrastructure.Persistence;

public class ConnectionOptions
{
    [MinLength(1)]
    public required string Host { get; set; }

    [Range(0, 65535)]
    public required int Port { get; set; }

    [MinLength(1)]
    public required string Database { get; set; }

    public required string Username { get; set; }

    public required string Password { get; set; }

    public string SslMode { get; set; } = "Prefer";

    public bool Pooling { get; set; } = true;

    public string ConnectionString =>
        $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};Ssl Mode={SslMode};Pooling={Pooling}";
}