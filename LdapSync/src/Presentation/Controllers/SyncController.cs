using LdapSync.Application.DTOs;
using LdapSync.Domain.Entities;
using LdapSync.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LdapSync.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly ILdapServerRepository _serverRepository;
    private readonly ILdapService _ldapService;
    private readonly ILdapUserRepository _userRepository;
    private readonly ILdapGroupRepository _groupRepository;
    
    public SyncController(
        ILdapServerRepository serverRepository,
        ILdapService ldapService,
        ILdapUserRepository userRepository,
        ILdapGroupRepository groupRepository)
    {
        _serverRepository = serverRepository;
        _ldapService = ldapService;
        _userRepository = userRepository;
        _groupRepository = groupRepository;
    }
    
    /// <summary>
    /// Sincroniza todos los servidores LDAP activos extrayendo usuarios y unidades organizativas
    /// </summary>
    [HttpPost("sync-all")]
    public async Task<ActionResult<SyncAllResponseDto>> SyncAllActiveServers()
    {
        try
        {
            var activeServers = await _serverRepository.GetActiveServersAsync();
            var serverList = activeServers.ToList();
            
            if (!serverList.Any())
            {
                return Ok(new SyncAllResponseDto
                {
                    Success = true,
                    Message = "No hay servidores LDAP activos para sincronizar",
                    TotalServers = 0,
                    SuccessfulSyncs = 0,
                    FailedSyncs = 0,
                    Results = new List<SyncResultDto>()
                });
            }

            var results = new List<SyncResultDto>();
            int successfulSyncs = 0;
            int failedSyncs = 0;

            foreach (var server in serverList)
            {
                try
                {
                    // Extraer políticas y OUs del servidor LDAP
                    var (config, ous) = await _ldapService.ExtractPoliciesAndOusFromLdapAsync(server);
                    
                    // Sincronizar usuarios desde el servidor LDAP
                    var users = await _ldapService.SyncUsersAsync(server, config);
                    
                    // Sincronizar grupos desde el servidor LDAP
                    var groups = await _ldapService.SyncGroupsAsync(server, config);
                    
                    // Guardar usuarios en la base de datos
                    var savedUsers = new List<LdapUser>();
                    foreach (var user in users)
                    {
                        var existingUser = await _userRepository.GetByDistinguishedNameAsync(user.DistinguishedName);
                        if (existingUser != null)
                        {
                            user.Id = existingUser.Id;
                            await _userRepository.UpdateAsync(user);
                            savedUsers.Add(user);
                        }
                        else
                        {
                            var newUser = await _userRepository.AddAsync(user);
                            savedUsers.Add(newUser);
                        }
                    }
                    
                    // Guardar grupos en la base de datos
                    var savedGroups = new List<LdapGroup>();
                    foreach (var group in groups)
                    {
                        var existingGroup = await _groupRepository.GetByDistinguishedNameAsync(group.DistinguishedName);
                        if (existingGroup != null)
                        {
                            group.Id = existingGroup.Id;
                            await _groupRepository.UpdateAsync(group);
                            savedGroups.Add(group);
                        }
                        else
                        {
                            var newGroup = await _groupRepository.AddAsync(group);
                            savedGroups.Add(newGroup);
                        }
                    }

                    successfulSyncs++;
                    results.Add(new SyncResultDto
                    {
                        ConfigurationId = string.Empty,
                        ServerId = server.Id,
                        ServerName = server.Name,
                        Success = true,
                        Message = $"Sincronización completada: {savedUsers.Count} usuarios, {savedGroups.Count} grupos, {ous.Count()} OUs",
                        UsersProcessed = savedUsers.Count,
                        GroupsProcessed = savedGroups.Count,
                        MembershipsProcessed = 0,
                        ErrorsCount = 0
                    });
                }
                catch (Exception ex)
                {
                    failedSyncs++;
                    results.Add(new SyncResultDto
                    {
                        ConfigurationId = string.Empty,
                        ServerId = server.Id,
                        ServerName = server.Name,
                        Success = false,
                        Message = $"Error durante la sincronización: {ex.Message}",
                        UsersProcessed = 0,
                        GroupsProcessed = 0,
                        MembershipsProcessed = 0,
                        ErrorsCount = 1
                    });
                }
            }

            return Ok(new SyncAllResponseDto
            {
                Success = true,
                Message = $"Sincronización completada: {successfulSyncs} exitosas, {failedSyncs} fallidas",
                TotalServers = serverList.Count,
                SuccessfulSyncs = successfulSyncs,
                FailedSyncs = failedSyncs,
                Results = results
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new SyncAllResponseDto
            {
                Success = false,
                Message = $"Error general durante la sincronización: {ex.Message}",
                TotalServers = 0,
                SuccessfulSyncs = 0,
                FailedSyncs = 0,
                Results = new List<SyncResultDto>()
            });
        }
    }
}
