namespace LdapSync.Domain.Entities;

public class SyncLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int Status { get; set; } // 0: Running, 1: Success, 2: Failed
    public int UsersProcessed { get; set; }
    public int GroupsProcessed { get; set; }
    public int MembershipsProcessed { get; set; }
    public int ErrorsCount { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsDryRun { get; set; } = false;
}
