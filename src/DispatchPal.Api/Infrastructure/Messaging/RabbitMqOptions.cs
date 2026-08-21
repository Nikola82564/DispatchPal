namespace DispatchPal.Api.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public required string HostName { get; init; }

    public int Port { get; init; }

    public required string UserName { get; init; }

    public required string Password { get; init; }

    public string ExchangeName { get; init; } =
        "dispatchpal.events";
}