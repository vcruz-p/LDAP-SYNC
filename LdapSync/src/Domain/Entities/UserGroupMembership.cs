namespace LdapSync.Domain.Entities;

public class UserGroupMembership
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string MemberAttributeType { get; set; } = "member";
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual LdapUser? User { get; set; }
    public virtual LdapGroup? Group { get; set; }
}
