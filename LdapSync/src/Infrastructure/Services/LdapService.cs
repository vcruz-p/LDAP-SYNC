using LdapSync.Domain.Entities;
using LdapSync.Domain.Interfaces;
using Novell.Directory.Ldap;

namespace LdapSync.Infrastructure.Services;

public class LdapService : ILdapService
{
    public async Task<bool> TestConnectionAsync(LdapServer server)
    {
        try
        {
            using var ldap = new LdapConnection();
            ldap.Connect(server.Host, server.Port);
            
            if (server.UseTls)
            {
                ldap.StartTls();
            }

            ldap.Bind(server.BindDn, server.BindPassword);
            return ldap.Connected;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<LdapUser>> SyncUsersAsync(LdapServer server, SyncConfiguration config)
    {
        var users = new List<LdapUser>();
        
        try
        {
            using var ldap = new LdapConnection();
            ldap.Connect(server.Host, server.Port);
            
            if (server.UseTls)
            {
                ldap.StartTls();
            }

            ldap.Bind(server.BindDn, server.BindPassword);

            var searchBase = config.SearchBase ?? server.BaseDn;
            var searchFilter = server.UserSearchFilter;
            
            var searchConstraints = new LdapSearchConstraints
            {
                MaxResults = config.MaxEntries > 0 ? config.MaxEntries : 10000,
                BatchSize = config.PageSize
            };

            var results = ldap.Search(searchBase, LdapConnection.ScopeSub, searchFilter, null, false, searchConstraints);
            
            while (results.HasMore())
            {
                var entry = results.Next();
                if (entry != null)
                {
                    var user = MapLdapEntryToUser(entry, config);
                    users.Add(user);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error syncing users: {ex.Message}", ex);
        }

        return users;
    }

    public async Task<IEnumerable<LdapGroup>> SyncGroupsAsync(LdapServer server, SyncConfiguration config)
    {
        var groups = new List<LdapGroup>();
        
        try
        {
            using var ldap = new LdapConnection();
            ldap.Connect(server.Host, server.Port);
            
            if (server.UseTls)
            {
                ldap.StartTls();
            }

            ldap.Bind(server.BindDn, server.BindPassword);

            var searchBase = config.SearchBase ?? server.BaseDn;
            var searchFilter = server.GroupSearchFilter;
            
            var searchConstraints = new LdapSearchConstraints
            {
                MaxResults = config.MaxEntries > 0 ? config.MaxEntries : 10000,
                BatchSize = config.PageSize
            };

            var results = ldap.Search(searchBase, LdapConnection.ScopeSub, searchFilter, null, false, searchConstraints);
            
            while (results.HasMore())
            {
                var entry = results.Next();
                if (entry != null)
                {
                    var group = MapLdapEntryToGroup(entry, config);
                    groups.Add(group);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error syncing groups: {ex.Message}", ex);
        }

        return groups;
    }

    public async Task<IEnumerable<UserGroupMembership>> SyncMembershipsAsync(LdapServer server, SyncConfiguration config)
    {
        var memberships = new List<UserGroupMembership>();
        
        // This would typically involve querying LDAP for group members
        // and creating membership records. Implementation depends on LDAP schema.
        
        return await Task.FromResult(memberships.AsEnumerable());
    }

    private static LdapUser MapLdapEntryToUser(LdapEntry entry, SyncConfiguration config)
    {
        var attr = entry.Attribute;
        
        return new LdapUser
        {
            Uid = GetAttributeValue(attr, "uid"),
            CommonName = GetAttributeValue(attr, "cn"),
            DisplayName = GetAttributeValue(attr, "displayName") ?? GetAttributeValue(attr, "cn")!,
            Email = GetAttributeValue(attr, "mail"),
            FirstName = GetAttributeValue(attr, "givenName") ?? string.Empty,
            LastName = GetAttributeValue(attr, "sn") ?? string.Empty,
            TelephoneNumber = GetAttributeValue(attr, "telephoneNumber"),
            Ou = GetAttributeValue(attr, "ou"),
            Organization = GetAttributeValue(attr, "o"),
            GidNumber = GetIntAttributeValue(attr, "gidNumber"),
            UidNumber = GetIntAttributeValue(attr, "uidNumber"),
            LoginShell = GetAttributeValue(attr, "loginShell"),
            HomeDirectory = GetAttributeValue(attr, "homeDirectory"),
            DistinguishedName = entry.Dn,
            IsActive = true,
            SyncedAt = DateTime.UtcNow
        };
    }

    private static LdapGroup MapLdapEntryToGroup(LdapEntry entry, SyncConfiguration config)
    {
        var attr = entry.Attribute;
        
        return new LdapGroup
        {
            CommonName = GetAttributeValue(attr, "cn"),
            Description = GetAttributeValue(attr, "description"),
            GidNumber = GetIntAttributeValue(attr, "gidNumber"),
            DistinguishedName = entry.Dn,
            ObjectClass = GetAttributeValue(attr, "objectClass"),
            IsActive = true,
            SyncedAt = DateTime.UtcNow
        };
    }

    private static string GetAttributeValue(LdapAttributeSet attributes, string name)
    {
        var attribute = attributes.GetAttribute(name);
        return attribute?.StringValue ?? string.Empty;
    }

    private static int? GetIntAttributeValue(LdapAttributeSet attributes, string name)
    {
        var value = GetAttributeValue(attributes, name);
        if (!string.IsNullOrEmpty(value) && int.TryParse(value, out var result))
        {
            return result;
        }
        return null;
    }
}
