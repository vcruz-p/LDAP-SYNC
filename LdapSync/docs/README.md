# LDAP Sync API - Documentación Completa

## Descripción General

LDAP Sync es una aplicación ASP.NET Core 9.0 desarrollada siguiendo los principios de **Clean Architecture** que permite sincronizar usuarios, grupos y membresías desde servidores LDAP hacia una base de datos MySQL.

## Arquitectura del Proyecto

El proyecto sigue Clean Architecture con las siguientes capas:

```
LdapSync/
├── src/
│   ├── Domain/              # Capa de Dominio (Entidades e Interfaces)
│   │   ├── Entities/        # Entidades del dominio
│   │   └── Interfaces/      # Interfaces de repositorios y servicios
│   ├── Application/         # Capa de Aplicación (DTOs y Servicios)
│   │   ├── DTOs/           # Objetos de Transferencia de Datos
│   │   └── Services/       # Servicios de aplicación
│   ├── Infrastructure/      # Capa de Infraestructura
│   │   ├── Data/           # DbContext y configuraciones EF Core
│   │   ├── Repositories/   # Implementaciones de repositorios
│   │   └── Services/       # Servicios externos (LDAP)
│   └── Presentation/        # Capa de Presentación (API Web)
│       ├── Controllers/    # Controladores API
│       └── Program.cs      # Punto de entrada y configuración
└── docs/                    # Documentación
```

## Estructura de la Base de Datos

La aplicación crea automáticamente la base de datos `ldapsync` al iniciar y gestiona las siguientes tablas:

### Tablas Principales

1. **ldap_servers** - Configuración de servidores LDAP
2. **ldap_users** - Usuarios sincronizados desde LDAP
3. **ldap_groups** - Grupos sincronizados desde LDAP
4. **sync_configurations** - Configuraciones de sincronización
5. **sync_logs** - Registro de ejecuciones de sincronización
6. **user_group_memberships** - Relación usuario-grupo

## Configuración

### Variables de Entorno

Configure la cadena de conexión en `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ldapsync;User=root;Password=tu_password;"
  }
}
```

### Parámetros de Conexión MySQL

| Parámetro | Descripción | Valor por defecto |
|-----------|-------------|-------------------|
| Server | Host del servidor MySQL | localhost |
| Database | Nombre de la base de datos | ldapsync |
| User | Usuario de MySQL | root |
| Password | Contraseña de MySQL | (vacío) |

## Endpoints de la API

### 1. Sincronización

#### Ejecutar Sincronización
```http
POST /api/sync/execute
Content-Type: application/json

{
  "configurationId": "guid-de-la-configuracion",
  "isDryRun": false,
  "syncMode": "full"
}
```

