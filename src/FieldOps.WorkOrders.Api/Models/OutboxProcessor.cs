using System.Text.Json;
using FieldOps.Contracts.Events;
using FieldOps.WorkOrders.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FieldOps.WorkOrders.Api.Messaging;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqEventPublisher _publisher;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        RabbitMqEventPublisher publisher,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Outbox processing cycle failed.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessPendingMessagesAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                WorkOrdersDbContext>();

        var messages = await dbContext.OutboxMessages
            .Where(x => x.ProcessedAtUtc == null)
            .OrderBy(x => x.OccurredAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var outboxMessage in messages)
        {
            try
            {
                if (outboxMessage.EventType !=
                    nameof(WorkOrderAssignedEvent))
                {
                    throw new InvalidOperationException(
                        $"Unsupported event type '{outboxMessage.EventType}'.");
                }

                var assignmentEvent =
                    JsonSerializer.Deserialize<
                        WorkOrderAssignedEvent>(
                            outboxMessage.Payload);

                if (assignmentEvent is null)
                {
                    throw new JsonException(
                        "The outbox payload was empty.");
                }

                await _publisher.PublishAsync(
                    assignmentEvent,
                    cancellationToken);

                outboxMessage.ProcessedAtUtc = DateTime.UtcNow;
                outboxMessage.LastError = null;

                await dbContext.SaveChangesAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Outbox message {OutboxMessageId} processed.",
                    outboxMessage.Id);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                outboxMessage.RetryCount++;
                outboxMessage.LastError =
                    Truncate(exception.Message, 2000);

                await dbContext.SaveChangesAsync(
                    cancellationToken);

                _logger.LogError(
                    exception,
                    "Outbox message {OutboxMessageId} failed on attempt {RetryCount}.",
                    outboxMessage.Id,
                    outboxMessage.RetryCount);
            }
        }
    }

    private static string Truncate(
        string value,
        int maximumLength)
    {
        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }
}