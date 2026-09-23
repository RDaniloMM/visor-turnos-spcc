<#
.SYNOPSIS
    Empaqueta el codigo fuente del visor en un .zip listo para copiar al servidor,
    excluyendo artefactos de compilacion, paquetes, salidas de publish y secretos.

.DESCRIPTION
    Genera source-visor-turnos-<fecha>.zip en artifacts/ con el codigo fuente completo
    (sin bin, obj, node_modules, artifacts, publish, .git, .vs, TestResults, etc.).
    El zip NO incluye .env reales de las sedes: deploy/sites/<sede>.env se versiona
    con la config no secreta (Site:Code/DisplayName/TimeZone) y la cadena de conexion
    real se agrega al .env del sitio en el servidor.
    El servidor necesita SDK de .NET 10 y Node.js/npm solo si se va a compilar ahi.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\deploy\Package-Source.ps1
#>

[CmdletBinding()]
param(
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $PSScriptRoot "..\artifacts"
}
if (-not (Test-Path -LiteralPath $OutputRoot)) {
    New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$stamp = Get-Date -Format "yyyyMMdd-HHmm"
$zipPath = Join-Path $OutputRoot "source-visor-turnos-$stamp.zip"

$excludeNames = @(
    ".git", ".vs", "bin", "obj", "node_modules", "TestResults",
    "artifacts", "publish"
)

$files = Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Force |
    Where-Object {
        if ($_.Name -like "*.csproj.user") { return $false }
        $relative = $_.FullName.Substring($repoRoot.Length + 1)
        $segments = $relative -split '[\\/]'
        $excluded = $segments | Where-Object { $_ -in $excludeNames } | Select-Object -First 1
        -not $excluded
    }

Write-Host "Empaquetando $($files.Count) archivos..."
Compress-Archive -Path $files.FullName -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host "Listo: $zipPath"
Write-Host "Copie este archivo al servidor y extraiga la carpeta visor_turnos."
Write-Host "Para compilar en el servidor y publicar localmente:"
Write-Host "    dotnet build --configuration Release"
Write-Host "    powershell -ExecutionPolicy Bypass -File .\deploy\Publish-VisorTurnos.ps1 -Configuration Release"