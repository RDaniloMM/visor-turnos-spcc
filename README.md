# Visor publico de turnos LOLCLI

Aplicacion ASP.NET Core Razor Pages para una pantalla de sala de espera. El servidor mantiene un unico snapshot en memoria, sondea una fuente configurable y publica cambios por SignalR. Los televisores nunca acceden a SQL Server.

## Estado actual

- La interfaz 16:9, la API, SignalR, el almacenamiento de snapshots y el worker estan implementados.
- La pantalla separa una vista rotativa por consultorio y medico de una vista general fija; esta ultima muestra el medico y el consultorio de destino.
- Las citas con `obscit=EMA` se clasifican como examen medico, reciben prioridad 1 y se distinguen visualmente sin exponer la observacion original.
- `Development` usa datos marcados como `DEMO` y **no abre conexiones de base de datos**.
- La configuracion base apunta al DSN ODBC `LOLCLI9000`, con sede fija `siscod=1` (Cuajone), mientras `Development` usa datos `Demo` para no consultar la base durante el desarrollo local.
- El adaptador ODBC usa solo lectura, rango diario indexable, limite de 100 filas, timeout de comando de 5 segundos y un unico worker de sondeo cada 3 segundos.
- Por aprobacion expresa del responsable, el modo productivo usa `pacnam` como identificador visible. No se muestran historias clinicas, prefacturas ni observaciones crudas.

## Ejecucion local

```powershell
dotnet restore
dotnet run --launch-profile http
```

Abrir `http://localhost:5280/turnos`. El build ejecuta `npm ci` y compila TypeScript a `wwwroot/js/tv.js`.

## Verificacion

```powershell
dotnet build --configuration Release
dotnet test .\tests\VisorTurnos.UnitTests\VisorTurnos.UnitTests.csproj --configuration Release

Set-Location ClientApp
npm run typecheck
npm run build
```

## Operacion ODBC

La activacion de Cuajone fue validada con [docs/data-discovery.md](docs/data-discovery.md) y registrada en [docs/decisions.md](docs/decisions.md). La cadena usa el DSN y autenticacion integrada autorizados; nunca debe incluir usuario o contrasena.
