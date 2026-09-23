using FieldOps.Contracts.Events;
using FieldOps.Notification.Worker.data;
using FieldOps.Notification.Worker.Models;
using FieldOps.Notification.Worker.Services;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace FieldOps.Notification.Worker;

public sealed class Worker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<Worker> _logger;
    private readonly EmailSender _emailSender;
    private readonly IDbContextFactory<NotificationsDbContext> _dbContextFactory;

    private IConnection? _connection;
    private IChannel? _channel;

    public Worker(
        IConfiguration configuration,
        ILogger<Worker> logger,
        EmailSender emailSender,
        IDbContextFactory<NotificationsDbContext> dbContextFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _emailSender = emailSender;
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = GetRequiredSetting("HostName"),
            Port = _configuration.GetValue<int>("RabbitMq:Port", 5672),
            UserName = GetRequiredSetting("UserName"),
            Password = GetRequiredSetting("Password"),
            ClientProvidedName = "FieldOps.Notification.Worker",
            AutomaticRecoveryEnabled = true
        };

        var exchangeName = GetRequiredSetting("ExchangeName");
        var queueName = GetRequiredSetting("QueueName");
        var routingKey = GetRequiredSetting("RoutingKey");
        var deadLetterExchangeName = GetRequiredSetting("DeadLetterExchangeName");

        var deadLetterQueueName = GetRequiredSetting("DeadLetterQueueName");

        var deadLetterRoutingKey = GetRequiredSetting("DeadLetterRoutingKey");


        _connection = await factory.CreateConnectionAsync(cancellationToken: stoppingToken);

        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: deadLetterExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: deadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: deadLetterQueueName,
            exchange: deadLetterExchangeName,
            routingKey: deadLetterRoutingKey,
            cancellationToken: stoppingToken);

        var queueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = deadLetterExchangeName,
            ["x-dead-letter-routing-key"] = deadLetterRoutingKey
        };

        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: queueName,
            exchange: exchangeName,
            routingKey: routingKey,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            // Copy the body before the handler returns.
            var body = eventArgs.Body.ToArray();

            try
            {
                var message = JsonSerializer.Deserialize<WorkOrderAssignedEvent>(body);

                if (message is null)
                {
                    throw new JsonException("The assignment event was empty.");
                }

                await using var dbContext = await _dbContextFactory.CreateDbContextAsync(stoppingToken);

                var alreadyProcessed = await dbContext.ProcessedMessages.AnyAsync(x => x.EventId == message.EventId, stoppingToken);

                if (alreadyProcessed)
                {
                    _logger.LogWarning("Duplicate event {EventId} ignored.", message.EventId);

                    await _channel.BasicAckAsync(deliveryTag: eventArgs.DeliveryTag,multiple: false, cancellationToken: stoppingToken);

                    return;
                }

                await SendEmailWithRetryAsync(message, stoppingToken);

                dbContext.ProcessedMessages.Add(
                    new ProcessedMessage
                    {
                        EventId = message.EventId,
                        EventType = nameof(WorkOrderAssignedEvent),
                        ProcessedAtUtc = DateTime.UtcNow
                    });

                await dbContext.SaveChangesAsync(stoppingToken);

                await _channel.BasicAckAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false,cancellationToken: stoppingToken);

                _logger.LogInformation(
                    """
                    WORK ORDER ASSIGNED
                    Event ID: {EventId}
                    Work Order ID: {WorkOrderId}
                    Technician ID: {TechnicianId}
                    Title: {Title}
                    Location: {Location}
                    Occurred: {OccurredAtUtc}
                    """,
                    message.EventId,
                    message.WorkOrderId,
                    message.TechnicianId,
                    message.Title,
                    message.Location,
                    message.OccurredAtUtc);

                return;
            }
            catch (JsonException exception)
            {
                _logger.LogError(
                    exception,
                    "Invalid message {MessageId}.",
                    eventArgs.BasicProperties.MessageId);

                // Invalid JSON will not succeed if retried.
                await _channel.BasicRejectAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to process message {MessageId}.",
                    eventArgs.BasicProperties.MessageId);

                // Retries are exhausted, so send the message to the dead-letter queue.
                await _channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "Notification worker is consuming queue {QueueName}.",
            queueName);

        try
        {
            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
            await _connection.DisposeAsync();
            _connection = null;
        }

        await base.StopAsync(cancellationToken);
    }

    private string GetRequiredSetting(string name)
    {
        var value = _configuration[$"RabbitMq:{name}"];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"RabbitMq:{name} is missing.");
        }

        return value;
    }
    private async Task SendEmailWithRetryAsync( WorkOrderAssignedEvent message,CancellationToken cancellationToken)
    {
        var maxAttempts =
            _configuration.GetValue<int>("Retry:MaxAttempts", 3);

        var delaySeconds =
            _configuration.GetValue<int>("Retry:DelaySeconds", 5);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _emailSender.SendAssignmentAsync(
                    message,
                    cancellationToken);

                return;
            }
            catch (Exception exception)
                when (attempt < maxAttempts)
            {
                _logger.LogWarning(
                    exception,
                    "Email attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelaySeconds} seconds.",
                    attempt,
                    maxAttempts,
                    delaySeconds);

                await Task.Delay(
                    TimeSpan.FromSeconds(delaySeconds),
                    cancellationToken);
            }
        }
    }
}