namespace LdapSync.Application.DTOs;

public class LdapServerDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string BaseDn { get; set; } = string.Empty;
    public string BindDn { get; set; } = string.Empty;
    public bool UseTls { get; set; }
    public bool ValidateCertificate { get; set; }
    public int TimeoutSeconds { get; set; }
    public string UserSearchFilter { get; set; } = string.Empty;
    public string GroupSearchFilter { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ServerType { get; set; }
    public string? Description { get; set; }
    public DateTime? LastConnectionTest { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateLdapServerDto
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 389;
    public string BaseDn { get; set; } = string.Empty;
    public string BindDn { get; set; } = string.Empty;
    public string BindPassword { get; set; } = string.Empty;
    public bool UseTls { get; set; } = false;
    public bool ValidateCertificate { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public string? UserSearchFilter { get; set; }
    public string? GroupSearchFilter { get; set; }
    public string? ServerType { get; set; }
    public string? Description { get; set; }
}

public class UpdateLdapServerDto
{
    public string? Name { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? BaseDn { get; set; }
    public string? BindDn { get; set; }
    public string? BindPassword { get; set; }
    public bool? UseTls { get; set; }
    public bool? ValidateCertificate { get; set; }
    public int? TimeoutSeconds { get; set; }
    public string? UserSearchFilter { get; set; }
    public string? GroupSearchFilter { get; set; }
    public bool? IsActive { get; set; }
    public string? ServerType { get; set; }
    public string? Description { get; set; }
}
