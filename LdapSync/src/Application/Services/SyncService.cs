using LdapSync.Domain.Entities;
using LdapSync.Domain.Interfaces;

namespace LdapSync.Application.Services;

public class SyncService : ISyncService
{
    private readonly ILdapServerRepository _serverRepository;
    private readonly ILdapUserRepository _userRepository;
    private readonly ILdapGroupRepository _groupRepository;
    private readonly IUserGroupMembershipRepository _membershipRepository;
    private readonly ISyncConfigurationRepository _configRepository;
    private readonly ISyncLogRepository _syncLogRepository;
    private readonly ILdapService _ldapService;

    public SyncService(
        ILdapServerRepository serverRepository,
        ILdapUserRepository userRepository,
        ILdapGroupRepository groupRepository,
        IUserGroupMembershipRepository membershipRepository,
        ISyncConfigurationRepository configRepository,
        ISyncLogRepository syncLogRepository,
        ILdapService ldapService)
    {
        _serverRepository = serverRepository;
        _userRepository = userRepository;
        _groupRepository = groupRepository;
        _membershipRepository = membershipRepository;
        _configRepository = configRepository;
        _syncLogRepository = syncLogRepository;
        _ldapService = ldapService;
    }

    public async Task<SyncLog> ExecuteFullSyncAsync(string configurationId, bool isDryRun = false)
    {
        var config = await _configRepository.GetByIdAsync(configurationId);
        if (config == null)
            throw new ArgumentException($"Configuration with id {configurationId} not found");

        var server = await _serverRepository.GetByIdAsync(config.ServerId);
        if (server == null)
            throw new ArgumentException($"Server with id {config.ServerId} not found");

        var syncLog = new SyncLog
        {
            StartedAt = DateTime.UtcNow,
            Status = 0, // Running
            IsDryRun = isDryRun
        };

        await _syncLogRepository.AddAsync(syncLog);

        try
        {
            int usersProcessed = 0;
            int groupsProcessed = 0;
            int membershipsProcessed = 0;
            int errorsCount = 0;

            // Sync Groups
            if (config.SyncGroups)
            {
                var groups = await _ldapService.SyncGroupsAsync(server, config);
                groupsProcessed = groups.Count();
                
                if (!isDryRun)
                {
                    foreach (var group in groups)
                    {
                        var existingGroup = await _groupRepository.GetByDistinguishedNameAsync(group.DistinguishedName);
                        if (existingGroup == null)
                        {
                            await _groupRepository.AddAsync(group);
                        }
                        else
                        {
                            group.Id = existingGroup.Id;
                            group.CreatedAt = existingGroup.CreatedAt;
                            await _groupRepository.UpdateAsync(group);
                        }
                    }
                }
            }

            // Sync Users
            if (config.SyncUsers)
            {
                var users = await _ldapService.SyncUsersAsync(server, config);
                usersProcessed = users.Count();
                
                if (!isDryRun)
                {
                    foreach (var user in users)
                    {
                        var existingUser = await _userRepository.GetByDistinguishedNameAsync(user.DistinguishedName);
                        if (existingUser == null)
                        {
                            await _userRepository.AddAsync(user);
                        }
                        else
                        {
                            user.Id = existingUser.Id;
                            user.CreatedAt = existingUser.CreatedAt;
                            await _userRepository.UpdateAsync(user);
                        }
                    }
                }
            }

            // Sync Memberships
            if (config.SyncMemberships)
            {
                var memberships = await _ldapService.SyncMembershipsAsync(server, config);
                membershipsProcessed = memberships.Count();
                
                if (!isDryRun)
                {
                    foreach (var membership in memberships)
                    {
                        var exists = await _membershipRepository.ExistsAsync(membership.UserId, membership.GroupId);
                        if (!exists)
                        {
                            await _membershipRepository.AddAsync(membership);
                        }
                        else
                        {
                            membership.SyncedAt = DateTime.UtcNow;
                            await _membershipRepository.UpdateAsync(membership);
                        }
                    }
                }
            }

            syncLog.CompletedAt = DateTime.UtcNow;
            syncLog.Status = 1; // Success
            syncLog.UsersProcessed = usersProcessed;
            syncLog.GroupsProcessed = groupsProcessed;
            syncLog.MembershipsProcessed = membershipsProcessed;
            syncLog.ErrorsCount = errorsCount;

            config.LastSync = DateTime.UtcNow;
            config.LastSyncStatus = "Success";
            await _configRepository.UpdateAsync(config);
        }
        catch (Exception ex)
        {
            syncLog.CompletedAt = DateTime.UtcNow;
            syncLog.Status = 2; // Failed
            syncLog.ErrorMessage = ex.Message;
            syncLog.ErrorsCount++;

            config.LastSync = DateTime.UtcNow;
            config.LastSyncStatus = "Failed";
            config.LastSyncError = ex.Message;
            await _configRepository.UpdateAsync(config);

            throw;
        }
        finally
        {
            await _syncLogRepository.UpdateAsync(syncLog);
        }

        return syncLog;
    }

    public async Task<SyncLog> ExecuteIncrementalSyncAsync(string configurationId, bool isDryRun = false)
    {
        // For now, incremental sync works similar to full sync
        // In a real scenario, you would only sync changes since last sync
        return await ExecuteFullSyncAsync(configurationId, isDryRun);
    }
}
