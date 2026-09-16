# Descubrimiento de datos LOLCLI

La construccion inicial no accedio a ninguna base. Posteriormente se valido el esquema mediante el DSN `LOLCLI9000` con consultas de solo lectura y el adaptador quedo preparado para ODBC.

La regla `obscit=EMA` fue aprobada por el responsable del proyecto el 2026-09-16 a partir de una muestra visual. La instancia real confirmo `citas.obscit` como `varchar(80)`; la consulta solo materializa un booleano EMA y nunca entrega la observacion cruda.

La sede activa fue validada en `dbo.sistema`: `siscod=1` corresponde a `HOSPITAL SPCC CUAJONE`. Los conteos anonimizados del 2026-09-16 confirmaron `statte=S` como cierre y la presencia de `prfnum=0` en casos no cerrados; la politica activa interpreta cero como ausencia. La instancia no contiene `citas.tppcod`, aunque el diccionario historico la documenta; por ello el adaptador deja `PatientTypeCode` sin valor y no aplica preferencias basadas en ese campo.

Por autorizacion expresa del responsable del proyecto el 2026-09-16, el modo productivo usa `pacnam` como identificador visible. Esta decision debe revisarse si cambia el alcance publico o la politica de privacidad.

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
SELECT tppcod, tppdes FROM dbo.tipo_paciente ORDER BY tppcod;

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
- `BusinessRules:PublicIdentifierMode` aprobado; `Invnum` solo si el negocio confirma unicidad diaria y que el paciente conoce el valor.
- DSN de 64 bits y cadena con autenticacion integrada, sin secretos.
- Identidad efectiva de IIS con permisos de lectura limitados.
- Prioridades y tolerancias pueden permanecer sin configurar; en ese caso la aplicacion usa orden cronologico estable y no inventa preferencias.
