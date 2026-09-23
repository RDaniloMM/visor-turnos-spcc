<#
.SYNOPSIS
    Despliega las carpetas publicadas del visor a las carpetas fisicas de IIS en
    produccion, con backup, app_offline, preservacion de config del servidor,
    reinicio de App Pool y smoke test. Pensado para ejecutarse en el propio servidor
    (codigo fuente y SDK ahi) o desde una PC con acceso a las carpetas.

.DESCRIPTION
    Flujo por sede:
      1. (opcional) Publica el proyecto mediante Publish-VisorTurnos.ps1.
      2. Respaldar la carpeta actual de IIS en <BackupRoot>\<marca>-<sede>\.
      3. Colocar app_offline.htm en la carpeta para detener la app con gracia.
      4. Copiar artifacts/publish-<sede> con robocopy /MIR, excluyendo siempre el
         .env local del sitio (la config de conexion de produccion ES autoritativa
         y nunca se sobreescribe ni se borra).
      5. Si el sitio no tiene .env (deploy inicial), sembrar el .env generado por
         Publish-VisorTurnos.ps1, que contiene solo la config no secreta de la sede;
         el operador debe agregar ahi ConnectionStrings__LolcliOdbc antes de activar.
      6. Quitar app_offline.htm y reciclar el App Pool mediante appcmd.
      7. Smoke test contra /health/ready y /turnos usando --resolve de curl.
      Si el smoke test falla, restaura el backup y recicla de nuevo.

    El comando appcmd requiere permisos de administrador. Sin IIS (p. ej. local)
    use -SkipIis, y -SkipSmokeTest para omitir la verificacion.

.PARAMETER SkipPublish
    Usa artifacts/publish-<sede> existentes sin volver a publicar.
.PARAMETER SkipIis
    No recicla App Pools (para entornos sin IIS).
.PARAMETER SkipSmokeTest
    No verifica salud tras el despliegue.
.PARAMETER Sites
    Lista separada por comas de sedes a desplegar (cuajone,ilo,toquepala).
.PARAMETER ArtifactsRoot
    Carpeta donde viven publish-<sede> (por defecto ..\artifacts relative a este script).
.PARAMETER WebRoot
    Carpeta fisica base de los sitios IIS (por defecto C:\inetpub\visorturnos).
.PARAMETER BackupRoot
    Carpeta destino de los backups (por defecto <WebRoot>\backup).
.PARAMETER AppPoolPrefix
    Prefijo de los App Pools; por defecto VisorPool-<sede> (p. ej. VisorPool-cuajone).
.PARAMETER SiteHosts
    Acceso a los hostnames del smoke test por sede. Puede pasarse como hashtable
    (solo al cargar el script con dot-source o -Command) o como cadena
    "sede=hostname;sede=hostname" (recomendado con -File), p. ej.
    "-SiteHosts 'cuajone=turnos-cuajone.hospital.local;ilo=turnos-ilo.hospital.local'".
.PARAMETER SmokeTestPort
    Puerto usado en el smoke test (por defecto 443). En pruebas locales use el puerto
    del perfil de arranque (p. ej. 7218).

.EXAMPLE
    # En el servidor, con el codigo en C:\visor_turnos:
    powershell -ExecutionPolicy Bypass -File .\deploy\Deploy-Production.ps1 -Sites cuajone,ilo,toquepala

