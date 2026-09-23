# AGENTS.md - Visor publico de turnos LOLCLI

## 1. Proposito de este documento

Este archivo contiene las instrucciones obligatorias para cualquier agente o desarrollador que trabaje en el proyecto. Debe leerse completo antes de proponer, generar o modificar codigo.

Si una instruccion de este archivo entra en conflicto con una indicacion explicita del responsable del proyecto, se debe detener el trabajo, explicar el conflicto y solicitar una decision. No se deben inventar reglas clinicas, codigos de prioridad ni significados de datos que no hayan sido validados.

## 2. Objetivo del producto

Construir una aplicacion web de solo visualizacion para mostrar turnos de atencion medica en una television de sala de espera.

El producto no es un dashboard administrativo. Es una pantalla publica, de lectura a distancia, que debe:

- mostrar los turnos relevantes de la sede configurada;
- distinguir pacientes en espera, llamados/en atencion y casos cerrados;
- reflejar cambios con baja latencia sin que el televisor consulte directamente SQL Server;
- respetar las prioridades, atencion preferencial y tolerancias definidas por el negocio;
- evitar exponer datos personales innecesarios;
- funcionar de forma continua y recuperarse de desconexiones;
- consultar la base LOLCLI exclusivamente en modo lectura.

## 3. Decision de arquitectura

### 3.1 Stack aprobado

- ASP.NET Core Web App con Razor Pages.
- SignalR en el servidor ASP.NET Core.
- Cliente TypeScript con `@microsoft/signalr`.
- CSS propio, optimizado para television 16:9 y resolucion objetivo 1920x1080.
- Acceso a SQL Server mediante ODBC si esa es la interfaz autorizada por TI.
- ADO.NET (`System.Data.Odbc`) para las consultas del MVP.
- IIS como host de produccion en Windows Server.
- Pruebas unitarias con xUnit.
- Pruebas de interfaz/recuperacion con Playwright cuando el entorno lo permita.

Usar la version LTS de .NET aprobada y soportada por TI. No actualizar la version objetivo por iniciativa propia.

### 3.2 Por que no Blazor

No usar Blazor para el MVP. Blazor Server mantiene un circuito de interfaz por cliente y utiliza SignalR internamente. Ese modelo es util para aplicaciones interactivas con muchos componentes, formularios y estado de usuario, pero agrega complejidad innecesaria para una pantalla publica de una sola pagina.

La pantalla de television debe ser HTML/CSS simple y resistente. TypeScript recibe snapshots por SignalR, actualiza el DOM y conserva una copia local del ultimo snapshot valido. Si SignalR falla, la pantalla debe continuar visible y reconectarse.

Reevaluar Blazor solo si el alcance futuro incorpora flujos interactivos complejos que justifiquen componentes con estado. No migrar por preferencia personal.

### 3.3 SignalR no sustituye el acceso a datos

LOLCLI es un sistema externo y, con la informacion disponible, no emite eventos para esta aplicacion. SignalR solo transporta cambios desde el servidor web hacia los televisores.

Flujo esperado:

1. `TurnosPollingWorker` ejecuta una consulta breve de solo lectura cada intervalo configurado.
2. `ITurnosRepository` abre la conexion, ejecuta la consulta parametrizada, materializa el resultado y cierra/dispone la conexion inmediatamente.
3. `TurnosSnapshotBuilder` normaliza estados y aplica reglas de ordenamiento validadas.
4. `TurnosChangeDetector` calcula una huella estable del snapshot visible.
5. Solo cuando la huella cambia, el servidor publica `TurnosActualizados` mediante `IHubContext<TurnosHub>`.
6. El cliente TypeScript reemplaza el estado visible de forma atomica.
7. Al cargar o reconectarse, el cliente solicita siempre un snapshot completo; no depende de haber recibido todos los eventos previos.

No implementar una conexion SQL permanente, un lector abierto, triggers, CDC, Service Broker ni cambios en la base sin autorizacion escrita del DBA.

## 4. Alcance funcional

