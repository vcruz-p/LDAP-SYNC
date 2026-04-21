using LdapSync.Domain.Entities;
using LdapSync.Domain.Interfaces;
using LdapSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LdapSync.Infrastructure.Repositories;

public class LdapServerRepository : ILdapServerRepository
{
    private readonly ApplicationDbContext _context;

    public LdapServerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LdapServer?> GetByIdAsync(string id) =>
        await _context.LdapServers.FindAsync(id);

    public async Task<IEnumerable<LdapServer>> GetAllAsync() =>
        await _context.LdapServers.ToListAsync();

    public async Task<IEnumerable<LdapServer>> GetActiveServersAsync() =>
        await _context.LdapServers.Where(s => s.IsActive).ToListAsync();

    public async Task<LdapServer> AddAsync(LdapServer server)
    {
        await _context.LdapServers.AddAsync(server);
        await _context.SaveChangesAsync();
        return server;
    }

    public async Task UpdateAsync(LdapServer server)
    {
        server.UpdatedAt = DateTime.UtcNow;
        _context.LdapServers.Update(server);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var server = await _context.LdapServers.FindAsync(id);
        if (server != null)
        {
            _context.LdapServers.Remove(server);
            await _context.SaveChangesAsync();
        }
    }
}

public class LdapUserRepository : ILdapUserRepository
{
    private readonly ApplicationDbContext _context;

    public LdapUserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LdapUser?> GetByIdAsync(string id) =>
        await _context.LdapUsers.FindAsync(id);

    public async Task<LdapUser?> GetByUidAsync(string uid) =>
        await _context.LdapUsers.FirstOrDefaultAsync(u => u.Uid == uid);

    public async Task<LdapUser?> GetByDistinguishedNameAsync(string dn) =>
        await _context.LdapUsers.FirstOrDefaultAsync(u => u.DistinguishedName == dn);

    public async Task<IEnumerable<LdapUser>> GetAllAsync() =>
        await _context.LdapUsers.ToListAsync();

    public async Task<IEnumerable<LdapUser>> GetActiveUsersAsync() =>
        await _context.LdapUsers.Where(u => u.IsActive).ToListAsync();