**Parámetros:**
- `configurationId` (string): ID de la configuración de sincronización
- `isDryRun` (boolean): Si es true, no guarda los cambios en BD
- `syncMode` (string): "full" o "incremental"

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "message": "Sincronización completada",
  "log": {
    "id": "guid",
    "startedAt": "2024-01-01T00:00:00Z",
    "completedAt": "2024-01-01T00:01:00Z",
    "status": 1,
    "statusDescription": "Success",
    "usersProcessed": 150,
    "groupsProcessed": 25,
    "membershipsProcessed": 300,
    "errorsCount": 0,
    "isDryRun": false
  },
  "usersProcessed": 150,
  "groupsProcessed": 25,
  "membershipsProcessed": 300,
  "errorsCount": 0
}
```

#### Obtener Logs de Sincronización
```http
GET /api/sync/logs?count=10
```

**Parámetros:**
- `count` (int): Número de registros a devolver (default: 10)

### 2. Configuraciones de Sincronización

#### Obtener Todas las Configuraciones
```http
GET /api/sync/configurations
```

#### Obtener Configuración por ID
```http
GET /api/sync/configurations/{id}
```

## Entidades del Dominio

### LdapServer
Representa un servidor LDAP configurado para sincronización.

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| Id | string | Identificador único (GUID) |
| Name | string | Nombre descriptivo del servidor |
| Host | string | hostname o IP del servidor |
| Port | int | Puerto de conexión (default: 389) |
| BaseDn | string | DN base para búsquedas |
| BindDn | string | DN para autenticación |
| BindPassword | string | Contraseña de autenticación |
| UseTls | bool | Usar TLS para conexión segura |
| ValidateCertificate | bool | Validar certificado SSL/TLS |
| TimeoutSeconds | int | Timeout de conexión |
| UserSearchFilter | string | Filtro LDAP para usuarios |
| GroupSearchFilter | string | Filtro LDAP para grupos |
| IsActive | bool | Estado del servidor |

### LdapUser
Representa un usuario sincronizado desde LDAP.

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| Id | string | Identificador único |
| Uid | string | UID del usuario |
| CommonName | string | Nombre común (cn) |
| DisplayName | string | Nombre para mostrar |
| Email | string | Correo electrónico |
| FirstName | string | Nombre |
| LastName | string | Apellido |
| DistinguishedName | string | DN completo del usuario |
| IsActive | bool | Estado del usuario |
| SyncedAt | DateTime | Última sincronización |

### LdapGroup
Representa un grupo sincronizado desde LDAP.

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| Id | string | Identificador único |
| CommonName | string | Nombre del grupo |
| Description | string | Descripción del grupo |
| GidNumber | int? | GID number (posix) |
| DistinguishedName | string | DN completo del grupo |
| IsActive | bool | Estado del grupo |

### SyncConfiguration
Configuración de sincronización para un servidor LDAP.

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| Id | string | Identificador único |
| ServerId | string | ID del servidor asociado |
| Enabled | bool | Configuración habilitada |
| CronSchedule | string | Expresión CRON para ejecución automática |
| SyncMode | string | Modo: "full" o "incremental" |
| SyncUsers | bool | Sincronizar usuarios |
| SyncGroups | bool | Sincronizar grupos |
| SyncMemberships | bool | Sincronizar membresías |
| SyncPasswords | bool | Sincronizar contraseñas |
| PageSize | int | Tamaño de página para búsquedas LDAP |
| MaxEntries | int | Máximo de entradas a procesar (0 = ilimitado) |

### SyncLog
Registro de cada ejecución de sincronización.

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| Id | string | Identificador único |
| StartedAt | DateTime | Inicio de la sincronización |
| CompletedAt | DateTime? | Finalización |
| Status | int | 0: Running, 1: Success, 2: Failed |
| UsersProcessed | int | Usuarios procesados |
| GroupsProcessed | int | Grupos procesados |
| MembershipsProcessed | int | Membresías procesadas |
| ErrorsCount | int | Cantidad de errores |
| ErrorMessage | string | Mensaje de error (si hubo) |
| IsDryRun | bool | Fue una ejecución de prueba |

## Flujo de Sincronización

1. **Inicio**: Se recibe solicitud de sincronización
2. **Validación**: Se verifica existencia de configuración y servidor
3. **Conexión LDAP**: Se establece conexión con el servidor LDAP
4. **Búsqueda**: Se ejecutan búsquedas LDAP según filtros configurados
5. **Mapeo**: Se convierten entradas LDAP a entidades del dominio
6. **Persistencia**: 
   - Si `isDryRun=false`: Se guardan/actualizan registros en MySQL
   - Si `isDryRun=true`: Solo se reporta lo que se haría
7. **Registro**: Se crea/actualiza el log de sincronización

## Ejecución de Migraciones

La aplicación automáticamente:
1. Verifica si la base de datos existe
2. La crea si no existe
3. Aplica migraciones pendientes al iniciar

Para generar migraciones manualmente:

```bash
cd src/Presentation
dotnet ef migrations add InitialCreate --project ../Infrastructure
dotnet ef database update
```

## Swagger UI

Al ejecutar la aplicación en modo desarrollo, acceda a:
- **URL**: http://localhost:5000
- **Swagger**: http://localhost:5000/index.html

## Consideraciones de Seguridad

1. **Contraseñas**: Las contraseñas LDAP se almacenan en texto plano en la BD. Considere usar un servicio de secretos en producción.
2. **TLS**: Active `UseTls=true` para conexiones seguras.
3. **Validación de Certificados**: Desactive solo en entornos de desarrollo.
4. **CORS**: Configure políticas específicas para producción.

## Requisitos Previos

- .NET 9.0 SDK
- MySQL 8.0+ o compatible
- Acceso a servidor(es) LDAP

## Comandos Útiles

```bash
# Restaurar paquetes
dotnet restore

# Compilar
dotnet build

# Ejecutar
dotnet run --project src/Presentation

# Publicar
dotnet publish -c Release -o ./publish
```

## Solución de Problemas

### Error de Conexión a MySQL
- Verifique que el servidor MySQL esté corriendo
- Confirme credenciales en appsettings.json
- Asegure que la BD tenga permisos de creación

### Error de Conexión LDAP
- Verifique host y puerto
- Confirme credenciales BindDN/BindPassword
- Revise reglas de firewall
- Para TLS, asegure que los certificados sean válidos

### Migraciones Fallidas
```bash
dotnet ef database drop --force
dotnet ef database update
```

## Licencia

Este proyecto es de uso interno. Todos los derechos reservados.
