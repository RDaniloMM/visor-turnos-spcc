<#
.SYNOPSIS
    Publica el visor de turnos una vez y genera una carpeta autocontenida por sede,
    cada una con su .env (Site:Code, Site:DisplayName y Site:TimeZone propios).

.DESCRIPTION
    Ejecuta dotnet publish una sola vez y copia la salida a artifacts/publish-<sede>/,
    colocando como .env el fragmento de deploy/sites/<sede>.env. Las carpetas resultantes
    son el contenido que debe copiarse a la carpeta fisica de cada aplicacion IIS
    (p. ej. C:\inetpub\visorturnos\<sede>).

    El .env generado solo contiene la configuracion no secreta de la sede
    (site code, nombre y zona horaria). La cadena de conexion real
    (ConnectionStrings__LolcliOdbc) se agrega en el servidor sobre el .env local
    del sitio, que es autoritativo y nunca se sobreescribe en el despliegue.

    Requiere dotnet (SDK) y npm disponibles en PATH en el entorno de compilacion.
    Si existe artifacts/publish-<sede>, se reemplaza por completo.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\deploy\Publish-VisorTurnos.ps1 -Configuration Release
#>

[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [string]$ProjectPath = "",

    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $PSScriptRoot "..\visor_turnos.csproj"
}
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $PSScriptRoot "..\artifacts"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Resolve-ProjectPath {
    param([string]$Path)
    if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
    return (Join-Path $repoRoot $Path)
}

$projectPath = Resolve-ProjectPath $ProjectPath
if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "No se encontro el proyecto: $projectPath"
}
$outputRoot = Resolve-ProjectPath $OutputRoot
if (-not $OutputRoot) { throw "OutputRoot no puede estar vacio." }

$sitesSourceDir = Join-Path $PSScriptRoot "sites"

$sites = @(
    @{ Name = "cuajone";   Code = 1 },
    @{ Name = "ilo";       Code = 2 },
    @{ Name = "toquepala"; Code = 3 }
)

$basePublish = Join-Path $outputRoot "publish"
Write-Host "==> Publicando el artefacto base: $basePublish"
& dotnet publish $projectPath --configuration $Configuration --output $basePublish --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet publish fallo con codigo $LASTEXITCODE." }

foreach ($site in $sites) {
    $name = $site.Name
    $fragment = Join-Path $sitesSourceDir "$name.env"
    if (-not (Test-Path -LiteralPath $fragment)) {
        throw "No existe el fragmento de configuracion: $fragment"
    }

    $dest = Join-Path $outputRoot "publish-$name"
    if (Test-Path -LiteralPath $dest) { Remove-Item -LiteralPath $dest -Recurse -Force }
    New-Item -ItemType Directory -Path $dest -Force | Out-Null
    Copy-Item -Path (Join-Path $basePublish "*") -Destination $dest -Recurse -Force
    Copy-Item -LiteralPath $fragment -Destination (Join-Path $dest ".env") -Force

    # Verificacion: la config solo sobreescribe Site, el resto se hereda de appsettings.json.
    Write-Host "    -> $dest (Site:Code=$($site.Code))"
}

Write-Host ""
Write-Host "Listo. Copie cada carpeta publish-<sede> a la carpeta fisica de su aplicacion IIS."
Write-Host "Ejemplo: C:\inetpub\visorturnos\cuajone  <- artifacts\publish-cuajone"