# Implementación de Sincronización LDAP con Extracción de Políticas

## Resumen de Cambios

Se ha implementado la funcionalidad para sincronizar servidores activos extrayendo automáticamente las políticas de seguridad y unidades organizativas directamente del servidor LDAP durante el proceso de configuración.

## Archivos Modificados

### 1. `/workspace/LdapSync/src/Domain/Interfaces/IRepositories.cs`
- **Cambio**: Se agregó el método `ExtractPoliciesAndOusFromLdapAsync` a la interfaz `ILdapService`
- **Propósito**: Definir el contrato para extraer políticas y OUs desde LDAP

```csharp
Task<(SyncConfiguration Config, IEnumerable<string> Ous)> ExtractPoliciesAndOusFromLdapAsync(LdapServer server);
```

### 2. `/workspace/LdapSync/src/Infrastructure/Services/LdapService.cs`
- **Cambio**: Se implementó el método `ExtractPoliciesAndOusFromLdapAsync`
- **Funcionalidad**:
  - Se conecta al servidor LDAP usando las credenciales configuradas
  - Extrae todas las Unidades Organizativas (OUs) buscando `objectClass=organizationalUnit`
  - Extrae políticas de seguridad buscando entradas `pwdPolicy`, `pwdPolicyElement`
  - Lee atributos de seguridad:
    - `pwdMinLength`: Longitud mínima de contraseña
    - `pwdInHistory`: Historial de contraseñas
    - `pwdMaxAge`: Edad máxima (convertida de segundos a días)
    - `pwdExpireWarning`: Días de advertencia
    - `pwdLockout`: Estado de bloqueo de cuenta
  - Detecta atributos de políticas en usuarios (`pwdChangedTime`, `pwdAccountLockedTime`)
  - Retorna una configuración poblada con los valores extraídos y la lista de OUs

### 3. `/workspace/LdapSync/src/Presentation/Controllers/SyncController.cs`
- **Cambio**: Se actualizó el endpoint `POST /api/sync/configurations/auto-from-server/{serverId}`
- **Funcionalidad**:
  - Ahora llama al nuevo método `ExtractPoliciesAndOusFromLdapAsync`
  - Crea automáticamente una configuración basada en las políticas reales del servidor
  - La configuración se guarda deshabilitada por defecto para revisión previa

### 4. `/workspace/LdapSync/docs/README.md`
- **Cambio**: Se añadió documentación completa de los nuevos endpoints
- **Contenido**:
  - Documentación de `POST /api/sync/execute-all-active`
  - Documentación detallada de `POST /api/sync/configurations/auto-from-server/{serverId}`
  - Explicación del flujo de extracción de políticas
  - Ejemplos de respuestas JSON

## Endpoints Nuevos/Actualizados

### 1. `POST /api/sync/execute-all-active`
Ejecuta sincronización para todos los servidores activos con configuraciones habilitadas.

**Parámetros:**
- `isDryRun` (query, boolean): Si es true, no guarda cambios en BD

**Respuesta:**
```json
{
  "success": true,
  "message": "Sincronización completada: 3 exitosas, 0 fallidas",
  "totalServers": 3,
  "successfulSyncs": 3,
  "failedSyncs": 0,
  "results": [...]
}
```

### 2. `POST /api/sync/configurations/auto-from-server/{serverId}`
Crea una configuración de sincronización extrayendo automáticamente las políticas del servidor LDAP.

**Proceso:**
1. Valida que el servidor exista
2. Verifica que no haya una configuración existente
3. Se conecta al LDAP y extrae:
   - Todas las OUs disponibles
   - Políticas de contraseña (pwdPolicy)
   - Atributos de seguridad de usuarios
4. Crea configuración con valores extraídos:
   - `SyncPasswordPolicies`: true si detecta políticas
   - `ForcePasswordResetDays`: valor de pwdMaxAge convertido a días
5. Guarda la configuración (deshabilitada por defecto)

**Flujo de Extracción:**
```
Conexión LDAP → Búsqueda OUs → Búsqueda pwdPolicy → 
Lectura atributos → Detección en usuarios → Crear config → Guardar
```

## Atributos LDAP Extraídos

### Políticas de Contraseña (pwdPolicy)
| Atributo | Descripción | Conversión |
|----------|-------------|------------|
| pwdMinLength | Longitud mínima | Entero directo |
| pwdInHistory | Historial contraseñas | Entero directo |
| pwdMaxAge | Edad máxima | Segundos → Días (/86400) |
| pwdExpireWarning | Advertencia expiración | Segundos → Días (/86400) |
| pwdLockout | Bloqueo cuenta | Booleano |

### Unidades Organizativas
- Busca: `objectClass=organizationalUnit`
- Retorna: Lista de Distinguished Names de todas las OUs

### Atributos de Usuario para Detección
- `pwdChangedTime`: Indica gestión de cambio de contraseña
- `pwdAccountLockedTime`: Indica gestión de bloqueo de cuenta
- `pwdFailureTime`: Intentos fallidos
- `loginGraceLimit`: Límite de gracia de login

## Beneficios

1. **Configuración Automática**: No requiere configuración manual de políticas
2. **Detección Inteligente**: Identifica automáticamente las capacidades del servidor LDAP
3. **Soporte Multi-Servidor**: Funciona con diferentes tipos de servidores LDAP
4. **Seguridad**: Las políticas reales del servidor se aplican correctamente
5. **Flexibilidad**: La configuración se crea deshabilitada para revisión previa

## Uso Recomendado

1. Registrar el servidor LDAP con sus credenciales
2. Llamar a `POST /api/sync/configurations/auto-from-server/{serverId}`
3. Revisar la configuración generada
4. Habilitar la configuración si es correcta
5. Ejecutar sincronización manual o esperar el schedule CRON
6. Opcionalmente usar `POST /api/sync/execute-all-active` para sincronizar todos los servidores

## Consideraciones

- El servidor LDAP debe ser accesible desde la aplicación
- Las credenciales deben tener permisos de lectura en el directorio
- Si no hay políticas pwdPolicy, la configuración se crea con valores por defecto
- Las OUs se extraen pero no se almacenan directamente (pueden usarse para filtrar búsquedas)
