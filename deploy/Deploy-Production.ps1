<#
.SYNOPSIS
    Publica y despliega el visor directamente en las carpetas fisicas de IIS,
    con backup, app_offline, preservacion de .env, parada/inicio del pool y smoke test.

.DESCRIPTION
    Rutas de produccion confirmadas:

      cuajone   -> C:\inetpub\publish-cuajone
      ilo       -> C:\inetpub\publish-ilo
      toquepala -> C:\inetpub\publish-toquepala

    Para cada sede seleccionada:
      1. Respalda la carpeta actual.
      2. Coloca app_offline.htm para detener la aplicacion con gracia.
      3. Ejecuta dotnet publish directamente sobre C:\inetpub\publish-<sede>.
      4. Conserva el .env local del servidor; en el primer despliegue siembra el
         fragmento no secreto de deploy/sites/<sede>.env.
      5. Retira app_offline.htm e inicia el Application Pool y el sitio IIS.
      6. Verifica /health/ready y /turnos en el puerto IIS de la sede.

    Si la publicacion o la verificacion falla, restaura el backup de esa sede.
    Debe ejecutarse como administrador en el servidor IIS.

.PARAMETER SkipPublish
    No compila ni publica; solo reinicia/verifica el contenido ya existente.
.PARAMETER SkipIis
    No detiene ni inicia Application Pools (util para pruebas fuera de IIS).
.PARAMETER SkipSmokeTest
    No verifica los endpoints tras el despliegue.
.PARAMETER Sites
    Lista separada por comas de sedes (cuajone,ilo,toquepala).
.PARAMETER WebRoot
    Carpeta padre de publish-<sede>. Por defecto C:\inetpub.
.PARAMETER BackupRoot
    Carpeta de backups. Por defecto C:\inetpub\visor-turnos-backups.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\deploy\Deploy-Production.ps1 `
        -Sites cuajone,ilo,toquepala

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\deploy\Deploy-Production.ps1 `
        -Sites ilo -SkipSmokeTest
#>

[CmdletBinding()]
param(
    [switch]$SkipPublish,
    [switch]$SkipIis,
    [switch]$SkipSmokeTest,
    [string]$Sites = "cuajone,ilo,toquepala",
    [string]$WebRoot = "C:\inetpub",
    [string]$BackupRoot = "C:\inetpub\visor-turnos-backups",
    [object]$AppPools = @{
        cuajone   = "Visor Turnos Hospital Cuajone"
        ilo       = "Visor Turnos Hospital Ilo"
        toquepala = "Visor Turnos Hospital Toquepala"
    },
    [object]$IisSites = @{
        cuajone   = "Visor Turnos Hospital Cuajone"
        ilo       = "Visor Turnos Hospital Ilo"
        toquepala = "Visor Turnos Hospital Toquepala"
    },
    [object]$SitePorts = @{
        cuajone   = 8080
        ilo       = 8081
        toquepala = 8082
    },
    [ValidateSet("http", "https")]
    [string]$SmokeTestScheme = "http",
    [string]$SmokeTestHost = "127.0.0.1",
    [ValidateRange(5, 300)]
    [int]$SmokeTestTimeoutSeconds = 60,
    [ValidateRange(1, 30)]
    [int]$SmokeTestRetrySeconds = 3
)

$ErrorActionPreference = "Stop"

function Test-IsAdministrator {
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object System.Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not $SkipIis -and -not (Test-IsAdministrator)) {
    throw @"
Este despliegue requiere una consola de PowerShell elevada porque escribe en
C:\inetpub y administra Application Pools de IIS.

Cierre esta consola, abra PowerShell con 'Ejecutar como administrador' y repita:
powershell -ExecutionPolicy Bypass -File .\deploy\Deploy-Production.ps1 -Sites cuajone,ilo,toquepala
"@
}