### 4.1 Pantalla publica

La ruta publica principal sera `/turnos` y podra configurarse como pagina inicial.

La interfaz debe mostrar como maximo la cantidad de filas que quepa sin desplazamiento. El valor inicial recomendado es entre 5 y 8 filas, configurable despues de probar en la television real.

Contenido minimo:

- titulo `TURNOS DE ATENCION`;
- nombre fijo de la sede configurada, por ejemplo `Centro Medico Cuajone`;
- fecha y hora local;
- identificador publico del turno o nombre anonimizado, segun decision de negocio;
- consultorio;
- estado visual;
- franja destacada para el turno que acaba de ser llamado;
- indicador discreto de conexion/desactualizacion, sin mostrar detalles tecnicos.

No incluir:

- login, avatar o nombre de usuario;
- selector de sede;
- menus de navegacion, filtros, buscador o paginacion;
- graficos, KPI o tarjetas de dashboard;
- controles administrativos;
- numero de historia, telefono, plan, datos de pago u otros datos sensibles;
- trazas, mensajes SQL o errores internos.

### 4.2 Sede fija por despliegue

Aunque la base contiene registros de Cuajone, Ilo y Toquepala, el televisor no debe permitir elegir sede. Cada despliegue recibe una configuracion fija:

- `Site:Code`: valor validado de `citas.siscod`.
- `Site:DisplayName`: texto publico mostrado en pantalla.
- `Site:TimeZone`: zona horaria IANA o Windows aprobada para el servidor.

`Site:Code` es obligatorio. Si falta o es invalido, la aplicacion debe fallar al iniciar y no debe consultar turnos de todas las sedes.

### 4.3 Privacidad

La politica predeterminada es no mostrar nombres completos.

Opciones aceptables, en orden de preferencia:

1. codigo de turno existente y conocido por el paciente;
2. identificador publico emitido por el proceso de admision;
3. nombre anonimizado, solo si el negocio lo aprueba formalmente.

No convertir `pachis` (numero de historia) en identificador publico. No usar `invnum` o los ultimos digitos sin validar unicidad diaria y sin asegurar que el paciente conozca ese codigo. No inventar codigos como `A-018` solamente para que la interfaz se vea bien.

Los logs no deben contener `pacnam`, `pachis`, telefonos, observaciones ni informacion medica.

## 5. Fuente de datos confirmada

El diccionario entregado es un escaneo historico y puede diferir de la instancia real. Toda consulta debe validarse primero con metadatos de solo lectura (`INFORMATION_SCHEMA`, `sys.columns`) y con muestras anonimizadas.

### 5.1 Tabla `citas`

Campos relevantes confirmados por el diccionario:

| Campo | Tipo documentado | Uso esperado |
| --- | --- | --- |
| `medcod` | `varchar(4)` | Codigo del medico. Parte de la clave documentada. |
| `codcon` | `varchar(4)` | Codigo del consultorio. |
| `citdat` | `datetime` | Fecha y hora programada de la cita. |
| `statte` | `varchar(2)` | Estado de atencion. Valores reales por validar. |
| `tcicod` | `varchar(2)` | Tipo de citado; posible insumo de prioridad, no confirmado. |
| `pachis` | `varchar(7)` | Numero de historia. Dato sensible, no mostrar. |
| `obscit` | `varchar(20)` | Observacion. No mostrar ni registrar por defecto. |
| `pacnam` | `varchar(30)` | Nombre del paciente. Aplicar politica de privacidad. |
| `prfnum` | `int` | Numero de prefactura. `NULL`/`0` requiere validacion. |
| `invnum` | `int` | Correlativo de origen e indice unico documentado. |
| `tppcod` | `varchar(2)` | Tipo de paciente; posible insumo de preferencia, no confirmado. |
| `usecod` | `int` | Codigo de usuario. No es necesario para la pantalla. |
| `siscod` | `int` | Establecimiento/sede; filtro obligatorio. |
| `cithll` | `datetime` | Hora de llegada a la cita. |

