# Visor publico de turnos LOLCLI

Aplicacion ASP.NET Core Razor Pages para una pantalla de sala de espera. El servidor mantiene un unico snapshot en memoria, sondea una fuente configurable y publica cambios por SignalR. Los televisores nunca acceden a SQL Server.

## Estado actual

- La interfaz 16:9, la API, SignalR, el almacenamiento de snapshots y el worker estan implementados.
- La pantalla separa una vista rotativa por consultorio y medico de una vista general fija; esta ultima muestra el medico y el consultorio de destino.
- Las citas con `obscit=EMA` se clasifican como examen medico, reciben prioridad 1 y se distinguen visualmente sin exponer la observacion original.
- Toda ejecucion usa exclusivamente el DSN ODBC `LOLCLI9000`, con sede fija `siscod=1` (Cuajone). Si la fuente no esta disponible, la pantalla conserva el ultimo snapshot real o indica que no puede actualizarse; nunca publica datos ficticios.
- El adaptador ODBC usa solo lectura, rango diario indexable, limite de 100 filas, timeout de comando de 5 segundos y un unico worker de sondeo cada 3 segundos.
- Cada llamado consume un `am_consulta.numcon` nuevo asociado por `invnum`; no espera la prefactura. `P/S` libera el consultorio, pero el siguiente paciente requiere que el médico lo habilite nuevamente en LOLCLI. La prefactura se conserva para el guardado/cierre del caso, no como requisito del aviso.
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
# Visor de turnos

## Snapshot local de Development

La copia local se encuentra en la instancia SQL Server LocalDB `VisorTurnosDevelopment` y solo contiene las tablas mínimas de la consulta diaria: `citas`, `medicos`, `consultorios` y `am_consulta`. Si la copia fue creada antes de incorporar `numcon` e `invnum` a `am_consulta`, vuelve a ejecutar el exportador antes de iniciar el simulador.

Para iniciarla de forma explícita desde PowerShell:

```powershell
dotnet run --launch-profile development-snapshot
```

En Visual Studio, selecciona el perfil **development-snapshot** antes de ejecutar. Ese perfil activa `DataSource:EnableDevelopmentSnapshot=true`; sin esa bandera, el modo Development falla al iniciar y no consulta LOLCLI accidentalmente.

Con el visor iniciado en ese perfil, abre [el simulador local](http://localhost:5280/simulador). Permite abrir el acto médico, guardar la consulta, cerrar la cita o restablecerla. Todas esas escrituras ocurren exclusivamente en la copia LocalDB.

Para renovar la copia con la jornada actual, ejecuta:

```powershell
dotnet run --project tools/VisorTurnos.Snapshot/VisorTurnos.Snapshot.csproj --configuration Release
```

Si un intento previo dejó el archivo `VisorTurnosDevelopment.mdf` sin registrar en LocalDB, el exportador lo adjunta de forma automática. No elimines el archivo manualmente.