function ConvertTo-SiteMap {
    param([object]$Value, [string]$ParameterName)

    if ($Value -is [hashtable]) {
        return $Value
    }

    if ($Value -isnot [string]) {
        throw "$ParameterName debe ser un hashtable o una cadena sede=valor;sede=valor."
    }

    $parsed = @{}
    foreach ($pair in ($Value -split ';' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $parts = $pair -split '=', 2
        if ($parts.Count -ne 2) {
            throw "Formato invalido en ${ParameterName}: '$pair'. Use sede=valor."
        }

        $parsed[$parts[0].Trim().ToLowerInvariant()] = $parts[1].Trim()
    }

    return $parsed
}

function Assert-Robocopy {
    param([int]$ExitCode)
    if ($ExitCode -ge 8) {
        throw "robocopy fallo con codigo $ExitCode."
    }
}

function Get-AppPoolState {
    param(
        [string]$AppCmdPath,
        [string]$AppPoolName
    )

    $output = & $AppCmdPath list apppool $AppPoolName /text:state 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudo consultar el Application Pool '$AppPoolName': $($output -join ' ')"
    }

    $stateText = ($output | Select-Object -First 1) -as [string]
    if ([string]::IsNullOrWhiteSpace($stateText)) {
        throw "appcmd no devolvio el estado del Application Pool '$AppPoolName'."
    }

    return $stateText.Trim()
}

function Wait-AppPoolState {
    param(
        [string]$AppCmdPath,
        [string]$AppPoolName,
        [string]$ExpectedState,
        [int]$TimeoutSeconds = 30
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $state = Get-AppPoolState -AppCmdPath $AppCmdPath -AppPoolName $AppPoolName
        if ([string]::Equals($state, $ExpectedState, [System.StringComparison]::OrdinalIgnoreCase)) {
            return
        }

        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "El Application Pool '$AppPoolName' no alcanzo el estado '$ExpectedState' en $TimeoutSeconds segundos. Estado actual: '$state'."
}

function Stop-AppPoolSafely {
    param([string]$AppCmdPath, [string]$AppPoolName)

    $state = Get-AppPoolState -AppCmdPath $AppCmdPath -AppPoolName $AppPoolName
    if (-not [string]::Equals($state, "Stopped", [System.StringComparison]::OrdinalIgnoreCase)) {
        & $AppCmdPath stop apppool "/apppool.name:$AppPoolName" | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "appcmd no pudo detener '$AppPoolName' (codigo $LASTEXITCODE)."
        }
    }

    Wait-AppPoolState -AppCmdPath $AppCmdPath -AppPoolName $AppPoolName -ExpectedState "Stopped"
}

function Start-AppPoolSafely {
    param([string]$AppCmdPath, [string]$AppPoolName)

    $state = Get-AppPoolState -AppCmdPath $AppCmdPath -AppPoolName $AppPoolName
    if (-not [string]::Equals($state, "Started", [System.StringComparison]::OrdinalIgnoreCase)) {
        & $AppCmdPath start apppool "/apppool.name:$AppPoolName" | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "appcmd no pudo iniciar '$AppPoolName' (codigo $LASTEXITCODE)."
        }
    }

    Wait-AppPoolState -AppCmdPath $AppCmdPath -AppPoolName $AppPoolName -ExpectedState "Started"
}

function Get-IisSiteState {
    param([string]$AppCmdPath, [string]$SiteName)

    $output = & $AppCmdPath list site $SiteName /text:state 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudo consultar el sitio IIS '$SiteName': $($output -join ' ')"
    }

    $stateText = ($output | Select-Object -First 1) -as [string]
    if ([string]::IsNullOrWhiteSpace($stateText)) {
        throw "appcmd no devolvio el estado del sitio IIS '$SiteName'."
    }

    return $stateText.Trim()
}

function Start-IisSiteSafely {
    param([string]$AppCmdPath, [string]$SiteName)

    $state = Get-IisSiteState -AppCmdPath $AppCmdPath -SiteName $SiteName
    if (-not [string]::Equals($state, "Started", [System.StringComparison]::OrdinalIgnoreCase)) {
        & $AppCmdPath start site $SiteName | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "appcmd no pudo iniciar el sitio IIS '$SiteName' (codigo $LASTEXITCODE)."
        }
    }

    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    do {
        $state = Get-IisSiteState -AppCmdPath $AppCmdPath -SiteName $SiteName
        if ([string]::Equals($state, "Started", [System.StringComparison]::OrdinalIgnoreCase)) {
            return
        }
        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "El sitio IIS '$SiteName' no alcanzo el estado Started en 30 segundos. Estado actual: '$state'."
}