La clave primaria documentada para `citas` es `(medcod, citdat)` y existe un indice unico documentado en `invnum`. Confirmar ambas condiciones en produccion antes de asumirlas.

### 5.2 Tablas relacionadas confirmadas

- `medicos`: `medcod`, `mednam`, `sercod`, `medsta`, `codcon`, entre otros.
- `consultorios`: `codcon`, `descon`.
- `tipo_citado`: `tcicod`, `tcides`.
- `tipo_paciente`: `tppcod`, `tppdes`.
- `sistema`: `siscod`, `sistit` y configuracion del establecimiento.
- `turnos`: horario configurado del medico; no representa por si mismo la cola diaria.
- `emergencia`: flujo separado con `tipeme`, `pachis`, `pacnam`, `medcod`, `contip`, `prfinum`, `stdeme`, `emeing` y otros campos.

No unir la tabla `emergencia` a la cola de citas hasta que el negocio confirme si ambos flujos deben aparecer en la misma television y como se evita duplicar pacientes.

### 5.3 Consulta base orientativa

La consulta real debe usar nombres de esquema confirmados. Con ODBC, los parametros `?` son posicionales y deben agregarse exactamente en el orden en que aparecen.

```sql
SELECT
    c.invnum,
    c.medcod,
    c.codcon,
    c.citdat,
    c.cithll,
    c.statte,
    c.tcicod,
    c.tppcod,
    c.prfnum,
    c.pacnam,
    m.mednam,
    co.descon
FROM dbo.citas AS c
LEFT JOIN dbo.medicos AS m
    ON m.medcod = c.medcod
LEFT JOIN dbo.consultorios AS co
    ON co.codcon = c.codcon
WHERE c.siscod = ?
  AND c.citdat >= ?
  AND c.citdat < ?
ORDER BY c.citdat, c.invnum;
```

El rango de fecha debe ser semiabierto `[inicioDelDia, inicioDelDiaSiguiente)` para conservar el uso de indices. No aplicar funciones como `CAST(citdat AS date)` sobre la columna en el `WHERE`.

Seleccionar solo columnas necesarias. Nunca usar `SELECT *`.

## 6. Modelo de estados

Las reglas proporcionadas por el negocio son:

- al iniciar la atencion, el medico activa una opcion que crea `prfnum`, pero el caso aun no esta cerrado;
- al guardar/cerrar, `statte` cambia de `N` a `S`.

Implementar la normalizacion con esta precedencia, una vez validados los valores reales:

1. `Cerrado`: `statte == "S"`.
2. `EnAtencion`: no esta cerrado y `prfnum` tiene un numero valido.
3. `EnEspera`: llego, no esta cerrado y no tiene prefactura.
4. `PendienteLlegada`: no existe `cithll` y la cita aun es vigente.
5. `Desconocido`: combinacion no reconocida; no adivinar.

`prfnum` es entero. La aplicacion debe estudiar si la instancia usa `NULL`, `0` o ambos como ausencia. Encapsular la regla en `PrefacturaPolicy`, no repetir comprobaciones en controladores o TypeScript.

La pantalla normal no debe mostrar casos cerrados. Se puede conservar un caso recien cerrado durante un periodo breve solo si el negocio lo solicita.

El evento de transicion `EnEspera -> EnAtencion` puede activar el aviso visual/sonoro `Pase, por favor`. No reproducir el aviso repetidamente al reconectar o recargar: el servidor debe incluir un identificador de version/evento y el cliente debe recordar el ultimo anuncio procesado durante la sesion.

## 7. Prioridades, preferencia y tolerancia

### 7.1 Regla provisional

La prioridad general informada es:

1. emergencia;
2. trabajadores de amanecida, aparentemente identificados con codigos como `A` o `B`;
3. trabajadores con cita por ventanilla durante horario de trabajo.

La fuente exacta y los valores de esos codigos no estan confirmados. No codificar `A`/`B` hasta comprobar en que campo aparecen y que significan en cada sede.

La regla de ordenamiento propuesta es una politica configurable:

