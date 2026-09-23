using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Text;

namespace FieldOps.Notification.Worker.data
{
    public sealed class NotificationsDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
    {
        public NotificationsDbContext CreateDbContext(string[] args)
        {
            var options =
            new DbContextOptionsBuilder<NotificationsDbContext>();

            options.UseSqlite("Data Source=fieldops-notifications.db");

            return new NotificationsDbContext(options.Options);
        }
    }
}
