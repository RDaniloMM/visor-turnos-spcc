<#
.SYNOPSIS
    Publica el visor directamente en las carpetas fisicas de IIS de cada sede.

.DESCRIPTION
    Ejecuta dotnet publish para cada sede seleccionada y genera estas carpetas:

      C:\inetpub\publish-cuajone
      C:\inetpub\publish-ilo
      C:\inetpub\publish-toquepala

    La configuracion local .env de cada sitio se conserva, pero las claves no secretas
    Site__Code, Site__DisplayName y Site__TimeZone se sincronizan siempre desde
    deploy/sites/<sede>.env. La cadena ConnectionStrings__LolcliOdbc y cualquier otra
    configuracion local no se modifican.

    Para una actualizacion de produccion use normalmente Deploy-Production.ps1,
    porque agrega backup, app_offline, reciclado del Application Pool, smoke test y
    rollback. Este script tambien puede usarse de forma independiente.

.PARAMETER Sites
    Lista separada por comas de sedes a publicar (cuajone,ilo,toquepala).
.PARAMETER OutputRoot
    Carpeta padre de publish-<sede>. Por defecto C:\inetpub.
.PARAMETER CleanOutput
    Elimina archivos anteriores antes de publicar, excepto .env y app_offline.htm.
    Deploy-Production.ps1 usa esta opcion despues de crear el backup.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\deploy\Publish-VisorTurnos.ps1 `
        -Configuration Release -Sites cuajone,ilo,toquepala
#>

[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [string]$ProjectPath = "",

    [string]$OutputRoot = "C:\inetpub",

    [string]$Sites = "cuajone,ilo,toquepala",

    [switch]$CleanOutput
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Resolve-AbsolutePath {
    param([string]$Path, [string]$BasePath)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $Path))
}

function Sync-SiteEnvironment {
    param(
        [string]$FragmentPath,
        [string]$DestinationPath
    )

    $siteKeys = @("Site__Code", "Site__DisplayName", "Site__TimeZone")
    $siteValues = @{}

    foreach ($line in [System.IO.File]::ReadAllLines($FragmentPath)) {
        if ($line -match '^\s*([^#=][^=]*)=(.*)$') {
            $key = $Matches[1].Trim()
            if ($key -in $siteKeys) {
                $siteValues[$key] = $Matches[2]
            }
        }
    }

    foreach ($key in $siteKeys) {
        if (-not $siteValues.ContainsKey($key)) {
            throw "Falta $key en el fragmento de sede: $FragmentPath"
        }
    }

    $existingLines = if (Test-Path -LiteralPath $DestinationPath -PathType Leaf) {
        [System.IO.File]::ReadAllLines($DestinationPath)
    } else {
        @()
    }

    $result = New-Object System.Collections.Generic.List[string]
    $writtenKeys = @{}

    foreach ($line in $existingLines) {
        if ($line -match '^\s*([^#=][^=]*)=(.*)$') {
            $key = $Matches[1].Trim()
            if ($siteValues.ContainsKey($key)) {
                if (-not $writtenKeys.ContainsKey($key)) {
                    $result.Add("$key=$($siteValues[$key])")
                    $writtenKeys[$key] = $true
                }
                continue
            }
        }

        $result.Add($line)
    }

    foreach ($key in $siteKeys) {
        if (-not $writtenKeys.ContainsKey($key)) {
            $result.Add("$key=$($siteValues[$key])")
        }
    }

    $utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllLines($DestinationPath, $result, $utf8WithoutBom)
}

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $repoRoot "visor_turnos.csproj"
}

$projectPath = Resolve-AbsolutePath -Path $ProjectPath -BasePath $repoRoot
if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "No se encontro el proyecto: $projectPath"
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    throw "OutputRoot no puede estar vacio."
}

$outputRoot = Resolve-AbsolutePath -Path $OutputRoot -BasePath $repoRoot
$sitesSourceDir = Join-Path $PSScriptRoot "sites"
$siteDefinitions = @{
    cuajone   = @{ Code = 1 }
    ilo       = @{ Code = 2 }
    toquepala = @{ Code = 3 }
}

$siteNames = @($Sites -split ',' | ForEach-Object { $_.Trim().ToLowerInvariant() } | Where-Object { $_ })
if ($siteNames.Count -eq 0) {
    throw "Indique al menos una sede en -Sites."
}

foreach ($name in $siteNames) {
    if (-not $siteDefinitions.ContainsKey($name)) {
        throw "Sede no valida: '$name'. Valores permitidos: cuajone, ilo, toquepala."
    }
}

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$normalizedRoot = $outputRoot.TrimEnd('\') + '\'

foreach ($name in $siteNames) {
    $definition = $siteDefinitions[$name]
    $fragment = Join-Path $sitesSourceDir "$name.env"
    if (-not (Test-Path -LiteralPath $fragment -PathType Leaf)) {
        throw "No existe el fragmento de configuracion: $fragment"
    }

    $dest = [System.IO.Path]::GetFullPath((Join-Path $outputRoot "publish-$name"))
    if (-not $dest.StartsWith($normalizedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "La salida calculada queda fuera de OutputRoot: $dest"
    }

    New-Item -ItemType Directory -Path $dest -Force | Out-Null

    if ($CleanOutput) {
        Write-Host "==> Limpiando publicacion anterior (se conservan .env y app_offline.htm): $dest"
        $cleaned = $false
        for ($attempt = 1; $attempt -le 10; $attempt++) {
            try {
                Get-ChildItem -LiteralPath $dest -Force |
                    Where-Object { $_.Name -notin @('.env', 'app_offline.htm') } |
                    Remove-Item -Recurse -Force -ErrorAction Stop
                $cleaned = $true
                break
            }
            catch {
                if ($attempt -eq 10) {
                    throw "No se pudo limpiar '$dest' despues de 10 intentos. Verifique que el Application Pool este detenido. Detalle: $($_.Exception.Message)"
                }

                Write-Host "    Archivo aun bloqueado; reintento de limpieza $attempt/10 en 1 s..."
                Start-Sleep -Seconds 1
            }
        }

        if (-not $cleaned) {
            throw "No se pudo confirmar la limpieza de '$dest'."
        }
    }

    Write-Host "==> dotnet publish para '$name': $dest"
    & dotnet publish $projectPath --configuration $Configuration --output $dest --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish fallo para '$name' con codigo $LASTEXITCODE."
    }

    $envDest = Join-Path $dest ".env"
    $hadEnvironment = Test-Path -LiteralPath $envDest -PathType Leaf
    Sync-SiteEnvironment -FragmentPath $fragment -DestinationPath $envDest
    if ($hadEnvironment) {
        Write-Host "    -> .env local conservado; claves Site__* sincronizadas para '$name'."
    } else {
        Write-Host "    -> .env inicial creado para '$name'. Agregue ConnectionStrings__LolcliOdbc."
    }

    Write-Host "    -> Publicado: $dest (Site:Code=$($definition.Code))"
}

Write-Host ""
Write-Host "Publicacion finalizada en: $outputRoot"