```text
PriorityTier asc
IsPreferential desc
EligibilityTime asc
ArrivalTime asc
ScheduledTime asc
StableId asc
```

La emergencia siempre prevalece. La posicion exacta de la atencion preferencial respecto a amanecida y citas normales debe ser aprobada por el responsable medico.

### 7.2 Descubrimiento obligatorio

Antes de implementar prioridades, ejecutar consultas de solo lectura para identificar:

- valores y descripciones de `tipo_citado`;
- valores y descripciones de `tipo_paciente`;
- distribucion de `statte` por sede y fecha;
- frecuencia de `prfnum IS NULL`, `prfnum = 0` y `prfnum > 0`;
- relacion entre `cithll`, `citdat`, `statte` y `prfnum`;
- campo real que identifica amanecida;
- campo real que identifica atencion preferencial;
- forma en que emergencias deben integrarse a la cola.

Usar conteos agregados y codigos; evitar extraer nombres durante el descubrimiento.

### 7.3 Tolerancia

No fijar minutos arbitrarios en codigo. Configurar, despues de aprobacion:

- `Queue:EarlyArrivalMinutes`;
- `Queue:LateToleranceMinutes`;
- `Queue:CalledDisplaySeconds`;
- `Queue:ClosedRetentionSeconds`;
- comportamiento cuando el paciente excede la tolerancia.

La tolerancia debe basarse en `citdat` y/o `cithll` segun la definicion formal del negocio. Un paciente fuera de tolerancia no debe desaparecer o perder prioridad sin una regla aprobada.

## 8. Contratos internos

### 8.1 DTO publico

El navegador no debe recibir filas crudas de LOLCLI. Usar un DTO minimo:

```csharp
public sealed record TurnoPublicoDto(
    string PublicId,
    string Consultorio,
    string Estado,
    int PriorityTier,
    bool IsPreferential,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? ArrivedAt,
    bool ShouldAnnounce);
```

No incluir `pachis`, `pacnam` completo, `prfnum`, observaciones, montos, plan, seguro ni usuario en el DTO publico.

### 8.2 Snapshot

```csharp
public sealed record TurnosSnapshotDto(
    long Version,
    DateTimeOffset GeneratedAt,
    string SiteDisplayName,
    string Status,
    IReadOnlyList<TurnoPublicoDto> Items);
```

`Version` debe aumentar cuando cambia el contenido visible. `Status` puede ser `live`, `stale` o `unavailable`.

### 8.3 SignalR

- Hub: `/hubs/turnos`.
- Metodo servidor a cliente: `TurnosActualizados`.
- Carga: snapshot completo, no deltas parciales en el MVP.
- No exponer metodos de escritura en el hub.
- Limitar el tamano del mensaje y la cantidad de filas.
- Enviar solo cuando cambie la huella visible o cuando cambie el estado de salud.

El cliente TypeScript debe:

- usar `withAutomaticReconnect` con retrasos acotados;
- obtener snapshot inicial mediante `GET /api/turnos/actuales`;
- volver a obtener snapshot al reconectarse;
- ignorar versiones anteriores o repetidas;
- conservar el ultimo snapshot valido si se pierde la red;
- marcar `stale` cuando `GeneratedAt` exceda el umbral configurado;
- no limpiar la pantalla por un error transitorio;
- no generar HTML con cadenas sin escapar;
- actualizar el DOM en una sola operacion visible para evitar parpadeos.

### 8.4 Endpoint inicial

`GET /api/turnos/actuales` devuelve el ultimo snapshot en memoria. No debe ejecutar una consulta SQL por cada televisor. El `BackgroundService` es el unico responsable del sondeo periodico y publica el snapshot en un almacen singleton thread-safe.

## 9. Acceso a SQL Server y prevencion de bloqueos

Reglas obligatorias:

