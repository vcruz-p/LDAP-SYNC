namespace LdapSync.Application.DTOs;

public class SyncConfigurationDto
{
    public string Id { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? CronSchedule { get; set; }
    public string SyncMode { get; set; } = string.Empty;
    public bool SyncUsers { get; set; }
    public bool SyncGroups { get; set; }
    public bool SyncMemberships { get; set; }
    public bool SyncPasswords { get; set; }
    public bool SyncPasswordPolicies { get; set; }
    public int? ForcePasswordResetDays { get; set; }
    public bool DeactivateOrphanUsers { get; set; }
    public bool DeleteOrphanGroups { get; set; }
    public int PageSize { get; set; }
    public int MaxEntries { get; set; }
    public string? SearchBase { get; set; }
    public string? ExcludedAttributes { get; set; }
    public DateTime? LastSync { get; set; }
    public string? LastSyncStatus { get; set; }
    public string? LastSyncError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSyncConfigurationDto
{
    public string ServerId { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
    public string? CronSchedule { get; set; }
    public string SyncMode { get; set; } = "full";
    public bool SyncUsers { get; set; } = true;
    public bool SyncGroups { get; set; } = true;
    public bool SyncMemberships { get; set; } = true;
    public bool SyncPasswords { get; set; } = false;
    public bool SyncPasswordPolicies { get; set; } = false;
    public int? ForcePasswordResetDays { get; set; }
    public bool DeactivateOrphanUsers { get; set; } = true;
    public bool DeleteOrphanGroups { get; set; } = false;
    public int PageSize { get; set; } = 500;
    public int MaxEntries { get; set; } = 0;
    public string? SearchBase { get; set; }
    public string? ExcludedAttributes { get; set; }
}

public class UpdateSyncConfigurationDto
{
    public bool? Enabled { get; set; }
    public string? CronSchedule { get; set; }
    public string? SyncMode { get; set; }
    public bool? SyncUsers { get; set; }
    public bool? SyncGroups { get; set; }
    public bool? SyncMemberships { get; set; }
    public bool? SyncPasswords { get; set; }
    public bool? SyncPasswordPolicies { get; set; }
    public int? ForcePasswordResetDays { get; set; }
    public bool? DeactivateOrphanUsers { get; set; }
    public bool? DeleteOrphanGroups { get; set; }
    public int? PageSize { get; set; }
    public int? MaxEntries { get; set; }
    public string? SearchBase { get; set; }
    public string? ExcludedAttributes { get; set; }
}
