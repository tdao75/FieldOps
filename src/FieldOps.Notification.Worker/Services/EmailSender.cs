using FieldOps.Contracts.Events;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FieldOps.Notification.Worker.Services;

public sealed class EmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(
        IConfiguration configuration,
        ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAssignmentAsync(WorkOrderAssignedEvent assignment, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(GetRequiredSetting("FromName"), GetRequiredSetting("FromAddress")));

        var recipientAddress = string.IsNullOrWhiteSpace(assignment.TechnicianEmail) ? GetRequiredSetting("DevelopmentRecipient"): assignment.TechnicianEmail;

        var recipientName =string.IsNullOrWhiteSpace(assignment.TechnicianName) ? "FieldOps Technician" : assignment.TechnicianName;

        message.To.Add(new MailboxAddress(recipientName, recipientAddress));

        message.Subject = $"Work order assigned: {assignment.Title}";

        message.Body = new TextPart("plain")
        {
            Text =
                $"""
                A work order has been assigned to you.

                Title: {assignment.Title}
                Location: {assignment.Location}
                Work order ID: {assignment.WorkOrderId}
                Technician ID: {assignment.TechnicianId}
                Assigned at: {assignment.OccurredAtUtc:yyyy-MM-dd HH:mm:ss} UTC

                Please sign in to FieldOps to review the work order.
                """
        };

        var host = GetRequiredSetting("Host");
        var port = _configuration.GetValue<int>("Smtp:Port", 1025);

        using var smtpClient = new SmtpClient();

        await smtpClient.ConnectAsync(host, port,SecureSocketOptions.None, cancellationToken);

        await smtpClient.SendAsync(message,cancellationToken);

        await smtpClient.DisconnectAsync(quit: true, cancellationToken);

        _logger.LogInformation("Assignment email sent for work order {WorkOrderId}.",assignment.WorkOrderId);
    }

    private string GetRequiredSetting(string name)
    {
        var value = _configuration[$"Smtp:{name}"];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Smtp:{name} is missing.");
        }

        return value;
    }
}