- acceso exclusivamente `SELECT`;
- no ejecutar `INSERT`, `UPDATE`, `DELETE`, `MERGE`, DDL ni procedimientos que escriban;
- no mantener `OdbcConnection`, `OdbcCommand` u `OdbcDataReader` como singleton;
- abrir la conexion dentro del ciclo, materializar resultados y disponerla con `using`/`await using` cuando la API lo permita;
- nunca dejar un `DataReader` abierto entre ciclos;
- no iniciar transacciones para la lectura periodica;
- usar `CommandTimeout` corto y cancelacion;
- impedir ciclos de sondeo superpuestos;
- si una consulta tarda mas que el intervalo, esperar a que termine o sea cancelada antes de iniciar otra;
- aplicar backoff tras errores consecutivos;
- no usar `WITH (NOLOCK)` como solucion automatica: puede mostrar datos sucios, duplicados o ausentes;
- preferir `READ_COMMITTED_SNAPSHOT` solo si el DBA confirma que ya esta habilitado o autoriza el cambio;
- solicitar un indice o vista al DBA si el plan de ejecucion lo exige; la aplicacion no modifica la base;
- usar filtro obligatorio por sede y rango de fecha;
- establecer un limite defensivo de filas;
- medir duracion y cantidad de filas sin registrar PII.

Valores iniciales sugeridos, todos configurables:

- intervalo normal de sondeo: 3 segundos;
- timeout de conexion: 3 a 5 segundos;
- timeout de comando: 5 segundos;
- maximo de filas consultadas: 100;
- maximo de filas visibles: 8;
- estado `stale`: 15 segundos sin snapshot valido;
- backoff por error: 3, 5, 10, 20 y maximo 30 segundos.

Estos valores deben probarse con el DBA y ajustarse segun carga. No prometer tiempo real estricto: es actualizacion casi en tiempo real, limitada por el intervalo de sondeo y la respuesta de LOLCLI.

## 10. Autenticacion Windows y ODBC

### 10.1 Separacion de identidades

La pagina publica no requiere login. Eso no significa que SQL Server sea anonimo.

En produccion, la conexion integrada usa la identidad del proceso de IIS, normalmente la identidad del Application Pool. El navegador de la television nunca recibe credenciales ni accede directamente a SQL Server.

La cadena de conexion no debe incluir usuario ni contrasena. Debe usar el DSN autorizado o parametros de autenticacion integrada aprobados por TI, por ejemplo `Trusted_Connection=Yes` cuando el controlador y la configuracion lo soporten.

### 10.2 Cuenta del Dr. Zambrano

Existe el requerimiento actual de usar la cuenta Windows del Dr. Zambrano, con permisos de solo lectura. Antes del despliegue se debe confirmar:

- si es cuenta local o de dominio;
- en que equipo existe;
- como la reconoce SQL Server;
- si SQL Server y IIS estan en equipos diferentes;
- politica de cambio, vencimiento y bloqueo de contrasena;
- autorizacion para ejecutar un Application Pool con esa identidad.

Una cuenta local en el servidor IIS no puede asumirse como identidad valida en un SQL Server remoto. El uso de cuentas locales coincidentes por nombre/contrasena es fragil y no debe adoptarse sin aprobacion de TI.

Recomendacion de produccion: solicitar una cuenta de servicio de dominio o gMSA exclusiva para el visor, con permisos `SELECT` solo sobre una vista o las tablas requeridas. Si TI obliga temporalmente a usar la cuenta personal, documentar el riesgo, configurar la identidad del Application Pool fuera del repositorio y definir un procedimiento de rotacion.

No almacenar contrasenas en:

- `appsettings.json`;
- codigo fuente;
- variables TypeScript;
- repositorio Git;
- JavaScript servido al navegador;
- logs o capturas.

## 11. Configuracion

La configuracion no secreta puede vivir en `appsettings.json`. Los valores de produccion se suministran por `.env` (configuracion no secreta por sede versionada en `deploy/sites/<sede>.env` y aplicada en el servidor), variables de entorno o configuracion protegida del servidor. No usar `appsettings.Production.json`.

Estructura esperada:

