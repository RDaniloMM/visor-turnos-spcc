# Flujo operativo de llamados

Ultima revision: 2026-09-22.

## Principio rector

El visor no inicia actos médicos ni escribe en LOLCLI. El médico selecciona al paciente en LOLCLI y ese acto crea un registro en `am_consulta`. La aparición de un `numcon` nuevo es la señal que habilita un llamado únicamente si está relacionado por `invnum` con una cita programada de la sede y jornada actual en `citas`.

`prfnum` no es requisito para anunciar. El médico lo crea explícitamente en LOLCLI mediante **Crear prefactura** y **Aceptar**; esa operación actualiza `citas.prfnum`. Es un dato administrativo de la cita, pero no decide si la TV debe llamar al paciente ni prueba por sí solo que el acto o la cita estén cerrados.

## Identificadores y estados

| Dato | Uso en el visor |
| --- | --- |
| `citas.invnum` | Identifica de forma estable una cita y agrupa sus intentos. No se muestra al público. |
| `am_consulta.numcon` | Identifica un intento individual. Un `numcon` solo puede iniciar un ciclo de llamado. |
| `am_consulta.stacon = P` | Acto médico guardado. No basta por sí solo para afirmar que la cita terminó: se han observado combinaciones `P`/`N` con prefactura `0`. |
| `citas.statte = S` | Cita cerrada. Junto con el último acto en `P` confirma el cierre `P/S`. Si existe un `numcon` posterior aún no cerrado, ese acto nuevo prevalece temporalmente. |
| `citas.prfnum` | Valor de prefactura de la cita. LOLCLI lo crea al confirmar **Crear prefactura**; no se usa para anunciar ni se publica. |

La consulta toma únicamente el último acto de cada cita: `TOP (1)` ordenado por `feccon DESC, numcon DESC`, unido mediante `am_consulta.invnum = citas.invnum`.

## Secuencia normal

1. El médico revisa su lista de LOLCLI y decide qué paciente atender según la prioridad clínica.
2. La cita debe existir en `citas` para Cuajone y la jornada actual. Al seleccionar ese paciente, LOLCLI crea un nuevo `am_consulta.numcon`.
3. Al arrancar, el visor toma una línea base silenciosa de los `numcon` ya existentes. El sondeo central solo considera habilitante un identificador distinto detectado después de esa línea base, normalmente en un máximo de tres segundos.
4. Si no hay otro anuncio en curso, la TV muestra el banner, pronuncia el llamado y marca al paciente como `Llamando`.
5. A los 20 segundos se realiza una única repetición de voz.
6. A los 40 segundos termina banner y voz. Ese `numcon` queda consumido: no se reutiliza para un segundo ciclo.
7. El médico puede crear y aceptar una prefactura en LOLCLI; esto actualiza `citas.prfnum`, pero no es condición del aviso. En el flujo confirmado de cierre, el último acto pasa a `P` y la cita a `S`; la combinación `P/S` confirma el término. El visor no llama al siguiente por su cuenta.
8. Para llamar a otro paciente, el médico vuelve a seleccionarlo en LOLCLI, creando otro `numcon`. Si reabre una cita que ya tuvo un acto guardado sin ausencia documentada, el visor trata esa reapertura como ambigua y no la anuncia automáticamente: el paciente debe acercarse o tocar la puerta. El visor nunca reabre una cita por su cuenta.

## Finalización y nueva decisión médica

Al terminar los 40 segundos, el visor no determina ni muestra si el paciente asistió o estuvo ausente. Tampoco reencola, cierra ni crea registros en LOLCLI.

El médico decide el siguiente paso desde LOLCLI. Un nuevo `numcon` solo inicia otro ciclo automático si no existe un acto previo guardado de esa cita sin ausencia documentada. Abrir un acto médico no es un botón de rellamado: en el caso ambiguo, la pantalla queda silenciosa y el paciente debe acercarse o tocar la puerta. Un llamado automático siempre depende del nuevo `numcon`, no de que la hora programada ya haya llegado.

Si el último acto contiene el diagnóstico `Z53.8` (**Procedimiento no realizado por otras razones**), el médico documentó que no corresponde mantener el llamado. El visor lo detecta como un booleano interno, cancela un llamado que estuviera activo y no inicia otro para ese `numcon`. No lee, registra ni publica la descripción diagnóstica. Un acto anterior con `Z53.8` no cuenta como atención previa: si después se crea un acto nuevo sin ese código, puede volver a anunciarse, salvo que exista además otro acto anterior guardado sin `Z53.8`.

## Orden y concurrencia

Puede haber varios médicos que creen actos al mismo tiempo, pero la televisión utiliza un solo altavoz: nunca interrumpe un llamado activo. Al quedar libre, selecciona un `numcon` habilitado con este orden:

