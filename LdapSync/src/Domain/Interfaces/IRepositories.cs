using LdapSync.Domain.Entities;

namespace LdapSync.Domain.Interfaces;

public interface ILdapServerRepository
{
    Task<LdapServer?> GetByIdAsync(string id);
    Task<IEnumerable<LdapServer>> GetAllAsync();
    Task<IEnumerable<LdapServer>> GetActiveServersAsync();
    Task<LdapServer> AddAsync(LdapServer server);
    Task UpdateAsync(LdapServer server);
    Task DeleteAsync(string id);
}

public interface ILdapUserRepository
{
    Task<LdapUser?> GetByIdAsync(string id);
    Task<LdapUser?> GetByUidAsync(string uid);
    Task<LdapUser?> GetByDistinguishedNameAsync(string dn);
    Task<IEnumerable<LdapUser>> GetAllAsync();
    Task<IEnumerable<LdapUser>> GetActiveUsersAsync();
    Task<LdapUser> AddAsync(LdapUser user);
    Task UpdateAsync(LdapUser user);
    Task DeleteAsync(string id);
    Task<bool> ExistsByDistinguishedNameAsync(string dn);
}

public interface ILdapGroupRepository
{
    Task<LdapGroup?> GetByIdAsync(string id);
    Task<LdapGroup?> GetByDistinguishedNameAsync(string dn);
    Task<IEnumerable<LdapGroup>> GetAllAsync();
    Task<IEnumerable<LdapGroup>> GetActiveGroupsAsync();
    Task<LdapGroup> AddAsync(LdapGroup group);
    Task UpdateAsync(LdapGroup group);
    Task DeleteAsync(string id);
    Task<bool> ExistsByDistinguishedNameAsync(string dn);
}

public interface ISyncConfigurationRepository
{
    Task<SyncConfiguration?> GetByIdAsync(string id);
    Task<SyncConfiguration?> GetByServerIdAsync(string serverId);
    Task<IEnumerable<SyncConfiguration>> GetAllAsync();
    Task<IEnumerable<SyncConfiguration>> GetEnabledConfigurationsAsync();
    Task<SyncConfiguration> AddAsync(SyncConfiguration config);
    Task UpdateAsync(SyncConfiguration config);
    Task DeleteAsync(string id);
}

public interface ISyncLogRepository
{
    Task<SyncLog?> GetByIdAsync(string id);
    Task<IEnumerable<SyncLog>> GetAllAsync();
    Task<IEnumerable<SyncLog>> GetRecentLogsAsync(int count);
    Task<SyncLog> AddAsync(SyncLog log);
    Task UpdateAsync(SyncLog log);
}

public interface IUserGroupMembershipRepository
{
    Task<UserGroupMembership?> GetByIdAsync(string id);
    Task<IEnumerable<UserGroupMembership>> GetByUserIdAsync(string userId);
    Task<IEnumerable<UserGroupMembership>> GetByGroupIdAsync(string groupId);
    Task<UserGroupMembership> AddAsync(UserGroupMembership membership);
    Task UpdateAsync(UserGroupMembership membership);
    Task DeleteAsync(string id);
    Task DeleteByUserIdAsync(string userId);
    Task DeleteByGroupIdAsync(string groupId);
    Task<bool> ExistsAsync(string userId, string groupId);
}

public interface ILdapService
{
    Task<bool> TestConnectionAsync(LdapServer server);
    Task<(bool Success, string? ErrorMessage)> TestConnectionWithDetailsAsync(LdapServer server);
    Task<IEnumerable<LdapUser>> SyncUsersAsync(LdapServer server, SyncConfiguration config);
    Task<IEnumerable<LdapGroup>> SyncGroupsAsync(LdapServer server, SyncConfiguration config);
    Task<IEnumerable<UserGroupMembership>> SyncMembershipsAsync(LdapServer server, SyncConfiguration config);
}

public interface ISyncService
{
    Task<SyncLog> ExecuteFullSyncAsync(string configurationId, bool isDryRun = false);
    Task<SyncLog> ExecuteIncrementalSyncAsync(string configurationId, bool isDryRun = false);
}
