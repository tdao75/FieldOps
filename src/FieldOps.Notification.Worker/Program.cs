using FieldOps.Notification.Worker;
using FieldOps.Notification.Worker.data;
using FieldOps.Notification.Worker.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContextFactory<NotificationsDbContext>(
    options =>
        options.UseSqlite(
            builder.Configuration.GetConnectionString(
                "NotificationsDatabase")));


builder.Services.AddSingleton<EmailSender>();
builder.Services.AddHostedService<Worker>();


var host = builder.Build();
host.Run();