```json
{
  "Site": {
    "Code": 0,
    "DisplayName": "Centro Medico Cuajone",
    "TimeZone": "SA Pacific Standard Time"
  },
  "Queue": {
    "PollingSeconds": 3,
    "CommandTimeoutSeconds": 5,
    "StaleAfterSeconds": 15,
    "MaxQueryRows": 100,
    "MaxVisibleRows": 8,
    "EarlyArrivalMinutes": 0,
    "LateToleranceMinutes": 0,
    "CalledDisplaySeconds": 20,
    "ClosedRetentionSeconds": 0
  },
  "ConnectionStrings": {
    "LolcliOdbc": "DSN=LOLCLI;Trusted_Connection=Yes;"
  }
}
```

Los ceros de sede y tolerancia son marcadores invalidos o pendientes, no valores de negocio. La aplicacion debe validar opciones al inicio y fallar con un mensaje tecnico en el log del servidor, nunca en la TV.

Los codigos de prioridad validados deben vivir en una seccion `PriorityRules` o en una fuente administrada, no dispersos como literales.

## 12. Estructura recomendada del repositorio

```text
VisorTurnos.sln
src/
  VisorTurnos.Web/
    ClientApp/
      src/
        tv.ts
      package.json
      tsconfig.json
    Data/
      ITurnosRepository.cs
      OdbcTurnosRepository.cs
    Domain/
      TurnoRaw.cs
      TurnoPublicoDto.cs
      TurnosSnapshotDto.cs
      TurnoStatus.cs
      PriorityPolicy.cs
      TolerancePolicy.cs
    Hubs/
      TurnosHub.cs
    Pages/
      Turnos.cshtml
      Turnos.cshtml.cs
    Services/
      TurnosPollingWorker.cs
      TurnosSnapshotStore.cs
      TurnosSnapshotBuilder.cs
      TurnosChangeDetector.cs
    Options/
      SiteOptions.cs
      QueueOptions.cs
      PriorityOptions.cs
    wwwroot/
      css/tv.css
      js/tv.js
    Program.cs
tests/
  VisorTurnos.UnitTests/
  VisorTurnos.IntegrationTests/
docs/
  data-discovery.md
  deployment-iis.md
  decisions.md
```

TypeScript debe compilarse durante build/publish. Produccion sirve JavaScript estatico y no requiere Node.js en ejecucion.

Mantener `strict: true`. Evitar frameworks de frontend adicionales salvo que una necesidad concreta lo justifique.

## 13. Experiencia de television

### 13.1 Diseno

- lienzo fluido 16:9 sin scroll;
- tipografia sans-serif de alto contraste;
- tamanos verificados fisicamente a 3-5 metros;
- areas grandes, sin texto denso;
- no depender solo del color: combinar texto, icono y contraste;
- respetar `prefers-reduced-motion`;
- animaciones breves y no distractoras;
- hora actualizada en el cliente, pero `GeneratedAt` viene del servidor;
- evitar que filas cambien de posicion continuamente si el estado/prioridad no cambio.

### 13.2 Sonido

El sonido es opcional y requiere validacion en la TV/navegador, porque la reproduccion automatica puede estar bloqueada. Si se habilita:

- anunciar una sola vez por version/evento;
- incluir alternativa visual equivalente;
- disponer de volumen y texto aprobados;
- no pronunciar datos sensibles;
- no repetir anuncios durante reconexiones.

### 13.3 Modo kiosco

El televisor o mini-PC debe abrir Edge/Chrome en modo kiosco con la URL interna. La aplicacion debe evitar cachear `index`/HTML de forma que impida despliegues, mientras que CSS/JS versionados pueden usar cache largo.

## 14. Manejo de fallos

- Si SQL falla temporalmente, conservar el ultimo snapshot valido.
- Mostrar un indicador discreto `Informacion en actualizacion` cuando el snapshot este vencido.
- No mostrar pantalla vacia salvo que nunca haya existido un snapshot valido; en ese caso mostrar `No se pudo actualizar la informacion` sin detalles.
- Registrar excepcion, duracion, tipo de fallo y contador, sin PII ni cadena de conexion.
- Aplicar health checks separados para proceso web y fuente SQL.
- SignalR debe reconectar; el cliente debe resincronizar con snapshot completo.
- La caida de un televisor no debe crear recursos persistentes en el servidor.
- La caida de SignalR no debe desencadenar sondeo SQL por cada cliente.

