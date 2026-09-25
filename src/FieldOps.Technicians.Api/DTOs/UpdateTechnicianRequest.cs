using System.ComponentModel.DataAnnotations;

namespace FieldOps.Technicians.Api.DTOs
{
    public sealed class UpdateTechnicianRequest
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(320)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(30)]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        public string? Skills { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
