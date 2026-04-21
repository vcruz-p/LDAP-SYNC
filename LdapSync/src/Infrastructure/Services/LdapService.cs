using System.DirectoryServices.Protocols;
using System.Net;
using System.Linq;
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
            
            ldap.Timeout = TimeSpan.FromSeconds(server.TimeoutSeconds);
            
            await Task.Run(() => ldap.Bind());
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> TestConnectionWithDetailsAsync(LdapServer server)
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
                try
                {
                    ldap.SessionOptions.StartTransportLayerSecurity(null);
                }
                catch (Exception ex)
                {
                    return (false, $"Error al iniciar TLS: {ex.Message}");
                }
            }
            
            ldap.Timeout = TimeSpan.FromSeconds(server.TimeoutSeconds);
            
            await Task.Run(() => ldap.Bind());
            return (true, null);
        }
        catch (LdapException ex)
        {
            return (false, $"Error LDAP: {ex.Message} (Código: {ex.ErrorCode})");
        }
        catch (TimeoutException ex)
        {
            return (false, $"Tiempo de espera agotado: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return (false, $"Acceso no autorizado: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Error de conexión: {ex.Message}");
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
            
            ldap.Timeout = TimeSpan.FromSeconds(server.TimeoutSeconds);
            
            await Task.Run(() => ldap.Bind());

            var searchBase = config.SearchBase ?? server.BaseDn;
            var searchFilter = server.UserSearchFilter ?? "(objectClass=person)";
            
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
            
            ldap.Timeout = TimeSpan.FromSeconds(server.TimeoutSeconds);
            
            await Task.Run(() => ldap.Bind());

            var searchBase = config.SearchBase ?? server.BaseDn;
            var searchFilter = server.GroupSearchFilter ?? "(objectClass=groupOfNames)";
            
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

    public async Task<(SyncConfiguration Config, IEnumerable<string> Ous)> ExtractPoliciesAndOusFromLdapAsync(LdapServer server)
    {
        var ous = new HashSet<string>();
        var config = new SyncConfiguration
        {
            ServerId = server.Id,
            Enabled = false,
            CronSchedule = "0 2 * * *",
            SyncMode = "full",
            SyncUsers = true,
            SyncGroups = true,
            SyncMemberships = true,
            SyncPasswords = false,
            SyncPasswordPolicies = false,
            ForcePasswordResetDays = null,
            DeactivateOrphanUsers = true,
            DeleteOrphanGroups = false,
            PageSize = 500,
            MaxEntries = 0,
            SearchBase = server.BaseDn,
            ExcludedAttributes = null
        };

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
            
            ldap.Timeout = TimeSpan.FromSeconds(server.TimeoutSeconds);
            
            await Task.Run(() => ldap.Bind());

            // Extraer todas las Unidades Organizativas (OUs)
            var ouSearchRequest = new SearchRequest(
                server.BaseDn,
                "(objectClass=organizationalUnit)",
                SearchScope.Subtree,
                "ou", "distinguishedName"
            );
            
            var ouSearchResponse = await Task.Run(() => (SearchResponse)ldap.SendRequest(ouSearchRequest));
            
            foreach (SearchResultEntry entry in ouSearchResponse.Entries)
            {
                if (entry.Attributes.Contains("ou") && entry.Attributes["ou"].Count > 0)
                {
                    var ouName = entry.Attributes["ou"][0]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(ouName))
                    {
                        ous.Add(entry.DistinguishedName);
                    }
                }
            }

            // Extraer políticas de seguridad desde el servidor LDAP
            // Buscar entradas de pwdPolicy o similar
            var policySearchRequest = new SearchRequest(
                server.BaseDn,
                "(|(objectClass=pwdPolicy)(objectClass=pwdPolicyElement)(objectClass=device))",
                SearchScope.Subtree,
                "pwdAttribute", "pwdMinLength", "pwdCheckQuality", "pwdInHistory", 
                "pwdMaxAge", "pwdExpireWarning", "pwdFailureCountInterval", 
                "pwdLockout", "pwdLockoutDuration", "passwordGraceLimit"
            );
            
            try
            {
                var policySearchResponse = await Task.Run(() => (SearchResponse)ldap.SendRequest(policySearchRequest));
                
                int? minLength = null;
                int? historyCount = null;
                int? maxAgeDays = null;
                int? warningDays = null;
                bool lockoutEnabled = false;
                
                foreach (SearchResultEntry entry in policySearchResponse.Entries)
                {
                    // Intentar extraer políticas de contraseña
                    if (entry.Attributes.Contains("pwdMinLength") && entry.Attributes["pwdMinLength"].Count > 0)
                    {
                        if (int.TryParse(entry.Attributes["pwdMinLength"][0]?.ToString(), out var minLen))
                        {
                            minLength = minLen;
                        }
                    }
                    
                    if (entry.Attributes.Contains("pwdInHistory") && entry.Attributes["pwdInHistory"].Count > 0)
                    {
                        if (int.TryParse(entry.Attributes["pwdInHistory"][0]?.ToString(), out var hist))
                        {
                            historyCount = hist;
                        }
                    }
                    
                    if (entry.Attributes.Contains("pwdMaxAge") && entry.Attributes["pwdMaxAge"].Count > 0)
                    {
                        if (int.TryParse(entry.Attributes["pwdMaxAge"][0]?.ToString(), out var maxAge))
                        {
                            // Convertir segundos a días
                            maxAgeDays = maxAge / 86400;
                        }
                    }
                    
                    if (entry.Attributes.Contains("pwdExpireWarning") && entry.Attributes["pwdExpireWarning"].Count > 0)
                    {
                        if (int.TryParse(entry.Attributes["pwdExpireWarning"][0]?.ToString(), out var warn))
                        {
                            warningDays = warn / 86400;
                        }
                    }
                    
                    if (entry.Attributes.Contains("pwdLockout") && entry.Attributes["pwdLockout"].Count > 0)
                    {
                        lockoutEnabled = entry.Attributes["pwdLockout"][0]?.ToString()?.ToLower() == "true";
                    }
                }

                // Aplicar políticas extraídas a la configuración
                if (historyCount.HasValue && historyCount.Value > 0)
                {
                    config.SyncPasswordPolicies = true;
                    // Se podría almacenar historyCount en un campo adicional si fuera necesario
                }
                
                if (maxAgeDays.HasValue && maxAgeDays.Value > 0)
                {
                    config.ForcePasswordResetDays = maxAgeDays.Value;
                }
            }
            catch (LdapException)
            {
                // Si no hay políticas de contraseña, continuar sin ellas
            }

            // Extraer información de usuarios para detectar atributos de políticas
            var userSearchRequest = new SearchRequest(
                server.BaseDn,
                "(objectClass=person)",
                SearchScope.Subtree,
                "pwdChangedTime", "pwdAccountLockedTime", "pwdFailureTime", 
                "loginGraceLimit", "loginGraceRemaining"
            );
            
            try
            {
                var userSearchResponse = await Task.Run(() => (SearchResponse)ldap.SendRequest(userSearchRequest));
                
                bool hasPasswordChangeTime = false;
                bool hasLockoutInfo = false;
                
                foreach (SearchResultEntry entry in userSearchResponse.Entries)
                {
                    if (!hasPasswordChangeTime && entry.Attributes.Contains("pwdChangedTime"))
                    {
                        hasPasswordChangeTime = true;
                    }
                    
                    if (!hasLockoutInfo && entry.Attributes.Contains("pwdAccountLockedTime"))
                    {
                        hasLockoutInfo = true;
                    }
                    
                    if (hasPasswordChangeTime && hasLockoutInfo)
                        break;
                }
                
                if (hasPasswordChangeTime || hasLockoutInfo)
                {
                    config.SyncPasswordPolicies = true;
                }
            }
            catch (LdapException)
            {
                // Continuar sin información de políticas de usuario
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al extraer políticas y OUs del servidor LDAP: {ex.Message}", ex);
        }

        return (config, ous);
    }
}
