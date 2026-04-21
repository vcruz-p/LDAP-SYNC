using System.DirectoryServices.Protocols;
using System.Net;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using LdapSync.Domain.Entities;
using LdapSync.Domain.Interfaces;

namespace LdapSync.Infrastructure.Services;

public class LdapService : ILdapService
{
    public async Task<bool> TestConnectionAsync(LdapServer server)
    {
        try
        {
            var identifier = new LdapDirectoryIdentifier(server.Host, server.Port);
            var credential = new NetworkCredential(server.BindDn, server.BindPassword);
            
            using var ldap = new LdapConnection(identifier, credential);
            ldap.SessionOptions.ProtocolVersion = 3;
            ldap.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
            
            if (server.UseTls)
            {
                ldap.SessionOptions.StartTransportLayerSecurity(null);
            }
            
            if (!server.ValidateCertificate)
            {
                ldap.SessionOptions.ServerCertificateValidationCallback += (sender, cert) => true;
            }
            
            ldap.Timeout = TimeSpan.FromSeconds(server.TimeoutSeconds);
            
            await Task.Run(() => ldap.Bind());
            return true;
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
            var identifier = new LdapDirectoryIdentifier(server.Host, server.Port);
            var credential = new NetworkCredential(server.BindDn, server.BindPassword);
            
            using var ldap = new LdapConnection(identifier, credential);
            ldap.SessionOptions.ProtocolVersion = 3;
            ldap.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
            
            if (server.UseTls)
            {
                ldap.SessionOptions.StartTransportLayerSecurity(null);
            }
            
            if (!server.ValidateCertificate)
            {
                ldap.SessionOptions.ServerCertificateValidationCallback += (sender, cert) => true;
            }
            
            ldap.Timeout = TimeSpan.FromSeconds(server.TimeoutSeconds);
            
            await Task.Run(() => ldap.Bind());

            var searchBase = config.SearchBase ?? server.BaseDn;
            var searchFilter = server.UserSearchFilter ?? "(&(objectClass=inetOrgPerson)(uid=*))";
            
            var searchRequest = new SearchRequest(
                searchBase,
                searchFilter,
                SearchScope.Subtree,
                null
            );
            
            searchRequest.SizeLimit = config.MaxEntries > 0 ? config.MaxEntries : 10000;
            
            var searchResponse = await Task.Run(() => (SearchResponse)ldap.SendRequest(searchRequest));
            
            foreach (SearchResultEntry entry in searchResponse.Entries)
            {
                var user = MapLdapEntryToUser(entry, config);
                users.Add(user);
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
            var identifier = new LdapDirectoryIdentifier(server.Host, server.Port);
            var credential = new NetworkCredential(server.BindDn, server.BindPassword);
            
            using var ldap = new LdapConnection(identifier, credential);
            ldap.SessionOptions.ProtocolVersion = 3;
            ldap.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
            
            if (server.UseTls)
            {
                ldap.SessionOptions.StartTransportLayerSecurity(null);
            }
            
            if (!server.ValidateCertificate)
            {
                ldap.SessionOptions.ServerCertificateValidationCallback += (sender, cert) => true;
            }
            
            ldap.Timeout = TimeSpan.FromSeconds(server.TimeoutSeconds);
            
            await Task.Run(() => ldap.Bind());

            var searchBase = config.SearchBase ?? server.BaseDn;
            var searchFilter = server.GroupSearchFilter ?? "(|(objectClass=groupOfNames)(objectClass=posixGroup)(objectClass=groupOfUniqueNames))";
            
            var searchRequest = new SearchRequest(
                searchBase,
                searchFilter,
                SearchScope.Subtree,
                null
            );
            
            searchRequest.SizeLimit = config.MaxEntries > 0 ? config.MaxEntries : 10000;
            
            var searchResponse = await Task.Run(() => (SearchResponse)ldap.SendRequest(searchRequest));
            
            foreach (SearchResultEntry entry in searchResponse.Entries)
            {
                var group = MapLdapEntryToGroup(entry, config);
                groups.Add(group);
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

    private static LdapUser MapLdapEntryToUser(SearchResultEntry entry, SyncConfiguration config)
    {
        string GetAttributeValue(string name)
        {
            if (entry.Attributes.Contains(name) && entry.Attributes[name].Count > 0)
            {
                return entry.Attributes[name][0]?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }
        
        int? GetIntAttributeValue(string name)
        {
            var value = GetAttributeValue(name);
            if (!string.IsNullOrEmpty(value) && int.TryParse(value, out var result))
            {
                return result;
            }
            return null;
        }
        
        return new LdapUser
        {
            Uid = GetAttributeValue("uid"),
            CommonName = GetAttributeValue("cn"),
            DisplayName = GetAttributeValue("displayName") != string.Empty ? GetAttributeValue("displayName") : GetAttributeValue("cn"),
            Email = GetAttributeValue("mail"),
            FirstName = GetAttributeValue("givenName"),
            LastName = GetAttributeValue("sn"),
            TelephoneNumber = GetAttributeValue("telephoneNumber"),
            Ou = GetAttributeValue("ou"),
            Organization = GetAttributeValue("o"),
            GidNumber = GetIntAttributeValue("gidNumber"),
            UidNumber = GetIntAttributeValue("uidNumber"),
            LoginShell = GetAttributeValue("loginShell"),
            HomeDirectory = GetAttributeValue("homeDirectory"),
            DistinguishedName = entry.DistinguishedName,
            IsActive = true,
            SyncedAt = DateTime.UtcNow
        };
    }

    private static LdapGroup MapLdapEntryToGroup(SearchResultEntry entry, SyncConfiguration config)
    {
        string GetAttributeValue(string name)
        {
            if (entry.Attributes.Contains(name) && entry.Attributes[name].Count > 0)
            {
                return entry.Attributes[name][0]?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }
        
        int? GetIntAttributeValue(string name)
        {
            var value = GetAttributeValue(name);
            if (!string.IsNullOrEmpty(value) && int.TryParse(value, out var result))
            {
                return result;
            }
            return null;
        }
        
        var objectClass = entry.Attributes.Contains("objectClass") 
            ? string.Join(",", entry.Attributes["objectClass"].GetValues(typeof(string))) 
            : string.Empty;
        
        return new LdapGroup
        {
            CommonName = GetAttributeValue("cn"),
            Description = GetAttributeValue("description"),
            GidNumber = GetIntAttributeValue("gidNumber"),
            DistinguishedName = entry.DistinguishedName,
            ObjectClass = objectClass,
            IsActive = true,
            SyncedAt = DateTime.UtcNow
        };
    }
}
