using System.Text.Json;
using FieldOps.Contracts.Events;
using RabbitMQ.Client;

namespace FieldOps.WorkOrders.Api.Messaging;

public sealed class RabbitMqEventPublisher : IAsyncDisposable
{
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly ConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly string _routingKey;
    private readonly string _deadLetterExchangeName;
    private readonly string _deadLetterQueueName;
    private readonly string _deadLetterRoutingKey;

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _logger = logger;

        _exchangeName =
            GetRequiredSetting(configuration, "ExchangeName");

        _queueName =
            GetRequiredSetting(configuration, "QueueName");

        _routingKey =
            GetRequiredSetting(configuration, "RoutingKey");

        _deadLetterExchangeName =
            GetRequiredSetting(
                configuration,
                "DeadLetterExchangeName");

        _deadLetterQueueName =
            GetRequiredSetting(
                configuration,
                "DeadLetterQueueName");

        _deadLetterRoutingKey =
            GetRequiredSetting(
                configuration,
                "DeadLetterRoutingKey");

        _connectionFactory = new ConnectionFactory
        {
            HostName =
                GetRequiredSetting(configuration, "HostName"),

            Port = configuration.GetValue<int>(
                "RabbitMq:Port",
                5672),

            UserName =
                GetRequiredSetting(configuration, "UserName"),

            Password =
                GetRequiredSetting(configuration, "Password"),

            ClientProvidedName = "FieldOps.WorkOrders.Api",
            AutomaticRecoveryEnabled = true
        };
    }

    public async Task PublishAsync(
        WorkOrderAssignedEvent message,
        CancellationToken cancellationToken)
    {
        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            await EnsureConnectedAsync(cancellationToken);

            var body = JsonSerializer.SerializeToUtf8Bytes(message);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                Persistent = true,
                MessageId = message.EventId.ToString(),
                Type = nameof(WorkOrderAssignedEvent)
            };

            await _channel!.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: _routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Published event {EventId} for work order {WorkOrderId}.",
                message.EventId,
                message.WorkOrderId);
        }
        catch
        {
            await ResetConnectionAsync();
            throw;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task EnsureConnectedAsync(
        CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } &&
            _channel is { IsOpen: true })
        {
            return;
        }

        await ResetConnectionAsync();

        _logger.LogInformation(
            "Connecting to RabbitMQ at {HostName}:{Port}.",
            _connectionFactory.HostName,
            _connectionFactory.Port);

        _connection =
            await _connectionFactory.CreateConnectionAsync(
                cancellationToken: cancellationToken);

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        _channel = await _connection.CreateChannelAsync(
            options: channelOptions,
            cancellationToken: cancellationToken);

        await DeclareTopologyAsync(
            _channel,
            cancellationToken);

        _logger.LogInformation(
            "RabbitMQ publisher connected.");
    }

    private async Task DeclareTopologyAsync(
        IChannel channel,
        CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: _deadLetterExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: _deadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: _deadLetterQueueName,
            exchange: _deadLetterExchangeName,
            routingKey: _deadLetterRoutingKey,
            cancellationToken: cancellationToken);

        var queueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] =
                _deadLetterExchangeName,

            ["x-dead-letter-routing-key"] =
                _deadLetterRoutingKey
        };

        await channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: _queueName,
            exchange: _exchangeName,
            routingKey: _routingKey,
            cancellationToken: cancellationToken);
    }

    private async Task ResetConnectionAsync()
    {
        if (_channel is not null)
        {
            try
            {
                await _channel.DisposeAsync();
            }
            catch (Exception exception)
            {
                _logger.LogDebug(
                    exception,
                    "Error disposing RabbitMQ channel.");
            }

            _channel = null;
        }

        if (_connection is not null)
        {
            try
            {
                await _connection.DisposeAsync();
            }
            catch (Exception exception)
            {
                _logger.LogDebug(
                    exception,
                    "Error disposing RabbitMQ connection.");
            }

            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _connectionLock.WaitAsync();

        try
        {
            await ResetConnectionAsync();
        }
        finally
        {
            _connectionLock.Release();
            _connectionLock.Dispose();
        }
    }

    private static string GetRequiredSetting(
        IConfiguration configuration,
        string name)
    {
        var value = configuration[$"RabbitMq:{name}"];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"RabbitMq:{name} is missing.");
        }

        return value;
    }
}