using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace FieldOps.Identity.Api.Models
{
    public sealed class ApplicationUser :IdentityUser<Guid>
    {
        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        [NotMapped]
        public string DisplayName =>$"{FirstName} {LastName}".Trim();
    }
}
