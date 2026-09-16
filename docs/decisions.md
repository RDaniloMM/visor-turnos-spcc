# Registro de decisiones

Ultima revision tecnica: 2026-09-16.

## Decisiones implementadas

| Fecha | Decision | Responsable | Estado |
| --- | --- | --- | --- |
| 2026-09-16 | Se autoriza que el visor público anuncie por voz el nombre visible del paciente y el consultorio, una sola vez por versión de llamado durante la sesión. | Responsable del proyecto | Aplicada |
| 2026-09-16 | Razor Pages + TypeScript + SignalR; sin Blazor. | Lineamiento del proyecto | Aplicada |
| 2026-09-16 | La fuente queda `Disabled` por defecto y `Demo` solo en Development. | Control preventivo tecnico | Reemplazada por la activacion ODBC autorizada para Cuajone |
| 2026-09-16 | El repositorio ODBC no selecciona `pacnam` ni `pachis`. | Privacidad por diseño | Reemplazada: `pacnam` autorizado como identificador publico; `pachis` sigue prohibido |
| 2026-09-16 | Sin identificador publico aprobado, una fila real no se publica. | Privacidad por diseño | Aplicada |
| 2026-09-16 | En la cola de citas, `citas.obscit = EMA` identifica examen medico y recibe `PriorityTier=1`. La observacion cruda no se publica ni registra. | Responsable del proyecto, solicitud directa | Aplicada |
| 2026-09-16 | La pantalla tiene un panel rotativo agrupado por consultorio y medico, y un panel general fijo ordenado por prioridad que muestra ambos datos de destino. | Responsable del proyecto, solicitud directa | Aplicada |
| 2026-09-16 | Se activa el DSN ODBC `LOLCLI9000` en modo lectura para `siscod=1` (Cuajone), con sondeo central cada 3 segundos, limite de 100 filas y timeout de 5 segundos. | Responsable del proyecto, autorizacion expresa | Aplicada |
| 2026-09-16 | El responsable autoriza mostrar `citas.pacnam` como identificador publico porque el paciente no conoce `invnum`; el visor no muestra `pachis`, `obscit` crudo ni `prfnum`. | Responsable del proyecto, autorizacion expresa | Aplicada |

## Pendientes de aprobacion

No se ha cerrado ninguno por inferencia. Registrar fecha, responsable y evidencia cuando se responda cada punto.

1. Valores reales de `statte` y transiciones.
2. Ausencia de prefactura: `NULL`, `0` o ambos.
3. Campo y valores para amanecida.
4. Integracion o separacion de emergencias y su precedencia exacta respecto a EMA.
5. Campo y regla de atencion preferencial.
6. Minutos y efecto de tolerancia.
7. Identificador publico conocido por el paciente.
8. Politica de nombre anonimizado.
9. Permanencia de casos llamados o cerrados.
10. Intervalo y SLA de actualizacion.
11. `siscod` por sede.
12. Version LTS aprobada por TI (el proyecto conserva `net10.0` del repositorio recibido).
13. Controlador y DSN ODBC.
14. Topologia IIS-SQL e identidad efectiva.
15. Cuenta personal temporal o cuenta de servicio/gMSA.
16. Texto, sonido y volumen.
17. Horario operativo y cambio de dia.

## Nota sobre demostracion

Los identificadores `DEMO-*`, estados y consultorios del modo Development son datos ficticios de presentacion. No representan codigos de LOLCLI ni decisiones del negocio.
