# Despliegue en IIS

Documento preparatorio. No se ha realizado ningun despliegue.

## Precondiciones

- Windows Server autorizado y Hosting Bundle de .NET LTS aprobado.
- IIS con certificado HTTPS corporativo.
- Driver ODBC y DSN de la misma arquitectura que el Application Pool.
- Identidad de servicio o gMSA validada contra SQL Server con `SELECT` minimo.
- Firewall de IIS a SQL Server y de los televisores a IIS.
- Decisiones de `docs/decisions.md` resueltas.

## Modelo de multiples sedes

Una sola base y un solo DSN (`LOLCLI9000`); cada despliegue varía únicamente
`Site:Code` (siscod), `Site:DisplayName` y `Site:TimeZone`. Se publica una vez y se
generan carpetas autocontenidas, una por sede, cada una con su propio `.env`
`deploy/sites/<sede>.env` + el `.env` local del servidor (cadena de conexion).

Las carpetas fisicas confirmadas en el servidor son:

| Sede | Puerto IIS | Carpeta fisica | Application Pool |
| --- | ---: | --- | --- |
| Cuajone | 8080 | `C:\inetpub\publish-cuajone` | `Visor Turnos Hospital Cuajone` |
| Ilo | 8081 | `C:\inetpub\publish-ilo` | `Visor Turnos Hospital Ilo` |
| Toquepala | 8082 | `C:\inetpub\publish-toquepala` | `Visor Turnos Hospital Toquepala` |

Cada sede es un **sitio IIS independiente** con su propio hostname (binding por host
header) y servido en la raiz de ese hostname. No se usan rutas virtuales largas; el
televisor apunta a la raiz de su hostname.

| Sitio IIS | Hostname | `Site:Code` | `Site:DisplayName` |
| --- | --- | --- | --- |
| Cuajone | `turnos-cuajone.hospital.local` | 1 | Hospital SPCC Cuajone |
| Ilo | `turnos-ilo.hospital.local` | 2 | Hospital SPCC Ilo |
| Toquepala | `turnos-toquepala.hospital.local` | 3 | Hospital SPCC Toquepala |

URLs de los televisores (raiz de cada hostname, sin rutas largas):

- Cuajone: `https://turnos-cuajone.hospital.local/turnos`
- Ilo: `https://turnos-ilo.hospital.local/turnos`
- Toquepala: `https://turnos-toquepala.hospital.local/turnos`

Para incorporar una sede nueva (p. ej. atencion virtual) basta agregar un fragmento en
`deploy/sites/`, una fila en el script, un hostname/sitio y un cert en IIS; no requiere cambios de codigo.

## DNS

Crear en el DNS corporativo un registro A por sede, apuntando todos a la IP fija del
servidor IIS. Los equipos (incluidas las televisores) los resuelven sin configuración local.

```
turnos-cuajone.hospital.local.   A   192.168.x.x
turnos-ilo.hospital.local.       A   192.168.x.x
turnos-toquepala.hospital.local. A   192.168.x.x
```

Reparto de puertos en la red interna (opcional si un solo servidor IIS atiende las tres):

- 443 (HTTPS) para las tres, distinguidas por host header.

El certificado HTTPS corporativo debe cubrir los tres hostnames (SAN) o usar un
comodín `*.hospital.local`.

## Configuracion

1. `appsettings.json` contiene la base compartida; los fragmentos por sede solo sobreescriben `Site`.
2. Configurar primero un `Site:Code` y nombre de sede validados; sin ellos la aplicacion falla al iniciar incluso con la fuente deshabilitada.
3. Configurar el DSN autorizado `LOLCLI9000`, las reglas aprobadas y la cadena ODBC integrada antes de iniciar la aplicacion.
4. Confirmar la conexion ODBC en la ventana coordinada con el DBA.
5. Configurar un Application Pool por sede como `No Managed Code`, una sola instancia y la identidad aprobada.
6. Verificar proceso en `/health/live`, fuente en `/health/ready`, pantalla en `/turnos` y contrato en `/api/turnos/actuales` sin exponer detalles internos.

### Cadena de conexion y config por sede fuera del repositorio

`ConnectionStrings:LolcliOdbc` **no** se define en `appsettings.json`. Toda la config
específica de sede (incluida la conexion) vive en el `.env` local de cada sitio IIS:

- `deploy/sites/<sede>.env` se versiona con la config no secreta de la sede
  (`Site__Code`, `Site__DisplayName`, `Site__TimeZone`) y se usa para crear el
  `.env` inicial en `C:\inetpub\publish-<sede>`.
- En el servidor, el `.env` de la carpeta fisica del sitio es **autoritativo**: se
  excluye de la limpieza previa. El despliegue actualiza exclusivamente
  `Site__Code`, `Site__DisplayName` y `Site__TimeZone` desde el fragmento versionado
  de la sede; conserva sin cambios `ConnectionStrings__LolcliOdbc` y las demas claves
  locales. Tambien puede definir la conexion como variable de entorno del Application
  Pool con el mismo nombre y valor.

El `.env` es local de cada servidor y queda ignorado por git. Nunca contiene usuario
ni contrasena: usa `Trusted_Connection` sobre el DSN:

```text
ConnectionStrings__LolcliOdbc=DSN=LOLCLI9000;Trusted_Connection=Yes;
```

