curl -X POST https://localhost:5001/api/LdapServers \
  -H "Content-Type: application/x-www-form-urlencoded" \
  --data-urlencode "name=gitea.sc2" \
  --data-urlencode "host=192.168.4.36" \
  --data-urlencode "port=389" \
  --data-urlencode "baseDn=dc=example,dc=com" \
  --data-urlencode "bindDn=cn=admin,dc=example,dc=com" \
  --data-urlencode "bindPassword=tu_password"


  $hostName = "192.168.4.36"
$port = 389
$bindDn = "cn=adm,dc=example,dc=com" # <--- CAMBIA ESTO por tu Bind DN real
$password = Read-Host "1q2w3e4r5t**" -AsSecureString

# Convertir secure string a texto plano para el ejemplo (solo para prueba local)
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
$plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

try {
    $entry = New-Object System.DirectoryServices.DirectoryEntry("LDAP://$hostName:$port/$bindDn", $bindDn, $plainPassword)
    
    # Intentar leer algo para forzar la conexión
    $name = $entry.Name
    Write-Host "¡Conexión Exitosa! Nombre: $name" -ForegroundColor Green
}
catch {
    Write-Host "Error de conexión: $_" -ForegroundColor Red
    if ($_.Exception.Message -like "*credentials*") {
        Write-Host "Las credenciales son inválidas (Error 49)." -ForegroundColor Yellow
    }
}
finally {
    if ($entry) { $entry.Dispose() }
}