    public async Task<LdapUser> AddAsync(LdapUser user)
    {
        await _context.LdapUsers.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(LdapUser user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.LdapUsers.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var user = await _context.LdapUsers.FindAsync(id);
        if (user != null)
        {
            _context.LdapUsers.Remove(user);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsByDistinguishedNameAsync(string dn) =>
        await _context.LdapUsers.AnyAsync(u => u.DistinguishedName == dn);
}

public class LdapGroupRepository : ILdapGroupRepository
{
    private readonly ApplicationDbContext _context;

    public LdapGroupRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LdapGroup?> GetByIdAsync(string id) =>
        await _context.LdapGroups.FindAsync(id);

    public async Task<LdapGroup?> GetByDistinguishedNameAsync(string dn) =>
        await _context.LdapGroups.FirstOrDefaultAsync(g => g.DistinguishedName == dn);

    public async Task<IEnumerable<LdapGroup>> GetAllAsync() =>
        await _context.LdapGroups.ToListAsync();

    public async Task<IEnumerable<LdapGroup>> GetActiveGroupsAsync() =>
        await _context.LdapGroups.Where(g => g.IsActive).ToListAsync();

    public async Task<LdapGroup> AddAsync(LdapGroup group)
    {
        await _context.LdapGroups.AddAsync(group);
        await _context.SaveChangesAsync();
        return group;
    }

    public async Task UpdateAsync(LdapGroup group)
    {
        group.UpdatedAt = DateTime.UtcNow;
        _context.LdapGroups.Update(group);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var group = await _context.LdapGroups.FindAsync(id);
        if (group != null)
        {
            _context.LdapGroups.Remove(group);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsByDistinguishedNameAsync(string dn) =>
        await _context.LdapGroups.AnyAsync(g => g.DistinguishedName == dn);
}

public class SyncConfigurationRepository : ISyncConfigurationRepository
{
    private readonly ApplicationDbContext _context;

    public SyncConfigurationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SyncConfiguration?> GetByIdAsync(string id) =>
        await _context.SyncConfigurations.Include(c => c.Server).FirstOrDefaultAsync(c => c.Id == id);

    public async Task<SyncConfiguration?> GetByServerIdAsync(string serverId) =>
        await _context.SyncConfigurations.Include(c => c.Server).FirstOrDefaultAsync(c => c.ServerId == serverId);

    public async Task<IEnumerable<SyncConfiguration>> GetAllAsync() =>
        await _context.SyncConfigurations.Include(c => c.Server).ToListAsync();

    public async Task<IEnumerable<SyncConfiguration>> GetEnabledConfigurationsAsync() =>
        await _context.SyncConfigurations.Include(c => c.Server).Where(c => c.Enabled && c.Server!.IsActive).ToListAsync();

    public async Task<SyncConfiguration> AddAsync(SyncConfiguration config)
    {
        await _context.SyncConfigurations.AddAsync(config);
        await _context.SaveChangesAsync();
        return config;
    }

    public async Task UpdateAsync(SyncConfiguration config)
    {
        config.UpdatedAt = DateTime.UtcNow;
        _context.SyncConfigurations.Update(config);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var config = await _context.SyncConfigurations.FindAsync(id);
        if (config != null)
        {
            _context.SyncConfigurations.Remove(config);
            await _context.SaveChangesAsync();
        }
    }
}

public class SyncLogRepository : ISyncLogRepository
{
    private readonly ApplicationDbContext _context;

    public SyncLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SyncLog?> GetByIdAsync(string id) =>
        await _context.SyncLogs.FindAsync(id);

    public async Task<IEnumerable<SyncLog>> GetAllAsync() =>
        await _context.SyncLogs.OrderByDescending(l => l.StartedAt).ToListAsync();

    public async Task<IEnumerable<SyncLog>> GetRecentLogsAsync(int count) =>
        await _context.SyncLogs.OrderByDescending(l => l.StartedAt).Take(count).ToListAsync();

    public async Task<SyncLog> AddAsync(SyncLog log)
    {
        await _context.SyncLogs.AddAsync(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task UpdateAsync(SyncLog log)
    {
        _context.SyncLogs.Update(log);
        await _context.SaveChangesAsync();
    }
}

public class UserGroupMembershipRepository : IUserGroupMembershipRepository
{
    private readonly ApplicationDbContext _context;

    public UserGroupMembershipRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserGroupMembership?> GetByIdAsync(string id) =>
        await _context.UserGroupMemberships.FindAsync(id);

    public async Task<IEnumerable<UserGroupMembership>> GetByUserIdAsync(string userId) =>
        await _context.UserGroupMemberships.Where(m => m.UserId == userId).ToListAsync();

    public async Task<IEnumerable<UserGroupMembership>> GetByGroupIdAsync(string groupId) =>
        await _context.UserGroupMemberships.Where(m => m.GroupId == groupId).ToListAsync();

    public async Task<UserGroupMembership> AddAsync(UserGroupMembership membership)
    {
        await _context.UserGroupMemberships.AddAsync(membership);
        await _context.SaveChangesAsync();
        return membership;
    }

    public async Task UpdateAsync(UserGroupMembership membership)
    {
        _context.UserGroupMemberships.Update(membership);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var membership = await _context.UserGroupMemberships.FindAsync(id);
        if (membership != null)
        {
            _context.UserGroupMemberships.Remove(membership);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteByUserIdAsync(string userId)
    {
        var memberships = await _context.UserGroupMemberships.Where(m => m.UserId == userId).ToListAsync();
        _context.UserGroupMemberships.RemoveRange(memberships);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByGroupIdAsync(string groupId)
    {
        var memberships = await _context.UserGroupMemberships.Where(m => m.GroupId == groupId).ToListAsync();
        _context.UserGroupMemberships.RemoveRange(memberships);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string userId, string groupId) =>
        await _context.UserGroupMemberships.AnyAsync(m => m.UserId == userId && m.GroupId == groupId);
}
