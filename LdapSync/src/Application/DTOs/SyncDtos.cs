namespace LdapSync.Application.DTOs;

public class SyncLogDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Status { get; set; }
    public string StatusDescription => Status switch
    {
        0 => "Running",
        1 => "Success",
        2 => "Failed",
        _ => "Unknown"
    };
    public int UsersProcessed { get; set; }
    public int GroupsProcessed { get; set; }
    public int MembershipsProcessed { get; set; }
    public int ErrorsCount { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsDryRun { get; set; }
}

public class SyncRequestDto
{
    public string ConfigurationId { get; set; } = string.Empty;
    public bool IsDryRun { get; set; } = false;
    public string SyncMode { get; set; } = "full"; // full or incremental
}

public class SyncResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public SyncLogDto? Log { get; set; }
    public int UsersProcessed { get; set; }
    public int GroupsProcessed { get; set; }
    public int MembershipsProcessed { get; set; }
    public int ErrorsCount { get; set; }
}

public class SyncResultDto
{
    public string ConfigurationId { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UsersProcessed { get; set; }
    public int GroupsProcessed { get; set; }
    public int MembershipsProcessed { get; set; }
    public int ErrorsCount { get; set; }
}

public class SyncAllResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalServers { get; set; }
    public int SuccessfulSyncs { get; set; }
    public int FailedSyncs { get; set; }
    public IEnumerable<SyncResultDto> Results { get; set; } = new List<SyncResultDto>();
}
