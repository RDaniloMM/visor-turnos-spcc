# Descubrimiento de datos LOLCLI

La construccion inicial no accedio a ninguna base. Posteriormente se valido el esquema mediante el DSN `LOLCLI9000` con consultas de solo lectura y el adaptador quedo preparado para ODBC.

La regla `obscit=EMA` fue aprobada por el responsable del proyecto el 2026-09-16 a partir de una muestra visual. La instancia real confirmo `citas.obscit` como `varchar(80)`; la consulta solo materializa un booleano EMA y nunca entrega la observacion cruda.

La sede activa fue validada en `dbo.sistema`: `siscod=1` corresponde a `HOSPITAL SPCC CUAJONE`. Los conteos anonimizados del 2026-09-16 confirmaron `statte=S` como cierre y la presencia de `prfnum=0` en casos no cerrados; la politica activa interpreta cero como ausencia. La instancia no contiene `citas.tppcod`, aunque el diccionario historico la documenta; por ello el adaptador deja `PatientTypeCode` sin valor y no aplica preferencias basadas en ese campo.

Por autorizacion expresa del responsable del proyecto el 2026-09-16, el modo productivo usa `pacnam` como identificador visible. Esta decision debe revisarse si cambia el alcance publico o la politica de privacidad.

El 2026-09-17 el responsable confirmó que la existencia de un acto médico abierto se reconoce por la relación `am_consulta.prfnum = citas.prfnum`. La consulta central obtiene, mediante una sola búsqueda acotada por cada cita de la jornada, `stacon`, `feccon`, `feccre` y `fecumv`; los conserva solo en la cola interna. `stacon` y las fechas no definen todavía una transición clínica. Antes de aumentar el límite de filas, el DBA debe confirmar un índice utilizable sobre `am_consulta.prfnum` (y preferiblemente la fecha usada para resolver duplicados).

El 2026-09-18 se verificó mediante muestras anonimizadas y acotadas a 100 citas de Cuajone que `statte=N`, prefactura válida y `am_consulta.stacon=T` coinciden con actos abiertos; los casos `statte=S` observados tenían `stacon=P`. La tabla dispone del índice no único `ind_am_consulta_prfnum` sobre `prfnum`. El responsable confirmó formalmente que `T` significa acto abierto y `P` acto guardado/cerrado. Por tanto, el adaptador solo reconoce como acto habilitante un registro `T`; no basta la existencia histórica de cualquier registro para la prefactura.

Una comprobación posterior del 2026-09-18 encontró, para el mismo `invnum` y `prfnum`, un `numcon` anterior en `P` y otro `numcon` posterior en `T`, mientras `citas.statte` permanecía en `S`. `am_consulta.numcon` es la clave primaria real. Esto confirma el flujo de reapertura: cada intento crea un `numcon` y el último acto prevalece sobre el estado histórico de la cita. La consulta relaciona la cita por `invnum`, selecciona `TOP (1) ORDER BY feccon DESC, numcon DESC` sin filtrar previamente por `stacon`, y solo después interpreta el cierre `P`.

La consulta posterior para buscar filas con `c.prfnum = 0` y `c.statte = 'S'` no devolvió resultados. Eso prueba que, en la muestra consultada, todos los casos cerrados ya tenían prefactura distinta de cero; no prueba el instante exacto en que LOLCLI la crea. Por ello el visor toma el `numcon` nuevo como señal de llamado, mientras que el simulador conserva una prefactura válida para reproducir el guardado/cierre del caso.

En la misma fecha se validó la prioridad por observación sin exponer su texto al navegador: `EMA` tiene tier 1 y puede entrar a la selección antes de su hora programada; los códigos de amanecida de una letra o letra más `1` tienen tier 2; el resto queda en el tier normal. La consulta conserva el límite de filas y ordena las filas elegidas por esas prioridades antes de la hora programada.

La consulta de producción conserva el límite de 100 filas y trabaja sobre una ventana horaria configurada. Desde el inicio del día hasta las 12:00 consulta la primera mitad; a partir de las 12:00 consulta la segunda mitad hasta finalizar la jornada. Así las citas de la tarde no quedan fuera por una acumulación de registros de la mañana.

## Protocolo previo

1. Obtener autorizacion del DBA y ejecutar con una identidad que solo tenga `SELECT`.
2. Confirmar servidor, base, esquema, controlador, arquitectura del DSN y sede de prueba.
3. Ejecutar primero metadatos y conteos; no exportar nombres, historias, observaciones ni datos medicos.
4. Registrar resultados anonimizados y aprobaciones en `docs/decisions.md`.
5. Revisar el plan estimado y el limite de 100 filas con el DBA.

## Consultas propuestas de solo lectura

Estas consultas son material de revision; no se ejecutan automaticamente.

```sql
SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('citas', 'consultorios', 'medicos', 'tipo_citado', 'tipo_paciente', 'sistema', 'emergencia')
ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION;

SELECT tcicod, tcides FROM dbo.tipo_citado ORDER BY tcicod;
SELECT statte, COUNT_BIG(*) AS cantidad
FROM dbo.citas
WHERE siscod = ? AND citdat >= ? AND citdat < ?
GROUP BY statte
ORDER BY statte;

SELECT
    CASE WHEN prfnum IS NULL THEN 'NULL'
         WHEN prfnum = 0 THEN 'CERO'
         ELSE 'POSITIVO' END AS grupo_prefactura,
    COUNT_BIG(*) AS cantidad
FROM dbo.citas
WHERE siscod = ? AND citdat >= ? AND citdat < ?
GROUP BY CASE WHEN prfnum IS NULL THEN 'NULL' WHEN prfnum = 0 THEN 'CERO' ELSE 'POSITIVO' END;

SELECT COUNT_BIG(*) AS total_citas,
       SUM(CASE WHEN LTRIM(RTRIM(obscit)) = ? THEN 1 ELSE 0 END) AS total_ema
FROM dbo.citas
WHERE siscod = ? AND citdat >= ? AND citdat < ?;

SELECT statte,
       CASE WHEN cithll IS NULL THEN 0 ELSE 1 END AS tiene_llegada,
       CASE WHEN prfnum IS NULL OR prfnum = 0 THEN 0 ELSE 1 END AS tiene_prefactura,
       COUNT_BIG(*) AS cantidad
FROM dbo.citas
WHERE siscod = ? AND citdat >= ? AND citdat < ?
GROUP BY statte,
         CASE WHEN cithll IS NULL THEN 0 ELSE 1 END,
         CASE WHEN prfnum IS NULL OR prfnum = 0 THEN 0 ELSE 1 END;
```

## Condiciones para activar la fuente

- `Site:Code` validado y mayor que cero.
- `BusinessRules:ClosedStatusCodes` aprobado.
- `PriorityRules:MedicalExamObservationCode` se mantiene en `EMA`; la consulta solo materializa un booleano derivado y nunca entrega `obscit` al dominio publico.
- `BusinessRules:PublicIdentifierMode=PatientName` esta aprobado para Cuajone; `Invnum` permanece deshabilitado porque no es un codigo conocido por el paciente.
- DSN de 64 bits y cadena con autenticacion integrada, sin secretos.
- Identidad efectiva de IIS con permisos de lectura limitados.
- Prioridades y tolerancias pueden permanecer sin configurar; en ese caso la aplicacion usa orden cronologico estable y no inventa preferencias.
