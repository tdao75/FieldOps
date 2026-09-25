using FieldOps.Notification.Worker;
using FieldOps.Notification.Worker.data;
using FieldOps.Notification.Worker.Services;
using Microsoft.EntityFrameworkCore;
using FieldOps.Notification.Worker.data;
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

using (var scope = host.Services.CreateScope())
{
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<NotificationsDbContext>>();

    await using var dbContext = await dbContextFactory.CreateDbContextAsync();

    await dbContext.Database.MigrateAsync();
}

host.Run();