Precedencia: variable de entorno real > `.env` > `appsettings.json`. Para cambiar la
config de una sede basta editar el `.env` del sitio y reciclar su App Pool.

## Publicacion

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\Publish-VisorTurnos.ps1 -Configuration Release
```

El script ejecuta `dotnet publish` directamente sobre las carpetas fisicas
`C:\inetpub\publish-<sede>`. El `.env` existente conserva la conexion y las claves
locales, mientras que las tres claves `Site__*` se sincronizan con la sede correcta.
Produccion no necesita Node.js en ejecucion:
el JavaScript compilado queda dentro del resultado publicado.

## Despliegue automatizado a produccion

`deploy/Deploy-Production.ps1` publica directamente en las carpetas fisicas de IIS
(`C:\inetpub\publish-cuajone`, `C:\inetpub\publish-ilo` y
`C:\inetpub\publish-toquepala`) con salvaguardas:

1. **Backup** de cada carpeta actual en `C:\inetpub\visor-turnos-backups\YYYYMMDD-HHMMSS-<sede>\`.
2. **app_offline.htm** se coloca antes de copiar, para que IIS detenga la app con gracia
   y las pantallas muestren "Actualizando informacion" en lugar de errores parciales.
3. **Publicacion directa** mediante `dotnet publish --output C:\inetpub\publish-<sede>`.
4. **Preservacion de la config del servidor**: el `.env` previo de cada aplicacion IIS
   se excluye de la limpieza. La conexion ODBC y las demas claves locales se conservan;
   solo se sincronizan `Site__Code`, `Site__DisplayName` y `Site__TimeZone` desde el
   fragmento correspondiente. En un deploy inicial el operador agrega
   `ConnectionStrings__LolcliOdbc` antes de activar.
5. **Reciclado del Application Pool real** (`Visor Turnos Hospital <Sede>`) mediante `appcmd`.
6. **Smoke test** sobre `127.0.0.1` y el puerto IIS de cada sede: 8080, 8081 o 8082.
   Espera hasta 60 segundos y reintenta `/health/ready` cada 3 segundos mientras el
   worker obtiene su primer snapshot de LOLCLI; después verifica `/turnos` (HTTP 200).
   Solo restaura el backup si la aplicación no queda lista dentro de ese plazo.

Uso en el servidor (donde se edita el codigo), publica y despliega todo:

Abra primero **PowerShell como administrador**. Pertenecer al grupo Administradores
no basta si la consola no fue elevada por UAC; sin elevacion Windows deniega la
escritura en `C:\inetpub` y el reciclado de los Application Pools.

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\Deploy-Production.ps1 -Sites cuajone,ilo,toquepala
```

Flujo habitual cuando se edita en el servidor:

```powershell
# 1. Editar codigo en el servidor.
# 2. Publicar + desplegar + verificar, todo en un paso:
powershell -ExecutionPolicy Bypass -File .\deploy\Deploy-Production.ps1
# 3. Si solo se necesita redeployar sin recompilar:
powershell -ExecutionPolicy Bypass -File .\deploy\Deploy-Production.ps1 -SkipPublish
# 4. Deploy de una sola sede:
powershell -ExecutionPolicy Bypass -File .\deploy\Deploy-Production.ps1 -Sites ilo
```

Opciones: `-WebRoot <ruta>` (por defecto `C:\inetpub`), `-BackupRoot <ruta>`,
`-AppPools "sede=nombre;sede=nombre"`, `-SitePorts "sede=puerto;sede=puerto"`,
`-SmokeTestScheme http|https`, `-SmokeTestHost <host>`, `-SkipIis` y
`-SkipSmokeTest`. La espera puede ajustarse con `-SmokeTestTimeoutSeconds` y
`-SmokeTestRetrySeconds`. El script exige administrador para reciclar pools; los backups
numerados por fecha constituyen el historial de despliegue.

### Prueba sin IIS

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\Deploy-Production.ps1 `
    -SkipPublish -SkipIis -SkipSmokeTest `
    -WebRoot C:\tmp\webroot -BackupRoot C:\tmp\visor-backups
```

Verificacion esperada: carpeta por sede con el contenido publicado, backup creado y
`.env` de la sede conservado (o sembrado en el deploy inicial).

## Prueba local de todas las sedes a la vez

Kestrel no permite que dos instancias compartan el mismo puerto, por lo que cada sede se
ejecuta en un puerto distinto, todos en la raiz `https://localhost:<puerto>/turnos`,
replicando el hostname separado que tendra cada sitio en IIS:

| Perfil | URL local | Puerto IIS equivalente |
| --- | --- | --- |
| `cuajone` | `https://localhost:7218/turnos` | 443 de `turnos-cuajone.*` |
| `ilo` | `https://localhost:7219/turnos` | 443 de `turnos-ilo.*` |
| `toquepala` | `https://localhost:7220/turnos` | 443 de `turnos-toquepala.*` |

```powershell
dotnet run --launch-profile cuajone
dotnet run --launch-profile ilo
dotnet run --launch-profile toquepala
```

## Rollback

Conservar el artefacto anterior, detener el Application Pool, restaurar el directorio anterior y arrancar el pool. Si existe duda sobre datos o permisos, detener el sitio hasta resolverla; la aplicacion no escribe en LOLCLI ni muestra datos demo.
