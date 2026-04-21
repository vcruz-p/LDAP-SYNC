namespace LdapSync.Domain.Entities;

public class LdapUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Uid { get; set; } = string.Empty;
    public string CommonName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? TelephoneNumber { get; set; }
    public string? Ou { get; set; }
    public string? Organization { get; set; }
    public int? GidNumber { get; set; }
    public int? UidNumber { get; set; }
    public string? LoginShell { get; set; }
    public string? HomeDirectory { get; set; }
    public string DistinguishedName { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PasswordLastChanged { get; set; }
    public int? PasswordMaxAgeDays { get; set; }
    public int? PasswordWarningDays { get; set; }
    public int? PasswordMinAgeDays { get; set; }
    public int? PasswordHistoryCount { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public bool IsAccountLocked { get; set; } = false;
    public DateTime? AccountLockoutExpiry { get; set; }

    // Navigation properties
    public virtual ICollection<UserGroupMembership> GroupMemberships { get; set; } = new List<UserGroupMembership>();
}