## 15. Seguridad

- endpoints de escritura prohibidos en el MVP;
- acceso restringido a la red corporativa mediante firewall/IIS;
- HTTPS aunque la red sea interna, sujeto a certificados corporativos;
- cabeceras de seguridad y CSP compatibles con SignalR;
- dependencias NuGet/npm fijadas y revisadas;
- no habilitar Swagger en produccion si no hay una API que lo necesite;
- no exponer stack traces ni Developer Exception Page en produccion;
- logs con acceso restringido;
- no registrar cuerpos de respuesta con datos de turnos;
- validar opciones al inicio;
- parametrizar toda consulta;
- no aceptar SQL, nombres de tabla, sede o codigos desde query string del televisor.

## 16. Observabilidad

Metricas y logs permitidos:

- duracion de consulta;
- filas leidas;
- exito/error del sondeo;
- antiguedad del ultimo snapshot;
- numero de conexiones SignalR;
- reconexiones;
- broadcasts emitidos;
- version actual;
- uso de memoria y salud del proceso.

Nunca usar nombres de pacientes como claves de log. Para correlacion tecnica usar un identificador interno no reversible o la version del snapshot.

## 17. Pruebas obligatorias

### 17.1 Unitarias

- `statte=S` prevalece sobre `prfnum` presente;
- `prfnum` presente y caso no cerrado produce `EnAtencion`;
- `prfnum NULL/0` se interpreta segun politica validada;
- llegada y tolerancia en limites exactos;
- orden estable con empates;
- emergencia prevalece;
- preferencia se aplica solo donde la regla la permita;
- datos desconocidos producen `Desconocido`, no una clasificacion inventada;
- anonimizacion no filtra nombre completo ni historia;
- la huella no cambia por orden no significativo;
- una transicion genera como maximo un anuncio.

### 17.2 Integracion

- consulta ODBC parametrizada contra una base de prueba o fake controlado;
- cancelacion y timeout;
- cierre de conexion/lector incluso con excepcion;
- no hay ciclos superpuestos;
- API inicial y evento SignalR entregan el mismo contrato;
- reconexion recupera el snapshot mas reciente;
- varias TVs no multiplican las consultas SQL.

### 17.3 Interfaz

- 1920x1080 y resolucion real del televisor;
- legibilidad a distancia;
- sin scroll con el maximo de filas;
- desconexion/reconexion de red;
- servidor reiniciado;
- SQL temporalmente inaccesible;
- textos largos de consultorio;
- reloj y cambio de dia;
- modo kiosco durante una prueba prolongada;
- no se muestran PII o controles administrativos.

## 18. Despliegue IIS

Precondiciones:

1. servidor Windows autorizado;
2. ASP.NET Core Hosting Bundle de la version LTS objetivo;
3. controlador ODBC para SQL Server y DSN configurado para la arquitectura correcta;
4. certificado HTTPS corporativo;
5. identidad del Application Pool validada contra SQL Server;
6. permisos de archivos minimos;
7. regla de firewall desde IIS hacia SQL Server y desde TV hacia IIS;
8. cuenta con `SELECT` solamente;
9. sede y zona horaria validadas;
10. prueba coordinada con LOLCLI.

Configuracion base del Application Pool:

- `No Managed Code`;
- identidad aprobada por TI;
- `Start Mode = AlwaysRunning` si la politica lo permite;
- precarga/aplicacion inicial si esta disponible;
- reciclaje planificado fuera del horario critico;
- una sola instancia del worker por despliegue, salvo que se implemente coordinacion distribuida.

Si se despliegan varias instancias web, cada una ejecutaria su propio `BackgroundService`. Antes de escalar horizontalmente se debe mover el sondeo a un worker unico o implementar liderazgo/distribucion para no duplicar consultas.