function Test-HttpEndpoint {
    param(
        [string]$Scheme,
        [string]$Hostname,
        [int]$Port,
        [string]$Path,
        [string]$OutFile
    )

    if (Test-Path -LiteralPath $OutFile) {
        Remove-Item -LiteralPath $OutFile -Force
    }

    $curlArgs = @(
        "--silent", "--show-error", "--insecure", "--noproxy", "*",
        "--connect-timeout", "3", "--max-time", "8",
        "--output", $OutFile, "--write-out", "%{http_code}",
        "${Scheme}://${Hostname}:${Port}${Path}"
    )

    # PowerShell 5 convierte stderr de curl.exe en un error terminante cuando
    # ErrorActionPreference es Stop. Un puerto aun no disponible debe poder
    # reintentarse; el codigo de salida de curl determina el resultado.
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $code = & curl.exe @curlArgs 2>$null
        $curlExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    if ($curlExitCode -ne 0) {
        return "000"
    }

    return ([string]$code).Trim()
}

function Assert-SiteDisplayName {
    param(
        [string]$SiteName,
        [string]$PageFile,
        [string]$SiteFragmentDirectory
    )

    $fragment = Join-Path $SiteFragmentDirectory "$SiteName.env"
    $configuredName = Get-Content -LiteralPath $fragment |
        Where-Object { $_ -match '^Site__DisplayName=' } |
        Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($configuredName)) {
        throw "No se encontro Site__DisplayName en $fragment."
    }

    $expectedName = $configuredName.Substring("Site__DisplayName=".Length).Trim()
    $page = Get-Content -LiteralPath $PageFile -Raw
    $match = [regex]::Match($page, '<h1\s+id="site-name"[^>]*>(.*?)</h1>', 'Singleline, IgnoreCase')
    if (-not $match.Success) {
        throw "La pagina de '$SiteName' no contiene el encabezado de sede esperado."
    }

    $actualName = [System.Net.WebUtility]::HtmlDecode($match.Groups[1].Value).Trim()
    if (-not [string]::Equals($actualName, $expectedName, [System.StringComparison]::Ordinal)) {
        throw "Identidad de sede incorrecta para '$SiteName': la pagina muestra '$actualName'; se esperaba '$expectedName'. Revise .env, variables Site__* del servidor y binding IIS."
    }
}