.EXAMPLE
    # Reutilizar artefactos ya publicados y no tocar IIS (prueba local):
    powershell -ExecutionPolicy Bypass -File .\deploy\Deploy-Production.ps1 `
        -SkipPublish -SkipIis -SkipSmokeTest -WebRoot C:\tmp\webroot
#>

[CmdletBinding()]
param(
    [switch]$SkipPublish,
    [switch]$SkipIis,
    [switch]$SkipSmokeTest,
    [string]$Sites = "cuajone,ilo,toquepala",
    [string]$ArtifactsRoot = "",
    [string]$WebRoot = "C:\inetpub\visorturnos",
    [string]$BackupRoot = "",
    [string]$AppPoolPrefix = "VisorPool",
    [object]$SiteHosts = @{
        cuajone   = "turnos-cuajone.hospital.local"
        ilo       = "turnos-ilo.hospital.local"
        toquepala = "turnos-toquepala.hospital.local"
    },
    [int]$SmokeTestPort = 443
)

$ErrorActionPreference = "Stop"

if ($SiteHosts -is [string]) {
    $parsed = @{}
    foreach ($pair in ($SiteHosts -split ';')) {
        $parts = $pair -split '='
        if ($parts.Count -ne 2) { throw "Formato invalido en -SiteHosts: '$pair'. Use sede=hostname." }
        $parsed[$parts[0].Trim()] = $parts[1].Trim()
    }
    $SiteHosts = $parsed
}

if ([string]::IsNullOrWhiteSpace($ArtifactsRoot)) {
    $ArtifactsRoot = Join-Path $PSScriptRoot "..\artifacts"
}
if ([string]::IsNullOrWhiteSpace($BackupRoot)) {
    $BackupRoot = Join-Path $WebRoot "backup"
}

$siteNames = $Sites -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
if ($siteNames.Count -eq 0) { throw "Indique al menos una sede en -Sites." }

$appcmd = "C:\Windows\System32\inetsrv\appcmd.exe"

function Assert-Robocopy {
    param([int]$ExitCode)
    if ($ExitCode -ge 8) { throw "robocopy fallo con codigo $ExitCode." }
}

function Test-HttpsEndpoint {
    param([string]$Hostname, [string]$Port, [string]$Path, [string]$OutFile)
    $args = @(
        "--resolve", "${Hostname}:${Port}:127.0.0.1",
        "-sk", "-o", $OutFile, "-w", "%{http_code}",
        "https://${Hostname}:${Port}${Path}"
    )
    $code = & curl.exe @args 2>$null
    return $code
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"

# 0. Publicar una sola vez para todas las sedes
if (-not $SkipPublish) {
    Write-Host "==> Publicando artefactos para todas las sedes..."
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "Publish-VisorTurnos.ps1") -Configuration Release
    if ($LASTEXITCODE -ne 0) { throw "Publish-VisorTurnos.ps1 fallo con codigo $LASTEXITCODE." }
}

foreach ($name in $siteNames) {
    Write-Host ""
    Write-Host "===== Sede: $name ====="

    if (-not $SiteHosts.ContainsKey($name)) {
        throw "No hay hostname definido para la sede '$name' en -SiteHosts."
    }
    $hostname = $SiteHosts[$name]
    $source    = Join-Path $ArtifactsRoot "publish-$name"
    $dest      = Join-Path $WebRoot $name
    $backup    = Join-Path $BackupRoot "$stamp-$name"
    $appPool   = "${AppPoolPrefix}-$name"

    if (-not (Test-Path -LiteralPath $source)) {
        throw "No existe el artefacto publicado: $source. Ejecute sin -SkipPublish o revise -ArtifactsRoot."
    }

    # 1. Backup de la carpeta actual
    if (Test-Path -LiteralPath $dest) {
        Write-Host "==> Backup: $dest -> $backup"
        New-Item -ItemType Directory -Path $backup -Force | Out-Null
        robocopy $dest $backup /E /NFL /NDL /NJH /NJS /NP | Out-Null
        Assert-Robocopy $LASTEXITCODE
        $envSeed = Join-Path $dest ".env"
        $envSeeded = $false
        if (Test-Path -LiteralPath $envSeed) {
            Write-Host "    -> .env de la sede preservado (config autoritativa)."
        } else {
            Write-Host "    -> Deploy inicial: se sembrara el .env del fragmento de la sede."
            $envSeeded = $true
        }
    } else {
        $envSeeded = $true
        Write-Host "==> Sin carpeta previa; deploy inicial."
        New-Item -ItemType Directory -Path $dest -Force | Out-Null
    }

    # 2. app_offline para detener la app con gracia
    $offline = Join-Path $dest "app_offline.htm"
    Set-Content -LiteralPath $offline -Value "<!doctype html><html><body><h1>Actualizando informacion</h1><p>La pantalla se restaurara en unos segundos.</p></body></html>" -Encoding UTF8
    Write-Host "==> app_offline.htm colocado: $offline"
    Start-Sleep -Seconds 2

    try {
        # 3. Copia /MIR de los artefactos. /XF conserva app_offline.htm (todavia en uso)
        #    y el .env del servidor (config de conexion por sede, fuera del artefacto).
        Write-Host "==> Copiando $source -> $dest"
        robocopy $source $dest /MIR /XF app_offline.htm .env /NFL /NDL /NJH /NJS /NP /R:1 /W:1
        Assert-Robocopy $LASTEXITCODE

        # 4. Deploy inicial: sembrar el .env del fragmento publicado si el sitio no tenia uno.
        $envDest = Join-Path $dest ".env"
        if ($envSeeded) {
            $envTemplate = Join-Path $source ".env"
            if (Test-Path -LiteralPath $envTemplate) {
                Copy-Item -LiteralPath $envTemplate -Destination $envDest -Force
                Write-Host "==> .env sembrado desde el fragmento publicado. Agregue ConnectionStrings__LolcliOdbc si la sede aun no lo tiene."
            } else {
                Write-Host "==> Sin fragmento .env en el artefacto; debe existir un .env en el sitio."
            }
        } else {
            Write-Host "==> .env del sitio conservado; no se sobreescribe."
        }

        # 5. Retirar app_offline y reciclar el App Pool
        if (Test-Path -LiteralPath $offline) { Remove-Item -LiteralPath $offline -Force }
        Write-Host "==> app_offline.htm retirado."
        if (-not $SkipIis) {
            Write-Host "==> Reciclando App Pool: $appPool"
            & $appcmd recycle apppool "/apppool.name:$appPool" | Out-Null
            if ($LASTEXITCODE -ne 0) {
                throw "appcmd no pudo reciclar '$appPool'. Verifique el nombre del App Pool (codigo $LASTEXITCODE)."
            }
        }
    }
    catch {
        # 6. Rollback automático: restaurar el backup
        Write-Host "ERROR durante el despliegue de '$name': $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "==> Restaurando backup..."
        if (Test-Path -LiteralPath $backup) {
            robocopy $backup $dest /MIR /NFL /NDL /NJH /NJS /NP /R:1 /W:1 | Out-Null
            Assert-Robocopy $LASTEXITCODE
            if (-not $SkipIis) { & $appcmd recycle apppool "/apppool.name:$appPool" | Out-Null }
        }
        throw "Despliegue de '$name' fallo y se restauro el backup."
    }

    # 7. Smoke test
    if (-not $SkipSmokeTest) {
        Write-Host "==> Smoke test: https://${hostname}:${SmokeTestPort}/health/ready"
        $healthyScript = Join-Path $env:TEMP "visor-health-$name.htm"
        $code = Test-HttpsEndpoint -Hostname $hostname -Port $SmokeTestPort -Path "/health/ready" -OutFile $healthyScript
        $ok = ($code -eq "200")
        $healthBody = ""
        if ($ok -and (Test-Path -LiteralPath $healthyScript)) {
            $healthBody = Get-Content -LiteralPath $healthyScript -Raw
            if ($healthBody -notmatch "Healthy") { $ok = $false }
        }
        if ($ok) {
            $pageCode = Test-HttpsEndpoint -Hostname $hostname -Port $SmokeTestPort -Path "/turnos" -OutFile (Join-Path $env:TEMP "visor-page-$name.htm")
            if ($pageCode -ne "200") { $ok = $false }
        }

        if (-not $ok) {
            Write-Host "Smoke test FALLIDO ($code). Restaurando backup..." -ForegroundColor Red
            if (Test-Path -LiteralPath $backup) {
                robocopy $backup $dest /MIR /NFL /NDL /NJH /NJS /NP /R:1 /W:1 | Out-Null
                Assert-Robocopy $LASTEXITCODE
                if (-not $SkipIis) { & $appcmd recycle apppool "/apppool.name:$appPool" | Out-Null }
            }
            throw "Smoke test fallo para '$name'. Se restauro el backup."
        } else {
            Write-Host "Smoke test OK: $healthBody"
        }
    }

    Write-Host "===== '$name' desplegada correctamente ====="
}

Write-Host ""
Write-Host "Deployment finalizado."
Write-Host "Backups en: $BackupRoot"
if (-not $SkipIis) { Write-Host "App Pools reciclados (prefijo $AppPoolPrefix)." }