No realizar el despliegue productivo hasta resolver la viabilidad de la cuenta local del Dr. Zambrano.

## 19. Flujo de trabajo para agentes

Antes de codificar:

1. leer este archivo;
2. inspeccionar el repositorio y preservar cambios existentes;
3. revisar `docs/decisions.md` y `docs/data-discovery.md` si existen;
4. identificar supuestos no validados;
5. evitar cambios fuera del alcance pedido.

Al modificar codigo:

- mantener responsabilidades separadas;
- preferir funciones puras para estado, prioridad y tolerancia;
- no duplicar reglas entre C# y TypeScript: el servidor decide el dominio y el cliente solo presenta;
- no introducir EF Core, Blazor, Redis, colas, Docker o un framework JS sin una decision registrada;
- no ejecutar migraciones ni scripts de escritura contra LOLCLI;
- usar nombres claros en espanol o ingles de forma consistente con el codigo existente;
- documentar decisiones no obvias;
- agregar o actualizar pruebas;
- mantener el contrato publico pequeno y versionable.

Antes de terminar:

- compilar backend y TypeScript;
- ejecutar pruebas;
- comprobar que no se incluyeron secretos;
- revisar que no se expone PII;
- comprobar que no hay conexiones/lectores sin disponer;
- actualizar documentacion relevante;
- informar supuestos pendientes y pruebas realizadas.

## 20. Comandos esperados

Ajustar los paths si la solucion real difiere:

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release

cd src/VisorTurnos.Web/ClientApp
npm ci
npm run typecheck
npm run build

cd ../../..
dotnet publish src/VisorTurnos.Web/VisorTurnos.Web.csproj `
  --configuration Release `
  --output ./artifacts/publish
```

No ejecutar comandos de despliegue a produccion sin autorizacion explicita.

## 21. Criterios de aceptacion del MVP

El MVP se considera aceptable cuando:

- muestra solo la sede configurada y no ofrece selector;
- funciona sin login en la television;
- refleja un cambio validado de LOLCLI dentro del SLA acordado;
- no consulta SQL por cada TV ni mantiene consultas abiertas;
- diferencia correctamente espera, atencion y cierre usando reglas validadas;
- aplica prioridades y tolerancias aprobadas;
- no muestra PII no autorizada;
- se recupera de desconexion de SignalR, reinicio web y fallo SQL temporal;
- mantiene el ultimo snapshot y avisa si esta desactualizado;
- opera en modo kiosco sin scroll ni interaccion;
- la cuenta SQL efectiva tiene solo permisos de lectura;
- las consultas estan parametrizadas, acotadas por sede/fecha y revisadas por el DBA;
- pruebas unitarias e integracion pasan;
- existe una guia de despliegue y rollback.

## 22. Decisiones pendientes obligatorias

No cerrar estas decisiones por inferencia:

1. valores exactos de `statte` y sus transiciones;
2. ausencia de prefactura como `NULL`, `0` o ambos;
3. campo y valores reales para amanecida (`A`, `B` u otros);
4. fuente de emergencia y posible union con `citas`;
5. campo/regla de atencion preferencial;
6. minutos y efecto de tolerancia;
7. identificador publico que reconoce el paciente;
8. si se permite algun grado de nombre anonimizado;
9. tiempo de permanencia de casos cerrados/llamados;
10. intervalo y SLA de actualizacion;
11. codigo `siscod` exacto de Cuajone, Ilo y Toquepala;
12. version LTS de .NET autorizada;
13. controlador/DSN ODBC autorizado;
14. topologia IIS-SQL y cuenta Windows efectiva;
15. aprobacion para usar cuenta personal o provision de cuenta de servicio;
16. texto, sonido y volumen del llamado;
17. horario operativo y comportamiento al cambio de dia.

Registrar cada respuesta aprobada en `docs/decisions.md`, con fecha y responsable. Hasta entonces, usar configuracion invalida que obligue a resolver la decision o implementar un modo seguro que no clasifique incorrectamente.
