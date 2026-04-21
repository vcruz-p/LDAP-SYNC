namespace LdapSync.Domain.Entities;

public class LdapServer
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 389;
    public string BaseDn { get; set; } = string.Empty;
    public string BindDn { get; set; } = string.Empty;
    public string BindPassword { get; set; } = string.Empty;
    public bool UseTls { get; set; } = false;
    public bool ValidateCertificate { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public string UserSearchFilter { get; set; } = "(&(objectClass=inetOrgPerson)(uid=*))";
    public string GroupSearchFilter { get; set; } = "(|(objectClass=groupOfNames)(objectClass=posixGroup)(objectClass=groupOfUniqueNames))";
    public bool IsActive { get; set; } = true;
    public string? ServerType { get; set; }
    public string? Description { get; set; }
    public DateTime? LastConnectionTest { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual SyncConfiguration? SyncConfiguration { get; set; }
}
