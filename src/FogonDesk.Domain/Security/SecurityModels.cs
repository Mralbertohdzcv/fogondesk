using System;

namespace FogonDesk.Domain.Security
{
    public sealed class UserAccount
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string RoleCode { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public DateTime? LastLoginUtc { get; set; }
    }

    public sealed class AuthenticatedSession
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string RoleCode { get; set; }
        public DateTime SignedInUtc { get; set; }
    }

    public sealed class PermissionGrant
    {
        public string RoleCode { get; set; }
        public string PermissionCode { get; set; }
    }
}
