using System.ComponentModel.DataAnnotations;

namespace FieldOps.Identity.Api.Contracts
{
    public sealed class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