1. EMA (`obscit = EMA`).
2. Amanecida: observación de una letra (`A`, `B`, `C`, etc.) o letra seguida de `1` en la tarde (`A1`, `B1`, `C1`, etc.).
3. Demás citas, por creación del acto y hora programada.

Las prioridades ordenan únicamente pacientes que el médico ya habilitó con un `numcon`; la TV no crea actos ni adelanta por sí sola una cita sin acto médico.

Un reinicio del visor no debe repetir anuncios: los actos que ya estaban abiertos cuando inició el proceso permanecen como referencia, pero no provocan voz. Para emitir un nuevo llamado, LOLCLI debe registrar otro `numcon`.

## Ventanas y visibilidad

El sondeo trabaja por sede fija Cuajone (`siscod = 1`) y por jornada. La pantalla pública no exhibe cerrados (`P/S`) ni datos técnicos. El panel **Próximos turnos** muestra citas agendadas abiertas cuya `citdat` aún no haya pasado y pertenezca a la jornada vigente: mañana desde las 07:00 hasta antes de las 12:00, y tarde desde las 14:00 hasta antes de las 18:00. Durante el receso no anticipa citas de la tarde. No filtra por `obscit` y ordena ascendentemente por hora. Una cita vencida no se elimina de LOLCLI ni pierde la posibilidad de que el médico la abra manualmente; simplemente deja de presentarse como próxima. EMA y amanecida solo intervienen al priorizar actos que el médico ya habilitó para un llamado; no cambian el orden de esta agenda pública.

Los consultorios configurados en `Queue:ExcludedConsultorios` no se publican, anuncian ni incorporan a la cola interna. La configuración vigente excluye `MEDICINA DEL TRABAJO` y `EMERGENCIA`/`EMERGENCIAS`.

Solo hay dos estados públicos: `Llamando` y `Próximo`. `numcon`, `prfnum` y `stacon` son visibles únicamente en `/simulador` cuando se ejecuta en `DevelopmentSnapshot`.

## Simulador LocalDB

El simulador replica el flujo sin escribir en LOLCLI:

- **Llamar** crea un `numcon` nuevo. **Crear prefactura** es opcional y actualiza únicamente `citas.prfnum`, como en LOLCLI.
- **Guardar** cambia el último acto a `P` y la cita a `S`.
- **Llamar** después de guardar genera otro `numcon`, incluso para el mismo paciente; la regla de reapertura ambigua evita que se anuncie automáticamente.
- **Restablecer** elimina los actos de prueba de esa cita local y vuelve a dejarla en `N`/prefactura `0`.

La tabla técnica del simulador muestra todas las citas de la jornada, incluidas las cerradas y las horas ya pasadas. La TV no es una copia de esa tabla: solo muestra la cola pública vigente.

## Rutas de desarrollo y pruebas

| Ruta | Propósito | Perfil de inicio | Fuente de datos | Escrituras |
| --- | --- | --- | --- | --- |
| `/turnos` | Pantalla pública de TV: muestra el banner y las listas de `Llamando`/`Próximo`. | `http` para LOLCLI o `development-snapshot` para probar la pantalla con una copia local. | LOLCLI9000 en operación; LocalDB solo cuando se inicia con `development-snapshot`. | Ninguna. |
| `/simulador` | Herramienta de prueba técnica: muestra columnas de `citas` y `am_consulta`, acciones del acto y la cola interna. | Solo `development-snapshot`. | LocalDB `VisorTurnosDevelopment`. | Solo LocalDB. Nunca LOLCLI. |

Para probar el flujo completo sin tocar LOLCLI:

```powershell
dotnet run --launch-profile development-snapshot
```

Luego abre `http://localhost:5280/simulador` para crear o manipular una cita local y `http://localhost:5280/turnos` en otra pestaña para comprobar el anuncio resultante. El simulador no está disponible cuando se inicia el perfil `http` o cuando `DataSource:EnableDevelopmentSnapshot` no está habilitado.

Para operar contra la fuente real de solo lectura:

```powershell
dotnet run --launch-profile http
```

En ese perfil se usa la conexión ODBC autorizada a LOLCLI9000 y solo debe abrirse `/turnos`; `/simulador` responde como no disponible.

## Límites de certeza

Las filas observadas con `c.prfnum = 0`, `c.statte = 'N'` y `am_consulta.stacon = 'P'` descartan usar la prefactura o `P` aislado como condición universal de cierre. Para validar técnicamente cada transición se debe observar una misma cita antes de seleccionar, después de crear el acto, después de crear una prefactura (si se usa) y después de guardar, siempre con consultas de solo lectura.
