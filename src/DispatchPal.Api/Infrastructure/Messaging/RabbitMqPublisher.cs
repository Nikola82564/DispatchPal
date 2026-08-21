using System.Text.Json;
using DispatchPal.Contracts.Events;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using DispatchPal.Api.Domain.Entities;

namespace DispatchPal.Api.Infrastructure.Messaging;

public sealed class RabbitMqPublisher(IOptions<RabbitMqOptions> options,
ILogger<RabbitMqPublisher> logger) : IIntegrationEventPublisher, IAsyncDisposable
{
    private const string RoutingKey = "dispatch-request.created";

    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _synchronization = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(
      OutboxMessage message,
      CancellationToken cancellationToken)
    {
        await _synchronization.WaitAsync(cancellationToken);

        try
        {
            await EnsureConnectedAsync(cancellationToken);

            var body = Encoding.UTF8.GetBytes(message.Payload);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = message.Id.ToString(),
                Type = message.EventType
            };

            await _channel!.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: message.RoutingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Published Outbox message {MessageId} of type {EventType}.",
                message.Id,
                message.EventType);
        }
        finally
        {
            _synchronization.Release();
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
        {
            return;
        }

        if(_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            ClientProvidedName = "DispatchPal.Api"
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);

        _channel = await _connection.CreateChannelAsync(cancellationToken : cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _synchronization.Dispose();
    }
}