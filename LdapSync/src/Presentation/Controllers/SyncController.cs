using LdapSync.Application.DTOs;
using LdapSync.Domain.Entities;
using LdapSync.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LdapSync.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly ISyncConfigurationRepository _configRepository;
    private readonly ISyncLogRepository _syncLogRepository;
    private readonly ILdapServerRepository _serverRepository;
    private readonly ILdapService _ldapService;

    public SyncController(
        ISyncService syncService,
        ISyncConfigurationRepository configRepository,
        ISyncLogRepository syncLogRepository,
        ILdapServerRepository serverRepository,
        ILdapService ldapService)
    {
        _syncService = syncService;
        _configRepository = configRepository;
        _syncLogRepository = syncLogRepository;
        _serverRepository = serverRepository;
        _ldapService = ldapService;
    }

    /// <summary>
    /// Ejecuta una sincronización completa o incremental con un servidor LDAP
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<SyncResponseDto>> ExecuteSync([FromBody] SyncRequestDto request)
    {
        try
        {
            var config = await _configRepository.GetByIdAsync(request.ConfigurationId);
            if (config == null)
                return BadRequest(new SyncResponseDto 
                { 
                    Success = false, 
                    Message = "Configuración de sincronización no encontrada" 
                });

            SyncLog log;
            if (request.SyncMode.ToLower() == "incremental")
            {
                log = await _syncService.ExecuteIncrementalSyncAsync(request.ConfigurationId, request.IsDryRun);
            }
            else
            {
                log = await _syncService.ExecuteFullSyncAsync(request.ConfigurationId, request.IsDryRun);
            }

            return Ok(new SyncResponseDto
            {
                Success = true,
                Message = request.IsDryRun ? "Sincronización de prueba completada" : "Sincronización completada",
                Log = new SyncLogDto
                {
                    Id = log.Id,
                    StartedAt = log.StartedAt,
                    CompletedAt = log.CompletedAt,
                    Status = log.Status,
                    UsersProcessed = log.UsersProcessed,
                    GroupsProcessed = log.GroupsProcessed,
                    MembershipsProcessed = log.MembershipsProcessed,
                    ErrorsCount = log.ErrorsCount,
                    ErrorMessage = log.ErrorMessage,
                    IsDryRun = log.IsDryRun
                },
                UsersProcessed = log.UsersProcessed,
                GroupsProcessed = log.GroupsProcessed,
                MembershipsProcessed = log.MembershipsProcessed,
                ErrorsCount = log.ErrorsCount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new SyncResponseDto
            {
                Success = false,
                Message = $"Error durante la sincronización: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Ejecuta sincronización para todos los servidores activos con configuraciones habilitadas
    /// </summary>
    [HttpPost("execute-all-active")]
    public async Task<ActionResult<SyncAllResponseDto>> ExecuteSyncForAllActiveServers([FromQuery] bool isDryRun = false)
    {
        try
        {
            var enabledConfigs = await _configRepository.GetEnabledConfigurationsAsync();
            var configList = enabledConfigs.ToList();
            
            if (!configList.Any())
            {
                return Ok(new SyncAllResponseDto
                {
                    Success = true,
                    Message = "No hay configuraciones habilitadas para sincronizar",
                    TotalServers = 0,
                    SuccessfulSyncs = 0,
                    FailedSyncs = 0,
                    Results = new List<SyncResultDto>()
                });
            }

            var results = new List<SyncResultDto>();
            int successfulSyncs = 0;
            int failedSyncs = 0;

            foreach (var config in configList)
            {
                try
                {
                    SyncLog log;
                    if (config.SyncMode.ToLower() == "incremental")
                    {
                        log = await _syncService.ExecuteIncrementalSyncAsync(config.Id, isDryRun);
                    }
                    else
                    {
                        log = await _syncService.ExecuteFullSyncAsync(config.Id, isDryRun);
                    }

                    successfulSyncs++;
                    results.Add(new SyncResultDto
                    {
                        ConfigurationId = config.Id,
                        ServerId = config.ServerId,
                        ServerName = config.Server?.Name ?? "Desconocido",
                        Success = true,
                        Message = isDryRun ? "Sincronización de prueba completada" : "Sincronización completada",
                        UsersProcessed = log.UsersProcessed,
                        GroupsProcessed = log.GroupsProcessed,
                        MembershipsProcessed = log.MembershipsProcessed,
                        ErrorsCount = log.ErrorsCount
                    });
                }
                catch (Exception ex)
                {
                    failedSyncs++;
                    results.Add(new SyncResultDto
                    {
                        ConfigurationId = config.Id,
                        ServerId = config.ServerId,
                        ServerName = config.Server?.Name ?? "Desconocido",
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
                TotalServers = configList.Count,
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

    /// <summary>
    /// Crea una configuración de sincronización poblada con las políticas del servidor
    /// </summary>
    [HttpPost("configurations/auto-from-server/{serverId}")]
    public async Task<ActionResult<SyncConfigurationDto>> CreateConfigurationFromServerPolicies(string serverId)
    {
        try
        {
            var server = await _serverRepository.GetByIdAsync(serverId);
            if (server == null)
                return NotFound(new { message = "Servidor LDAP no encontrado" });

            // Verificar si ya existe una configuración para este servidor
            var existingConfig = await _configRepository.GetByServerIdAsync(serverId);
            if (existingConfig != null)
            {
                return BadRequest(new { message = "Ya existe una configuración para este servidor" });
            }

            // Crear configuración con políticas basadas en el servidor
            var config = new SyncConfiguration
            {
                ServerId = serverId,
                Enabled = false, // Se crea deshabilitada por defecto
                CronSchedule = "0 2 * * *", // Daily at 2 AM
                SyncMode = "full",
                SyncUsers = true,
                SyncGroups = true,
                SyncMemberships = true,
                SyncPasswords = false,
                SyncPasswordPolicies = true,
                ForcePasswordResetDays = null,
                DeactivateOrphanUsers = true,
                DeleteOrphanGroups = false,
                PageSize = 500,
                MaxEntries = 0,
                SearchBase = server.BaseDn,
                ExcludedAttributes = null
            };

            var createdConfig = await _configRepository.AddAsync(config);

            return Ok(new SyncConfigurationDto
            {
                Id = createdConfig.Id,
                ServerId = createdConfig.ServerId,
                Enabled = createdConfig.Enabled,
                CronSchedule = createdConfig.CronSchedule,
                SyncMode = createdConfig.SyncMode,
                SyncUsers = createdConfig.SyncUsers,
                SyncGroups = createdConfig.SyncGroups,
                SyncMemberships = createdConfig.SyncMemberships,
                SyncPasswords = createdConfig.SyncPasswords,
                SyncPasswordPolicies = createdConfig.SyncPasswordPolicies,
                ForcePasswordResetDays = createdConfig.ForcePasswordResetDays,
                DeactivateOrphanUsers = createdConfig.DeactivateOrphanUsers,
                DeleteOrphanGroups = createdConfig.DeleteOrphanGroups,
                PageSize = createdConfig.PageSize,
                MaxEntries = createdConfig.MaxEntries,
                SearchBase = createdConfig.SearchBase,
                ExcludedAttributes = createdConfig.ExcludedAttributes,
                LastSync = createdConfig.LastSync,
                LastSyncStatus = createdConfig.LastSyncStatus,
                LastSyncError = createdConfig.LastSyncError,
                CreatedAt = createdConfig.CreatedAt,
                UpdatedAt = createdConfig.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al crear configuración: {ex.Message}" });
        }
    }

    /// <summary>
    /// Obtiene los registros de sincronización recientes
    /// </summary>
    [HttpGet("logs")]
    public async Task<ActionResult<IEnumerable<SyncLogDto>>> GetSyncLogs([FromQuery] int count = 10)
    {
        var logs = await _syncLogRepository.GetRecentLogsAsync(count);
        var logDtos = logs.Select(l => new SyncLogDto
        {
            Id = l.Id,
            StartedAt = l.StartedAt,
            CompletedAt = l.CompletedAt,
            Status = l.Status,
            UsersProcessed = l.UsersProcessed,
            GroupsProcessed = l.GroupsProcessed,
            MembershipsProcessed = l.MembershipsProcessed,
            ErrorsCount = l.ErrorsCount,
            ErrorMessage = l.ErrorMessage,
            IsDryRun = l.IsDryRun
        });

        return Ok(logDtos);
    }

    /// <summary>
    /// Obtiene una configuración de sincronización específica
    /// </summary>
    [HttpGet("configurations/{id}")]
    public async Task<ActionResult<SyncConfigurationDto>> GetConfiguration(string id)
    {
        var config = await _configRepository.GetByIdAsync(id);
        if (config == null)
            return NotFound();

        return Ok(new SyncConfigurationDto
        {
            Id = config.Id,
            ServerId = config.ServerId,
            Enabled = config.Enabled,
            CronSchedule = config.CronSchedule,
            SyncMode = config.SyncMode,
            SyncUsers = config.SyncUsers,
            SyncGroups = config.SyncGroups,
            SyncMemberships = config.SyncMemberships,
            SyncPasswords = config.SyncPasswords,
            SyncPasswordPolicies = config.SyncPasswordPolicies,
            ForcePasswordResetDays = config.ForcePasswordResetDays,
            DeactivateOrphanUsers = config.DeactivateOrphanUsers,
            DeleteOrphanGroups = config.DeleteOrphanGroups,
            PageSize = config.PageSize,
            MaxEntries = config.MaxEntries,
            SearchBase = config.SearchBase,
            ExcludedAttributes = config.ExcludedAttributes,
            LastSync = config.LastSync,
            LastSyncStatus = config.LastSyncStatus,
            LastSyncError = config.LastSyncError,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        });
    }

    /// <summary>
    /// Obtiene todas las configuraciones de sincronización
    /// </summary>
    [HttpGet("configurations")]
    public async Task<ActionResult<IEnumerable<SyncConfigurationDto>>> GetAllConfigurations()
    {
        var configs = await _configRepository.GetAllAsync();
        var configDtos = configs.Select(c => new SyncConfigurationDto
        {
            Id = c.Id,
            ServerId = c.ServerId,
            Enabled = c.Enabled,
            CronSchedule = c.CronSchedule,
            SyncMode = c.SyncMode,
            SyncUsers = c.SyncUsers,
            SyncGroups = c.SyncGroups,
            SyncMemberships = c.SyncMemberships,
            SyncPasswords = c.SyncPasswords,
            SyncPasswordPolicies = c.SyncPasswordPolicies,
            ForcePasswordResetDays = c.ForcePasswordResetDays,
            DeactivateOrphanUsers = c.DeactivateOrphanUsers,
            DeleteOrphanGroups = c.DeleteOrphanGroups,
            PageSize = c.PageSize,
            MaxEntries = c.MaxEntries,
            SearchBase = c.SearchBase,
            ExcludedAttributes = c.ExcludedAttributes,
            LastSync = c.LastSync,
            LastSyncStatus = c.LastSyncStatus,
            LastSyncError = c.LastSyncError,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        });

        return Ok(configDtos);
    }
}
