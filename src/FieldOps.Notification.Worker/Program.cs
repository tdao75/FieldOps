using FieldOps.Notification.Worker;
using FieldOps.Notification.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<EmailSender>();
builder.Services.AddHostedService<Worker>();


var host = builder.Build();
host.Run();