function Wait-ForReadyEndpoint {
    param(
        [string]$Scheme,
        [string]$Hostname,
        [int]$Port,
        [string]$OutFile,
        [int]$TimeoutSeconds,
        [int]$RetrySeconds
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    $attempt = 0
    $lastCode = "sin respuesta"
    $lastBody = ""

    do {
        $attempt++
        $lastCode = Test-HttpEndpoint -Scheme $Scheme -Hostname $Hostname -Port $Port -Path "/health/ready" -OutFile $OutFile
        $lastBody = if (Test-Path -LiteralPath $OutFile) {
            Get-Content -LiteralPath $OutFile -Raw
        } else {
            ""
        }

        if ($lastCode -eq "200" -and $lastBody.Trim() -ceq "Healthy") {
            return @{
                IsReady = $true
                Code = $lastCode
                Body = $lastBody
                Attempts = $attempt
            }
        }

        if ([DateTime]::UtcNow -lt $deadline) {
            $state = if ($lastCode -eq "000") {
                "sin conexion HTTP"
            } elseif ([string]::IsNullOrWhiteSpace($lastBody)) {
                "sin cuerpo"
            } else {
                $lastBody.Trim()
            }
            Write-Host "    Aun no listo: HTTP $lastCode ($state). Reintento en $RetrySeconds s..."
            Start-Sleep -Seconds $RetrySeconds
        }
    } while ([DateTime]::UtcNow -lt $deadline)

    return @{
        IsReady = $false
        Code = $lastCode
        Body = $lastBody
        Attempts = $attempt
    }
}

$AppPools = ConvertTo-SiteMap -Value $AppPools -ParameterName "-AppPools"
$IisSites = ConvertTo-SiteMap -Value $IisSites -ParameterName "-IisSites"
$SitePorts = ConvertTo-SiteMap -Value $SitePorts -ParameterName "-SitePorts"

$siteNames = @($Sites -split ',' | ForEach-Object { $_.Trim().ToLowerInvariant() } | Where-Object { $_ })
if ($siteNames.Count -eq 0) {
    throw "Indique al menos una sede en -Sites."
}

$allowedSites = @("cuajone", "ilo", "toquepala")
foreach ($name in $siteNames) {
    if ($name -notin $allowedSites) {
        throw "Sede no valida: '$name'. Valores permitidos: cuajone, ilo, toquepala."
    }
    if (-not $AppPools.ContainsKey($name)) {
        throw "No hay Application Pool definido para la sede '$name'."
    }
    if (-not $IisSites.ContainsKey($name)) {
        throw "No hay sitio IIS definido para la sede '$name'."
    }
    if (-not $SitePorts.ContainsKey($name)) {
        throw "No hay puerto IIS definido para la sede '$name'."
    }
}

$webRootPath = [System.IO.Path]::GetFullPath($WebRoot)
$backupRootPath = [System.IO.Path]::GetFullPath($BackupRoot)
New-Item -ItemType Directory -Path $webRootPath -Force | Out-Null
New-Item -ItemType Directory -Path $backupRootPath -Force | Out-Null

$appcmd = "C:\Windows\System32\inetsrv\appcmd.exe"
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"

foreach ($name in $siteNames) {
    Write-Host ""
    Write-Host "===== Sede: $name ====="

    $dest = [System.IO.Path]::GetFullPath((Join-Path $webRootPath "publish-$name"))
    $expectedPrefix = $webRootPath.TrimEnd('\') + '\'
    if (-not $dest.StartsWith($expectedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "La carpeta de la sede queda fuera de WebRoot: $dest"
    }

    $backup = Join-Path $backupRootPath "$stamp-$name"
    $appPool = [string]$AppPools[$name]
    $iisSite = [string]$IisSites[$name]
    $port = [int]$SitePorts[$name]
    $hadPreviousDeployment = Test-Path -LiteralPath $dest -PathType Container

    if ($hadPreviousDeployment) {
        Write-Host "==> Backup: $dest -> $backup"
        New-Item -ItemType Directory -Path $backup -Force | Out-Null
        robocopy $dest $backup /E /NFL /NDL /NJH /NJS /NP | Out-Null
        Assert-Robocopy $LASTEXITCODE
    } else {
        Write-Host "==> Primer despliegue; no existe una carpeta anterior para respaldar."
        New-Item -ItemType Directory -Path $dest -Force | Out-Null
    }

    $offline = Join-Path $dest "app_offline.htm"
    Set-Content -LiteralPath $offline -Value "<!doctype html><html><body><h1>Actualizando informacion</h1><p>La pantalla se restaurara en unos segundos.</p></body></html>" -Encoding UTF8
    Write-Host "==> app_offline.htm colocado: $offline"
    Start-Sleep -Seconds 2

    try {
        if (-not $SkipIis) {
            if (-not (Test-Path -LiteralPath $appcmd -PathType Leaf)) {
                throw "No se encontro appcmd.exe. Verifique que IIS este instalado."
            }

            $siteState = Get-IisSiteState -AppCmdPath $appcmd -SiteName $iisSite
            Write-Host "==> Estado inicial del sitio IIS '$iisSite': $siteState"

            Write-Host "==> Deteniendo Application Pool para liberar las DLL: $appPool"
            Stop-AppPoolSafely -AppCmdPath $appcmd -AppPoolName $appPool
            Write-Host "    -> Application Pool detenido."
        }

        if (-not $SkipPublish) {
            Write-Host "==> Publicando directamente en: $dest"
            & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "Publish-VisorTurnos.ps1") `
                -Configuration Release -Sites $name -OutputRoot $webRootPath -CleanOutput
            if ($LASTEXITCODE -ne 0) {
                throw "Publish-VisorTurnos.ps1 fallo para '$name' con codigo $LASTEXITCODE."
            }
        } else {
            Write-Host "==> Publicacion omitida; se conserva el contenido existente."
        }

        if (Test-Path -LiteralPath $offline) {
            Remove-Item -LiteralPath $offline -Force
        }
        Write-Host "==> app_offline.htm retirado."

        if (-not $SkipIis) {
            Write-Host "==> Iniciando Application Pool: $appPool"
            Start-AppPoolSafely -AppCmdPath $appcmd -AppPoolName $appPool
            Write-Host "==> Iniciando sitio IIS: $iisSite"
            Start-IisSiteSafely -AppCmdPath $appcmd -SiteName $iisSite
        }

        if (-not $SkipSmokeTest) {
            $healthFile = Join-Path $env:TEMP "visor-health-$name.htm"
            $pageFile = Join-Path $env:TEMP "visor-page-$name.htm"
            $baseUrl = "${SmokeTestScheme}://${SmokeTestHost}:${port}"

            Write-Host "==> Smoke test: $baseUrl/health/ready (espera maxima: $SmokeTestTimeoutSeconds s)"
            $ready = Wait-ForReadyEndpoint -Scheme $SmokeTestScheme -Hostname $SmokeTestHost -Port $port `
                -OutFile $healthFile -TimeoutSeconds $SmokeTestTimeoutSeconds -RetrySeconds $SmokeTestRetrySeconds
            $ok = [bool]$ready.IsReady

            if ($ok) {
                $pageCode = Test-HttpEndpoint -Scheme $SmokeTestScheme -Hostname $SmokeTestHost -Port $port -Path "/turnos" -OutFile $pageFile
                $ok = ($pageCode -eq "200")
                if ($ok) {
                    Assert-SiteDisplayName -SiteName $name -PageFile $pageFile -SiteFragmentDirectory (Join-Path $PSScriptRoot "sites")
                }
            }

            if (-not $ok) {
                $detail = if ($ready.Code -eq "000") {
                    "No hubo conexion HTTP; revise que el sitio IIS este iniciado y que su binding escuche en ${SmokeTestHost}:${port}. Si usa otra IP, indique -SmokeTestHost."
                } else {
                    "Ultimo health HTTP $($ready.Code)."
                }
                throw "Smoke test fallo para '$name' en $baseUrl despues de $($ready.Attempts) intentos. $detail"
            }

            Write-Host "==> Smoke test OK: $baseUrl (listo en $($ready.Attempts) intento(s))"
        }
    }
    catch {
        $failure = $_.Exception.Message
        Write-Host "ERROR durante el despliegue de '$name': $failure" -ForegroundColor Red

        if ($hadPreviousDeployment -and (Test-Path -LiteralPath $backup -PathType Container)) {
            if (-not $SkipIis -and (Test-Path -LiteralPath $appcmd -PathType Leaf)) {
                Write-Host "==> Deteniendo Application Pool antes del rollback: $appPool"
                Stop-AppPoolSafely -AppCmdPath $appcmd -AppPoolName $appPool
            }

            Set-Content -LiteralPath $offline -Value "<!doctype html><html><body><h1>Restaurando version anterior</h1></body></html>" -Encoding UTF8
            Start-Sleep -Seconds 2
            Write-Host "==> Restaurando backup: $backup -> $dest"
            robocopy $backup $dest /MIR /NFL /NDL /NJH /NJS /NP /R:1 /W:1 | Out-Null
            Assert-Robocopy $LASTEXITCODE
        } else {
            Write-Host "==> No habia despliegue anterior; app_offline.htm se mantiene para no servir una publicacion incompleta."
        }

        if ($hadPreviousDeployment -and -not $SkipIis -and (Test-Path -LiteralPath $appcmd -PathType Leaf)) {
            Write-Host "==> Iniciando Application Pool restaurado: $appPool"
            Start-AppPoolSafely -AppCmdPath $appcmd -AppPoolName $appPool
            Write-Host "==> Iniciando sitio IIS restaurado: $iisSite"
            Start-IisSiteSafely -AppCmdPath $appcmd -SiteName $iisSite
        }

        throw "Despliegue de '$name' fallo. Detalle: $failure"
    }

    Write-Host "===== '$name' desplegada correctamente en $dest ====="
}

Write-Host ""
Write-Host "Despliegue finalizado."
Write-Host "Backups en: $backupRootPath"
