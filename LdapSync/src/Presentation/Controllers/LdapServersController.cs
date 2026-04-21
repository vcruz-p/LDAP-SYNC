using LdapSync.Application.DTOs;
using LdapSync.Domain.Entities;
using LdapSync.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LdapSync.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LdapServersController : ControllerBase
{
    private readonly ILdapServerRepository _serverRepository;
    private readonly ILdapService _ldapService;

    public LdapServersController(
        ILdapServerRepository serverRepository,
        ILdapService ldapService)
    {
        _serverRepository = serverRepository;
        _ldapService = ldapService;
    }

    /// <summary>
    /// Obtiene todos los servidores LDAP registrados
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LdapServerDto>>> GetAllServers()
    {
        var servers = await _serverRepository.GetAllAsync();
        var serverDtos = servers.Select(s => new LdapServerDto
        {
            Id = s.Id,
            Name = s.Name,
            Host = s.Host,
            Port = s.Port,
            BaseDn = s.BaseDn,
            BindDn = s.BindDn,
            UseTls = s.UseTls,
            ValidateCertificate = s.ValidateCertificate,
            TimeoutSeconds = s.TimeoutSeconds,
            UserSearchFilter = s.UserSearchFilter,
            GroupSearchFilter = s.GroupSearchFilter,
            IsActive = s.IsActive,
            ServerType = s.ServerType,
            Description = s.Description,
            LastConnectionTest = s.LastConnectionTest,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        });

        return Ok(serverDtos);
    }

    /// <summary>
    /// Obtiene un servidor LDAP específico por su ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<LdapServerDto>> GetServerById(string id)
    {
        var server = await _serverRepository.GetByIdAsync(id);
        if (server == null)
            return NotFound(new { message = "Servidor LDAP no encontrado" });

        return Ok(new LdapServerDto
        {
            Id = server.Id,
            Name = server.Name,
            Host = server.Host,
            Port = server.Port,
            BaseDn = server.BaseDn,
            BindDn = server.BindDn,
            UseTls = server.UseTls,
            ValidateCertificate = server.ValidateCertificate,
            TimeoutSeconds = server.TimeoutSeconds,
            UserSearchFilter = server.UserSearchFilter,
            GroupSearchFilter = server.GroupSearchFilter,
            IsActive = server.IsActive,
            ServerType = server.ServerType,
            Description = server.Description,
            LastConnectionTest = server.LastConnectionTest,
            CreatedAt = server.CreatedAt,
            UpdatedAt = server.UpdatedAt
        });
    }

    /// <summary>
    /// Obtiene solo los servidores LDAP activos
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<LdapServerDto>>> GetActiveServers()
    {
        var servers = await _serverRepository.GetActiveServersAsync();
        var serverDtos = servers.Select(s => new LdapServerDto
        {
            Id = s.Id,
            Name = s.Name,
            Host = s.Host,
            Port = s.Port,
            BaseDn = s.BaseDn,
            BindDn = s.BindDn,
            UseTls = s.UseTls,
            ValidateCertificate = s.ValidateCertificate,
            TimeoutSeconds = s.TimeoutSeconds,
            UserSearchFilter = s.UserSearchFilter,
            GroupSearchFilter = s.GroupSearchFilter,
            IsActive = s.IsActive,
            ServerType = s.ServerType,
            Description = s.Description,
            LastConnectionTest = s.LastConnectionTest,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        });

        return Ok(serverDtos);
    }

    /// <summary>
    /// Crea un nuevo servidor LDAP
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LdapServerDto>> CreateServer(
        [FromForm] string name,
        [FromForm] string host,
        [FromForm] string baseDn,
        [FromForm] string bindDn,
        [FromForm] int port = 389,
        [FromForm] string? bindPassword = null,
        [FromForm] bool useTls = false,
        [FromForm] bool validateCertificate = true,
        [FromForm] int timeoutSeconds = 30,
        [FromForm] string? userSearchFilter = null,
        [FromForm] string? groupSearchFilter = null,
        [FromForm] string? serverType = null,
        [FromForm] string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "El nombre del servidor es requerido" });

        if (string.IsNullOrWhiteSpace(host))
            return BadRequest(new { message = "El host del servidor es requerido" });

        if (string.IsNullOrWhiteSpace(baseDn))
            return BadRequest(new { message = "El DN base es requerido" });

        if (string.IsNullOrWhiteSpace(bindDn))
            return BadRequest(new { message = "El DN de enlace es requerido" });

        var server = new LdapServer
        {
            Name = name,
            Host = host,
            Port = port,
            BaseDn = baseDn,
            BindDn = bindDn,
            BindPassword = bindPassword ?? string.Empty,
            UseTls = useTls,
            ValidateCertificate = validateCertificate,
            TimeoutSeconds = timeoutSeconds,
            UserSearchFilter = userSearchFilter ?? "(objectClass=person)",
            GroupSearchFilter = groupSearchFilter ?? "(objectClass=groupOfNames)",
            ServerType = serverType,
            Description = description,
            IsActive = true
        };

        var createdServer = await _serverRepository.AddAsync(server);

        var response = new LdapServerDto
        {
            Id = createdServer.Id,
            Name = createdServer.Name,
            Host = createdServer.Host,
            Port = createdServer.Port,
            BaseDn = createdServer.BaseDn,
            BindDn = createdServer.BindDn,
            UseTls = createdServer.UseTls,
            ValidateCertificate = createdServer.ValidateCertificate,
            TimeoutSeconds = createdServer.TimeoutSeconds,
            UserSearchFilter = createdServer.UserSearchFilter,
            GroupSearchFilter = createdServer.GroupSearchFilter,
            IsActive = createdServer.IsActive,
            ServerType = createdServer.ServerType,
            Description = createdServer.Description,
            LastConnectionTest = createdServer.LastConnectionTest,
            CreatedAt = createdServer.CreatedAt,
            UpdatedAt = createdServer.UpdatedAt
        };

        return CreatedAtAction(nameof(GetServerById), new { id = createdServer.Id }, response);
    }

    /// <summary>
    /// Actualiza un servidor LDAP existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<LdapServerDto>> UpdateServer(string id, [FromBody] UpdateLdapServerDto dto)
    {
        var server = await _serverRepository.GetByIdAsync(id);
        if (server == null)
            return NotFound(new { message = "Servidor LDAP no encontrado" });

        // Actualizar solo los campos proporcionados
        if (dto.Name != null)
            server.Name = dto.Name;

        if (dto.Host != null)
            server.Host = dto.Host;

        if (dto.Port.HasValue)
            server.Port = dto.Port.Value;

        if (dto.BaseDn != null)
            server.BaseDn = dto.BaseDn;

        if (dto.BindDn != null)
            server.BindDn = dto.BindDn;

        if (dto.BindPassword != null)
            server.BindPassword = dto.BindPassword;

        if (dto.UseTls.HasValue)
            server.UseTls = dto.UseTls.Value;

        if (dto.ValidateCertificate.HasValue)
            server.ValidateCertificate = dto.ValidateCertificate.Value;

        if (dto.TimeoutSeconds.HasValue)
            server.TimeoutSeconds = dto.TimeoutSeconds.Value;

        if (dto.UserSearchFilter != null)
            server.UserSearchFilter = dto.UserSearchFilter;

        if (dto.GroupSearchFilter != null)
            server.GroupSearchFilter = dto.GroupSearchFilter;

        if (dto.IsActive.HasValue)
            server.IsActive = dto.IsActive.Value;

        if (dto.ServerType != null)
            server.ServerType = dto.ServerType;

        if (dto.Description != null)
            server.Description = dto.Description;

        await _serverRepository.UpdateAsync(server);

        var response = new LdapServerDto
        {
            Id = server.Id,
            Name = server.Name,
            Host = server.Host,
            Port = server.Port,
            BaseDn = server.BaseDn,
            BindDn = server.BindDn,
            UseTls = server.UseTls,
            ValidateCertificate = server.ValidateCertificate,
            TimeoutSeconds = server.TimeoutSeconds,
            UserSearchFilter = server.UserSearchFilter,
            GroupSearchFilter = server.GroupSearchFilter,
            IsActive = server.IsActive,
            ServerType = server.ServerType,
            Description = server.Description,
            LastConnectionTest = server.LastConnectionTest,
            CreatedAt = server.CreatedAt,
            UpdatedAt = server.UpdatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Elimina un servidor LDAP por su ID
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteServer(string id)
    {
        var server = await _serverRepository.GetByIdAsync(id);
        if (server == null)
            return NotFound(new { message = "Servidor LDAP no encontrado" });

        await _serverRepository.DeleteAsync(id);

        return NoContent();
    }

    /// <summary>
    /// Prueba la conexión con un servidor LDAP
    /// </summary>
    [HttpPost("{id}/test-connection")]
    public async Task<ActionResult> TestConnection(string id)
    {
        var server = await _serverRepository.GetByIdAsync(id);
        if (server == null)
            return NotFound(new { message = "Servidor LDAP no encontrado" });

        try
        {
            var result = await _ldapService.TestConnectionWithDetailsAsync(server);
            
            server.LastConnectionTest = DateTime.UtcNow;
            await _serverRepository.UpdateAsync(server);

            return Ok(new 
            { 
                success = result.Success, 
                message = result.Success ? "Conexión exitosa" : result.ErrorMessage,
                lastTest = server.LastConnectionTest
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                success = false, 
                message = $"Error al probar la conexión: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Prueba la conexión con todos los servidores LDAP activos y reporta errores detallados
    /// </summary>
    [HttpPost("test-all-connections")]
    public async Task<ActionResult> TestAllActiveConnections()
    {
        var servers = await _serverRepository.GetActiveServersAsync();
        var results = new List<object>();

        foreach (var server in servers)
        {
            try
            {
                var result = await _ldapService.TestConnectionWithDetailsAsync(server);
                
                server.LastConnectionTest = DateTime.UtcNow;
                await _serverRepository.UpdateAsync(server);

                results.Add(new 
                { 
                    serverId = server.Id,
                    serverName = server.Name,
                    host = server.Host,
                    port = server.Port,
                    success = result.Success, 
                    message = result.Success ? "Conexión exitosa" : result.ErrorMessage,
                    lastTest = server.LastConnectionTest
                });
            }
            catch (Exception ex)
            {
                results.Add(new 
                { 
                    serverId = server.Id,
                    serverName = server.Name,
                    host = server.Host,
                    port = server.Port,
                    success = false, 
                    message = $"Error excepcional: {ex.Message}"
                });
            }
        }

        var totalServers = results.Count;
        var successfulConnections = results.Count(r => Convert.ToBoolean(((dynamic)r).success));
        var failedConnections = totalServers - successfulConnections;

        return Ok(new 
        { 
            summary = new
            {
                totalServers = totalServers,
                successfulConnections = successfulConnections,
                failedConnections = failedConnections
            },
            details = results
        });
    }
}
