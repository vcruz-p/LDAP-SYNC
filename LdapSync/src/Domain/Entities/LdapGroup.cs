namespace LdapSync.Domain.Entities;

public class LdapGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CommonName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? GidNumber { get; set; }
    public string DistinguishedName { get; set; } = string.Empty;
    public string? ObjectClass { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? IdOrganizativa { get; set; }

    // Navigation properties
    public virtual ICollection<UserGroupMembership> UserMemberships { get; set; } = new List<UserGroupMembership>();
}
