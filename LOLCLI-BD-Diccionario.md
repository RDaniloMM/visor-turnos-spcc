# Diccionario de Datos - Base de Datos LOLCLI

## Tabla: am_anamnesis

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numcon | int | 4 | Número de consulta |
| 2 | actana | text | 16 | Texto de la Anamnesis |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_am_anamnesis | nonclustered, unique, primary key located on PRIMARY | numcon |

---

## Tabla: am_anotaciones

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numcon | int | 4 | No de Secuencia |
| 2 | modcod | varchar | 1 | Código de modelo |
| 3 | codpln | varchar | 12 | Codigo de Item |
| 4 | tippln | varchar | 1 | Tipo de item |
| 5 | nivpln | int | 4 | Nivel de Item |
| 6 | despln | varchar | 50 | Descripción |
| 7 | datpin | varchar | 50 | Datos |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_am_anotaciones | nonclustered, unique, primary key located on PRIMARY | codpin |

---

## Tabla: am_antecedentes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | pachis | varchar | 7 | Número de historia |
| 2 | modcod | varchar | 1 | Código del modelo de historia |
| 3 | codpln | varchar | 12 | Codigo del formato de historia |
| 4 | tippin | varchar | 1 | Tipo de dato del formato |
| 5 | despin | varchar | 50 | Descripción del Dato del Formato |
| 6 | datpln | varchar | 50 | Respuesta |
| 7 | nivpln | int | 4 | Nivel del formato |
| 8 | longcar | int | 4 | Longitud de cadenas tipo varchar |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_am_antecedentes | nonclustered, unique, primary key located on PRIMARY | pachis, modcod, codpln |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_am_antec_ref_466_paciente | FOREIGN KEY | pachis REFERENCES pacientes (pachis) |

---

## Tabla: am_atencion

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numcon | int | 4 | Número de consulta |
| 2 | modcod | varchar | 1 | Código del modelo |
| 3 | codpin | varchar | 12 | Código de indentación |
| 4 | tippin | varchar | 1 | Tipo de formato |
| 5 | nivpln | int | 4 | Nivel del formato |
| 6 | despin | varchar | 50 | Descripción del formato |
| 7 | datpin | varchar | 50 | Respuesta del formato de atención |
| 8 | longcar | int | 4 | Longitud de cadenas tipo varchar |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_am_atenci | nonclustered, unique, primary key located on PRIMARY | numcon, modcod, codpin |

---

## Tabla: am_consulta

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numcons | int | 4 | Número de consulta |
| 2 | pachis | varchar | 7 | Número de historia |
| 3 | sercod | varchar | 4 | Código del servicio |
| 4 | medcod | varchar | 4 | Código del médico |
| 5 | feccon | datetime | 8 | Fecha de la consulta |
| 6 | houcon | varchar | 5 | Hora de la consulta |
| 7 | modcon | varchar | 1 | Modelo de la consulta |
| 8 | obscon | text | 16 | Observación de la consulta |
| 9 | prfnum | int | 4 | Número de prefactura asociada |
| 10 | pinnum | varchar | 6 | Código del plan |
| 11 | siscod | int | 4 | Establecimiento |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| fecon | nonclustered located on PRIMARY | feccon |
| pk_am_consulta | nonclustered, unique, primary key located on PRIMARY | numcon |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_am_consu_ref_509_paciente | FOREIGN KEY | pachis REFERENCES pacientes (pachis) |
| fk_am_consu_ref_529_medicos | FOREIGN KEY | medcod REFERENCES medicos (medcod) |
| fk_am_consu_ref_684_servicio | FOREIGN KEY | sercod REFERENCES servicios (sercod) |

**Referencias desde:**
- `.am_funciones`: `fk_am_funciones_numcon`
- `.am_incapacidades_temporales`: `fk_numcon_am`

---

## Tabla: am_diagnosticos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numcon | int | 4 | Número de consulta |
| 2 | coditm | varchar | 1 | Numero de item del diagnóstico |
| 3 | diades | varchar | 140 | Descripción del diagnóstico |
| 4 | tipdia | varchar | 1 | Tipo de diagnóstico |
| 5 | diacod | varchar | 6 | Código del diagnóstico |
| 6 | obsdia | varchar | 50 | Observación del diagnóstico |
| 7 | diarcod | varchar | 2 | Reincidencia del Diagnóstico (Primera Vez) |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_am_diagnosticos | clustered, unique, primary key located on PRIMARY | numcon, coditm |

### Constraints
| Nombre Constraint | Tipo | Constraint Keys |
|---|---|---|
| DF_am_diagno_diarc_50E6C0E9 | DEFAULT | on column diarcod ('00') |

---

## Tabla: am_enfermeria

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numcon | int | 4 | Numero de consulta |
| 2 | datenf | text | 16 | Registro de información |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_am_enfermeria | clustered, unique, primary key located on PRIMARY | numcon |

---

## Tabla: am_evolucion

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numcon | int | 4 | Secuencia Acto Médico |
| 2 | actevo | text | 16 | Texto de Evolución |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_numcon_am_evolucion | clustered, unique, primary key located on PRIMARY | numcon |

---

## Tabla: am_examenes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numcon | int | 4 | Número de consulta |
| 2 | coditm | int | 4 | Número de item |
| 3 | exacod | varchar | 6 | Código del examen |
| 4 | exades | varchar | 40 | Descripción del examen |
| 5 | exares | text | 16 | Resultados del examen |
| 6 | exaobs | varchar | 40 | Observaciones del examen |
| 7 | tigexa1 | varchar | 1 | Tipo del 1er gráfico del examen |
| 8 | degexa1 | varchar | 13 | Nombre del 1er archivo del examen |
| 9 | tigexa2 | varchar | 1 | Tipo del 2do gráfico del examen |
| 10 | degexa2 | varchar | 13 | Nombre del 2do archivo del examen |
| 11 | tigexa3 | varchar | 1 | Tipo del 3er gráfico del examen |
| 12 | degexa3 | varchar | 13 | Nombre del 3er archivo del examen |
| 13 | exachk | varchar | 1 | Estado de la Orden |
| 14 | invnum | int | 4 | Secuencia EA |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_am_examenes | nonclustered, unique, primary key located on PRIMARY | numcon, coditm |

---

## Tabla: am_farmacos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numcon | int | 4 | Número de consulta |
| 2 | coditm | int | 4 | Número de item |
| 3 | codpos | varchar | 1 | Item asociado al diagnóstico |
| 4 | codpro | varchar | 5 | Código del producto |
| 5 | despro | varchar | 30 | Descripción del Producto |
| 6 | durpro | varchar | 15 | Duración del tratamiento |
| 7 | accpro | varchar | 15 | Acción del tratamiento |
| 8 | trapro | varchar | 1 | Tipo de inicio de tratamiento |
| 9 | desdos | varchar | 50 | Dosis |
| 10 | qtypro | int | 4 | Cantidad Entera |
| 11 | qtypro_m | int | 4 | Cantidad Menudeo |
| 12 | stkfra | int | 4 | Unidad de Fraccionamiento (Tabla fa_productos) |
| 13 | chkpro | varchar | 1 | Estado del producto (Despachado o Pendiente) |
| 14 | invnum | int | 4 | Secuencia FA |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_am_farmacos | clustered, unique, primary key located on PRIMARY | numcon, coditm |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_am_farmacos_codpro | FOREIGN KEY | codpro REFERENCES .fa_productos (codpro) |

---

## Tabla: am_formatos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | forcod | varchar | 1 | Código del formulario |
| 2 | codpin | varchar | 12 | Código del formato |
| 3 | nivpin | int | 4 | Nivel del formato |
| 4 | despin | varchar | 50 | Descripción del formato |
| 5 | typpin | varchar | 1 | Definición de tipo de dato |
| 6 | modcod | varchar | 1 | Código de modelo del formato |
| 7 | ranpln1 | varchar | 15 | Rango de inicio |
| 8 | ranpin2 | varchar | 15 | Rango final |
| 9 | valpin | varchar | 30 | Valores del formato |
| 10 | datpin | text | 16 | Datos predeterminados de la respuesta |
| 11 | picpin | varchar | 30 | Nombre del archivo BITMAP asociado |
| 12 | longcar | int | 4 | Longitud de cadenas tipo varchar |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_am_formatos | nonclustered, unique, primary key located on PRIMARY | forcod, codpin, modcod |

---

## Tabla: am_funciones

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numcon | int | 4 | Secuencia Acto Médico |
| 2 | modcod | varchar | 1 | Modelo de Historia |
| 3 | codpln | varchar | 12 | Parte del Modelo (Antecedentes, Atención) |
| 4 | tippin | varchar | 1 | Tipo de Dato |
| 5 | despin | varchar | 50 | Nombre del Dato |
| 6 | datpin | varchar | 50 | Datos registrados |
| 7 | nivpln | int | 4 | Nivel del Dato en el Árbol |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_am_funciones | nonclustered, unique, primary key located on PRIMARY | numcon, modcod, codpin |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_am_funciones_numcon | FOREIGN KEY | numcon REFERENCES .am_consulta (numcon) |
| fk_am_funciones_ref_am_modelos | FOREIGN KEY | modcod REFERENCES .am_modelos (modcod) |

---

## Tabla: am_incapacidades_temporales

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Secuencia Incapacidades |
| 2 | numcon | int | 4 | Secuencia Acto Médico asociado. |
| 3 | tincod | varchar | 2 | Tipo de Incapacidad |
| 4 | tipmat | varchar | 1 | Si es Maternidad (Post natal o Pre natal) |
| 5 | fecini | datetime | 8 | Fecha Inicio |
| 6 | fecfin | datetime | 8 | Fecha Fin |
| 7 | numdias | int | 4 | Número de Días |
| 8 | mespro | varchar | 6 | Mes de Proceso |
| 9 | obspro | varchar | 40 | Observaciones |
| 10 | compro | text | 16 | Documento |
| 11 | docpro | varchar | 400 | Comentarios |
| 12 | incfem | datetime | 8 | Fecha de Emisión (de Registro) |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_invnum_am_incapa_temp | clustered, unique, primary key located on PRIMARY | invnum |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| chk_tipmat_am_incapa_temp | CHECK Table Level | ([tipmat] = 'D' or [tipmat] = 'A') |
| fk_numcon_am_incapa_temp | FOREIGN KEY | numcon REFERENCES .am_consulta (numcon) |

---

## Tabla: am_intervenciones

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numcon | int | 4 | Número de Consulta |
| 2 | coditm | varchar | 1 | No. Intervención solicitada |
| 3 | intcod | varchar | 6 | Código Intervención |
| 4 | intdes | varchar | 70 | Descripción de Intervención |
| 5 | intchk | varchar | 1 | Estado de la petición de la Intervención |
| 6 | invnum | int | 4 | Secuencia IQX |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_am_intervenciones | clustered, unique, primary key located on PRIMARY | numcon, coditm |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_am_intervenciones_intcod | FOREIGN KEY | intcod REFERENCES intervenciones_quirurgicas (intcod) |

---

## Tabla: am_laboratorio

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numcon | int | 4 | Número de consulta |
| 2 | coditm | int | 4 | Número de item |
| 3 | exacod | varchar | 6 | Código del examen |
| 4 | exades | varchar | 40 | Descripción del examen |
| 5 | exares | text | 16 | Resultados del examen |
| 6 | exaobs | varchar | 40 | Observaciones del examen |
| 7 | tigexa1 | varchar | 1 | Tipo del 1er gráfico del examen |
| 8 | degexa1 | varchar | 13 | Nombre del 1er archivo del examen |
| 9 | tigexa2 | varchar | 1 | Tipo del 2do gráfico del examen |
| 10 | degexa2 | varchar | 13 | Nombre del 2do archivo del examen |
| 11 | tigexa3 | varchar | 1 | Tipo del 3er gráfico del examen |
| 12 | degexa3 | varchar | 13 | Nombre del 3er archivo del examen |
| 13 | exachk | varchar | 1 | Estado de la Orden LA |
| 14 | invnum | int | 4 | Secuencia LA |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_am_laboratorio | clustered, unique, primary key located on PRIMARY | numcon, coditm |

---

## Tabla: am_modelos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | modcod | varchar | 1 | Código del modelo de formato |
| 2 | moddes | varchar | 20 | Descripción del modelo de formato |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_am_modelos | nonclustered, unique, primary key located on PRIMARY | modcod |

**Referencias desde:**
- `am_funciones`: `fk_am_funciones_ref_am_mode`

---

## Tabla: am_notas_medico

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numcon | int | 4 | Secuencia Acto Médico |
| 2 | actnot | text | 16 | Notas del Médico |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_numcon_am_notas_medico | clustered, unique, primary key located on PRIMARY | numcon |

---

## Tabla: am_notas_paciente

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | pachis | varchar | 7 | No. de Historia |
| 2 | actpac | text | 16 | Notas Importantes del Paciente |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_pachis_am_notas_paciente | clustered, unique, primary key located on PRIMARY | pachis |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_pachis_am_notas_pacientes | FOREIGN KEY | pachis REFERENCES pacientes (pachis) |

---

## Tabla: am_partes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | forcod | varchar | 1 | Código del formulario |
| 2 | fordes | varchar | 20 | Descripción del formulario |
| 3 | forsta | varchar | 1 | Estado del formulario |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_am_partes | nonclustered, unique, primary key located on PRIMARY | forcod |

---

## Tabla: am_plan_trabajo

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numcon | int | 4 | Secuencia Acto Médico |
| 2 | actplan | text | 16 | Plan de Trabajo |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_numcon_am_plan_trabajo | clustered, unique, primary key located on PRIMARY | numcon |

---

## Tabla: am_procedimientos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numcon | int | 4 | Secuencia Acto Médico |
| 2 | coditm | varchar | 1 | No. procedimiento solicitado |
| 3 | tarcod | varchar | 8 | Código de tarifario |
| 4 | tardes | varchar | 70 | Descripción del Procedimiento |
| 5 | procan | int | 4 | Cantidad |
| 6 | intchk | varchar | 1 | Estado de la solicitud de procedimiento |
| 7 | invnum | int | 4 | Secuencia de PM |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_am_procedimientos | clustered, unique, primary key located on PRIMARY | numcon, coditm |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_am_procedimientos_tarcod | FOREIGN KEY | tarcod REFERENCES tarifario (tarcod) |

---

## Tabla: am_visuales

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numcon | int | 4 | Número de consulta |
| 2 | numpic | varchar | 1 | Número de gráfico |
| 3 | filpic | varchar | 15 | Nombre del archivo del gráfico |
| 4 | despic | varchar | 40 | Descripción del Gráfico |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_am_visuales | nonclustered, unique, primary key located on PRIMARY | numcon, numpic |

---

## Tabla: ampliaciones_cobertura

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Secuencia Ampliación |
| 2 | prfnum | int | 4 | No. Prefactura |
| 3 | useamp | int | 4 | Código Usuario que registra ampliación |
| 4 | fecamp | datetime | 8 | Fecha de registro de ampliación |
| 5 | motamp | text | 16 | Motivo de ampliación |
| 6 | obsamp | varchar | 50 | Observaciones |
| 7 | monamp | numeric | 9 | Monto de ampliación (sumará a la cobertura) |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ampliaciones_cobertura | clustered, unique, primary key located on PRIMARY | invnum |

---

## Tabla: aportantes (COTIZACIONES)

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Secuencia de Aportantes |
| 2 | codase | varchar | 12 | Código de Asegurado |
| 3 | docase | varchar | 20 | Documento |
| 4 | patase | varchar | 20 | Apellido Paterno |
| 5 | matase | varchar | 20 | Apellido Materno |
| 6 | nomase | varchar | 20 | Nombres |
| 7 | concod | varchar | 4 | Código del Contratante |
| 8 | cardoc | varchar | 20 | Carnet |
| 9 | fecing | datetime | 8 | Fecha de Ingreso |
| 10 | monpro | varchar | 6 | Mes y Año |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_invnum_aportantes | nonclustered, unique, primary key located on PRIMARY | invnum |
| UQ_aportantes_57F2E5D4 | nonclustered, unique, unique key located on PRIMARY | docase |
| UQ_aportantes_58E70A0D | nonclustered, unique, unique key located on PRIMARY | codase |

**Referencia desde:**
- `.aportantes_aportes`: `fk_docase_aportantes_`

---

## Tabla: aportantes_aportes (COTIZACIONES)

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | docase | varchar | 20 | Documento |
| 2 | codase | varchar | 12 | Código del Asegurado |
| 3 | datapo | varchar | 6 | Mes y Año |
| 4 | monapo | decimal | 9 | Monto de Aportación |
| 5 | trecod | varchar | 2 | Tipo de Remuneración |
| 6 | concod | varchar | 4 | Código de Contratante |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_docase_datapo_aportantes | clustered, unique, primary key located on PRIMARY | docase, datapo, trecod, concod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_docase_aportantes_aportes_a | FOREIGN KEY | docase REFERENCES aportantes (docase) |
| fk_trecod_aportantes_aportes_t | FOREIGN KEY | trecod REFERENCES tipo_remuneracion (trecod) |

---

## Tabla: aportes_empresariales (COTIZACIONES)

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | concod | varchar | 4 | Contratante |
| 2 | datapo | varchar | 6 | Fecha de Aportación |
| 3 | datpag | datetime | 8 | Fecha de Pago |
| 4 | datven | datetime | 8 | Fecha de Vencimiento |
| 5 | totsal | numeric | 9 | Total Salarios |
| 6 | tasapo | numeric | 9 | Tasa de aportaciones |
| 7 | totcot | numeric | 9 | Total de la cotización |
| 8 | totsub | numeric | 9 | Total de la subvención |
| 9 | totrec | numeric | 9 | Total recibido |
| 10 | totapo | numeric | 9 | Total aportaciones |
| 11 | trecod | varchar | 2 | Tipo de remuneración |
| 12 | totact | numeric | 9 | Total actualizado |
| 13 | totint | numeric | 9 | Total intereses |
| 14 | totmul | numeric | 9 | Total Multas |
| 15 | totret | numeric | 9 | Total Retenciones |
| 16 | totactx | numeric | 9 | Total actualizado registrado |
| 17 | totintx | numeric | 9 | Total Intereses registrado |
| 18 | totmulx | numeric | 9 | Total Multas registrado |
| 19 | totretx | numeric | 9 | Total Retenciones registrado |
| 20 | totapox | numeric | 9 | Total aportaciones registrado |
| 21 | totsalx | numeric | 9 | Total Salarios registrado |
| 22 | totcotx | numeric | 9 | Total cotizacion registrado |
| 23 | totsubx | numeric | 9 | Total subvención registrado |
| 24 | tottra | numeric | 9 | Total trabajadores |
| 25 | tottrax | numeric | 9 | Total trabajadores registrado |
| 26 | refapo | varchar | 20 | Referencia |
| 27 | datpre | datetime | 8 | Fecha de presentacion planilla |
| 28 | totrecx | decimal | 9 | Total Recibido registrado |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_concod_datapo_trecod_aporte | nonclustered, unique, primary key located on PRIMARY | concod, datapo, trecod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_trecod_aportes_empresariale | FOREIGN KEY | trecod REFERENCES tipo_remuneracion (trecod) |

---

## Tabla: aportes_empresariales_detalle (COTIZACIONES)

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | concod | varchar | 4 | Contratante |
| 2 | datapo | varchar | 6 | Fecha de Aportación |
| 3 | trecod | varchar | 2 | Tipo de Remuneración |
| 4 | invsec | int | 4 | Secuencia de aportación |
| 5 | tincod | varchar | 2 | Tipo de incapacidad |
| 6 | tinnum | int | 4 | Número de incapacidad por tipo |
| 7 | tinmon | numeric | 9 | Monto por incapacidad |
| 8 | tinnumx | numeric | 9 | Número de incapacidad por tipo registrado |
| 9 | tinmonx | numeric | 9 | Monto por incapacidad registrado |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_concod_datapo_trecod_invsec | nonclustered, unique, primary key located on PRIMARY | concod, datapo, trecod, invsec |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_tincod_aportes_empresariale | FOREIGN KEY | tincod REFERENCES tipo_incapacidad (tincod) |
| fk_trecod_aportes_empresariale | FOREIGN KEY | trecod REFERENCES .tipo_remuneracion (trecod) |

---

## Tabla: asociaciones

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | asocod | varchar | 6 | Código de la Asociación |
| 2 | asodes | varchar | 40 | Descripción |
| 3 | asoobs | varchar | 40 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_asociaciones_asocod | clustered, unique, primary key located on PRIMARY | asocod |

---

## Tabla: bajas (COTIZACIONES)

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | docase | varchar | 20 | Doc.Identidad asegurado |
| 2 | concod | varchar | 4 | Contratante |
| 3 | fecbaj | datetime | 8 | Fecha de baja |
| 4 | monpro | varchar | 6 | Mes de proceso |
| 5 | bajobs | varchar | 40 | Observaciones |
| 6 | codase | varchar | 12 | Código de asegurado |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_docase_concod_bajas | nonclustered, unique, primary key located on PRIMARY | docase, concod, monpro |

---

## Tabla: caja_egresos_varios

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Numero de Secuencia |
| 2 | usecaj | varchar | 2 | codigo de cajeros |
| 3 | invfec | datetime | 8 | Fecha de egreso |
| 4 | usecod | numeric | 9 | Código de ususario |
| 5 | invtot | numeric | 9 | Total de egreso |
| 6 | invigv | numeric | 9 | Total de impuesto |
| 7 | invnet | numeric | 9 | Monto sin impuesto |
| 8 | invcon | varchar | 50 | Concepto del Egreso |
| 9 | invobs | varchar | 50 | Observación |
| 10 | invref | varchar | 25 | Referencia |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_caja_egresos_varios | clustered, unique, primary key located on PRIMARY | invnum |

---

## Tabla: caja_ingresos_varios

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | usecaj | varchar | 2 | Número de Secuencia |
| 3 | invfec | datetime | 8 | Fecha de ingreso |
| 4 | usecod | numeric | 9 | Código de ususario |
| 5 | invtot | numeric | 9 | Monto total del ingreso |
| 6 | invigv | numeric | 9 | Monto total de impuesto |
| 7 | invnet | numeric | 9 | Monto sin impuesto |
| 8 | invcon | varchar | 50 | Concepto del Ingreso |
| 9 | invobs | varchar | 50 | Observación |
| 10 | invref | varchar | 25 | Referencia del Ingreso |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_caja_ingresos_varios | clustered, unique, primary key located on PRIMARY | invnum |

---

## Tabla: cajeros

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codcaj | varchar | 2 | Código del cajero |
| 2 | descaj | varchar | 20 | Descripción del cajero |
| 3 | usecod | int | 4 | Código de usuario |
| 4 | orifac | varchar | 2 | Origen de facturación |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_cajeros | clustered, unique, primary key located on PRIMARY | codcaj |

---

## Tabla: cajeros_tabla

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codcaj | varchar | 2 | Código del cajero |
| 2 | tdoser | varchar | 2 | Tipo de documento de la serie |
| 3 | tdofac | varchar | 2 | Tipo de Documento |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_cajeros_tabla | clustered, unique, primary key located on PRIMARY | codcaj, tdoser |

---

## Tabla: cajeros_usuarios

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codcaj | varchar | 2 | Código del cajero |
| 2 | usecod | int | 4 | Código del usuario |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| PK_TAB_Cajeros_Usuarios | clustered, unique, primary key located on PRIMARY | codcaj, usecod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| FK_CAJEROS_REF_195_USUARIOS | FOREIGN KEY | usecod REFERENCES usuarios (usecod) |

---

## Tabla: camas

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | hoscam | varchar | 4 | Código de la cama |
| 2 | stacam | varchar | 2 | Código de estado de la cama |
| 3 | tarcod | varchar | 8 | Código de tarifa |
| 4 | invnum | int | 4 | Correlativo asociado de origen |
| 5 | pacpic | varchar | 20 | Nombre del archivo gráfico de la cama |
| 6 | descam | varchar | 40 | Nombre largo de la habitación |
| 7 | abrcam | varchar | 10 | Nombre corto de la habitación |
| 8 | sercod | varchar | 4 | Servicio de referencia |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_camas | nonclustered, unique, primary key located on PRIMARY | hoscam |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| df_camas_sercod | DEFAULT | on column sercod ('0000') |
| fk_cams_tarifario | FOREIGN KEY | tarcod REFERENCES tarifario (tarcod) |

---

## Tabla: categorias_pago

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | parcod | varchar | 2 | Código de categoría de pago |
| 2 | pardes | varchar | 25 | Descripción de la categoria |
| 3 | parfac | numeric | 9 | Factor de la categoría |
| 4 | parobs | text | 16 | Observación |
| 5 | paruti_fa | numeric | 5 | Utilidad para Farmacia |
| 6 | pardto_fa | numeric | 5 | Descuento de farmacia |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_categorias_pago | clustered, unique, primary key located on PRIMARY | parcod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| df_pardto_fa | DEFAULT | on column pardto_fa (0) |
| df_paruti_fa | DEFAULT | on column paruti_fa (0) |

**Referencia desde:**
- `.planes_categoria_historico`: `fk_categoria_`
- `LOLCLI2000.jessica.planes_categoria_historico`: `fk_catego`

---

## Tabla: citas

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | medcod | varchar | 4 | Código del médico |
| 2 | codcon | varchar | 4 | Código de consultorio |
| 3 | citdat | datetime | 8 | Fecha de la cita |
| 4 | statte | varchar | 2 | Estado de atención de la cita |
| 5 | tcicod | varchar | 2 | Tipo de citado |
| 6 | pachis | varchar | 7 | Número de historia |
| 7 | obscit | varchar | 20 | Observación |
| 8 | pacnam | varchar | 30 | Nombre del paciente |
| 9 | tarcod | varchar | 8 | Tarifa asociada |
| 10 | prfnum | int | 4 | Número de prefactura |
| 11 | invnum | int | 4 | Correlativo de origen |
| 12 | tppcod | varchar | 2 | Tipo de paciente |
| 13 | usecod | int | 4 | Código de usuario |
| 14 | plnnum | varchar | 6 | Código de plan |
| 15 | parcod | varchar | 2 | Código de categoria de pago |
| 16 | citded | numeric | 9 | Monto del deducible |
| 17 | citcoa | numeric | 9 | Porcentaje de coaseguro |
| 18 | citmon | numeric | 9 | Monto del servicio |
| 19 | citcar | varchar | 10 | Carta de beneficios |
| 20 | citfca | datetime | 8 | Fecha de vigencia |
| 21 | invnum_pre | int | 4 | Número de presupuesto |
| 22 | citfvc | datetime | 8 | Fecha de vencimiento de la carta |
| 23 | concod | varchar | 4 | Código de cía Contratante |
| 24 | segcod | varchar | 4 | Código de cía de Seguros |
| 25 | siscod | int | 4 | Código de Establecimiento |
| 26 | tarcos | numeric | 9 | Costo de Tarifa |
| 27 | tarcos_d | numeric | 9 | Costo en dolares de la tarifa |
| 28 | tarcos_e | numeric | 9 | Costos de Establecimiento |
| 29 | cithll | datetime | 8 | Hora de llegada a la cita |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| invnum | nonclustered, unique located on PRIMARY | invnum |
| pacnam | nonclustered located on PRIMARY | pacnam |
| pk_citas | nonclustered, unique, primary key located on PRIMARY | medcod, citdat |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_citas_ref_107_medicos | FOREIGN KEY | medcod REFERENCES medicos (medcod) |
| fk_citas_ref_41_tipo_cit | FOREIGN KEY | tcicod REFERENCES tipo_citado (tcicod) |
| fk_citas_ref_432_usuarios | FOREIGN KEY | usecod REFERENCES usuarios (usecod) |
| fk_citas_siscod | FOREIGN KEY | siscod REFERENCES sistema (siscod) |

---

## Tabla: citas_ea

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | medcod | varchar | 4 | Código del Médico |
| 2 | citdat | datetime | 8 | Fecha y hora de la Cita |
| 3 | sercod | varchar | 4 | Código del Servicio |
| 4 | pachis | varchar | 7 | Historia del Paciente |
| 5 | exacod | varchar | 6 | Código del Exámen |
| 6 | statte | varchar | 2 | Estado de Atención |
| 7 | usecod | int | 4 | Código del Usuario |
| 8 | siscod | int | 4 | Código de Procedencia |
| 9 | citobs | varchar | 25 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_medcod_citdat_citas_ea | nonclustered, unique, primary key located on PRIMARY | medcod, citdat |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_medcod_citas_ea_medicos | FOREIGN KEY | medcod REFERENCES medicos (medcod) |
| fk_pachis_citas_ea_pacientes | FOREIGN KEY | pachis REFERENCES pacientes (pachis) |
| _siscod_citas_ea_sistema | FOREIGN KEY | siscod REFERENCES sistema (siscod) |
| fk_usecod_citas_ea_usuarios | FOREIGN KEY | usecod REFERENCES usuarios (usecod) |

---

## Tabla: cobradores

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codcob | varchar | 6 | Código del cobrador |
| 2 | nomcob | varchar | 30 | Nombre del cobrador |
| 3 | dircob | varchar | 45 | Dirección del cobrador |
| 4 | telcob | varchar | 25 | Teléfono del cobrador |
| 5 | tpccob | varchar | 1 | Estado de vigencia del cobrador |
| 6 | porcob | numeric | 9 | Porcentaje del Cobrador |
| 7 | moncob | numeric | 9 | Monto de cobranza o comisión |
| 8 | obscob | varchar | 60 | Observación |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_cobradores | nonclustered, unique, primary key located on PRIMARY | codcob |

---

## Tabla: cobradores_zonas

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codcob | varchar | 6 | Código del cobrador |
| 2 | ubicod | varchar | 6 | Código de ubicación |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_cobradores_zonas | nonclustered, unique, primary key located on PRIMARY | codcob, ubicod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_cobradores_zonas_cob | FOREIGN KEY | ubicod REFERENCES ubigeo (ubicod) |

---

## Tabla: condicion_medico

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | comcod | varchar | 2 | Código de condición del médico (administrativo, etc.) |
| 2 | comdes | varchar | 25 | Descripción de la condición del médico |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_condicion_medico | clustered, unique, primary key located on PRIMARY | comcod |

---

## Tabla: condicion_paciente

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | contip | varchar | 2 | Tipo de condición del paciente (llegada) |
| 2 | condes | varchar | 20 | Descripción de condición del paciente |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_condicion_paciente | clustered, unique, primary key located on PRIMARY | contip |

---

## Tabla: consultorios

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codcon | varchar | 4 | Código de consultorios |
| 2 | descon | varchar | 20 | Descripción del consultorio |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_codigo_consulto | clustered, unique, primary key located on PRIMARY | codcon |

---

## Tabla: contratantes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | concod | varchar | 4 | Código de la cía contratante |
| 2 | conraz | varchar | 70 | Razón social de la cía contratante |
| 3 | condir | varchar | 45 | Dirección de la cía contratante |
| 4 | ubicod | varchar | 6 | Código de ubicación |
| 5 | contel | varchar | 25 | Teléfono de la cía contratante |
| 6 | conruc | varchar | 8 | Número de RUC de la cía contratante |
| 7 | conres | varchar | 30 | Responsable de la cía contratante |
| 8 | conobs | varchar | 40 | Observaciones |
| 9 | ruc | varchar | 20 | C.Contribuyente |
| 10 | conref1 | varchar | 15 | Responsable |
| 11 | conref2 | varchar | 15 | Número de empleados |
| 12 | conref3 | varchar | 15 | Total salario |
| 13 | conref4 | varchar | 15 | Promedio Salarial |
| 14 | conref5 | varchar | 15 | Primera referencia |
| 15 | concmt | varchar | 400 | Documento de referencia |
| 16 | conref6 | varchar | 15 | Segunda referencia |
| 17 | contxt | text | 16 | Comentario |
| 18 | siscod | int | 4 | Código de Establecimiento |
| 19 | conact | varchar | 2 | Código de Actividad |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_contratantes | clustered, unique, primary key located on PRIMARY | concod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_siscod_contratantes_sistema | FOREIGN KEY | siscod REFERENCES sistema (siscod) |

**Referencia desde:**
- `planes_seguro_historico`: `fk_contratantes_`
- `LOLCLI2000.jessica.planes_seguro_historico`: `fk_contratan`

---

## Tabla: control_colas_tickets

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Secuencia de ticket |
| 2 | invfec | datetime | 8 | Fecha de emisión |
| 3 | invobs | varchar | 25 | Observación |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_control_colas_tickets | clustered, unique, primary key located on PRIMARY | invnum |

---

## Tabla: diagnostico_reincidencia

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | diarcod | varchar | 2 | Código del diagnóstico |
| 2 | diardes | varchar | 15 | Descripción |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_diarcod | clustered, unique, primary key located on PRIMARY | diarcod |

---

## Tabla: diagnosticos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | diacod | varchar | 6 | Código del diagnóstico |
| 2 | diades | varchar | 140 | Descripción del diagnóstico |
| 3 | diasex | varchar | 1 | Sexo asociado |
| 4 | diaini | int | 4 | Edad de inicio |
| 5 | diafin | int | 4 | Edad de fin |
| 6 | tarcos | numeric | 9 | Costo asociado |
| 7 | diagru | varchar | 3 | Grupo de diagnóstico |
| 8 | diaceps | varchar | 2 | diaceps |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_diagnosticos | nonclustered, unique, primary key located on PRIMARY | diacod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| chk_diagnosticos_diaceps | CHECK on column diaceps | ([diaceps] = 'CS' or [diaceps] = 'CC') |

**Referencia desde:**
- `.intervenciones_cabecera`: `fk_diacod`

---

## Tabla: diagnosticos_asociaciones

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | diacod | varchar | 6 | Código del diagnóstico |
| 2 | asocod | varchar | 6 | Diagnostico asociado |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_diagnosticos_asociaciones_d | clustered, unique, primary key located on PRIMARY | diacod, asocod |

---

## Tabla: diagnosticos_protocolos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | diacod | varchar | 6 | Código del diagnóstico |
| 2 | numitm | int | 4 | Número de item |
| 3 | tippro | varchar | 2 | Tipo de producto |
| 4 | tpscod | varchar | 2 | Tipo de tarifa |
| 5 | codprs | varchar | 8 | Código de la tarifa |
| 6 | despro | varchar | 40 | Descripción del producto |
| 7 | diaexc | int | 4 | Número de exclusión |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_diacod_diagnosticos_protoco | clustered, unique, primary key located on PRIMARY | diacod, numitm |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| chk_tippro_diagnosticos_protoc | CHECK on column tippro | ([tippro] = 'PR' or ([tippro] = 'FA' or [tippro] = 'GE')) |
| chk_tpscod_diagnosticos_protoc | CHECK on column tpscod | ([tpscod] = 'IN' or ([tpscod] = 'EA' or ([tpscod] = 'TA'))) |

---

## Tabla: dietas

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | coddie | varchar | 4 | Código de dieta |
| 2 | desdie | varchar | 40 | Descripción de la dieta |
| 3 | condie | text | 16 | Contenido de la dieta |
| 4 | tarcos | numeric | 9 | Costo de la dieta |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| coddie_01 | nonclustered located on PRIMARY | coddie |
| PK_DIETAS | clustered, unique, primary key located on PRIMARY | coddie |

---

## Tabla: documentos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | tdofac | varchar | 2 | Código del tipo de documento (FA, BO, TI) |
| 2 | tdodes | varchar | 20 | Descripción del tipo de documento |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_documentos | clustered, unique, primary key located on PRIMARY | tdofac |

---

## Tabla: documentos_pago

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | docpag | varchar | 2 | Código de documento de pago |
| 2 | docdes | varchar | 20 | Descripción del documento de pago |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_documentos_pago | clustered, unique, primary key located on PRIMARY | docpag |

---

## Tabla: documentos_series

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | tdoser | varchar | 2 | Serie del documento |
| 2 | tdofac | varchar | 2 | Tipo de documento |
| 3 | tdonum | int | 4 | Número del documento |
| 4 | destse | varchar | 20 | Descripción de la serie |
| 5 | orifac | varchar | 2 | Origen de la serie (caja, facturación) |
| 6 | tsedwf | varchar | 70 | Nombre del datawindow |
| 7 | serpre | varchar | 4 | Prefijo de la serie |
| 8 | serini | int | 4 | Inicio de la serie |
| 9 | serfin | int | 4 | Fin de la serie |
| 10 | seraux_1 | varchar | 40 | Observación 1 |
| 11 | seraux_2 | varchar | 40 | Observación 2 |
| 12 | serdat | datetime | 8 | Fecha de autorización de la serie |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_docu_series | clustered, unique, primary key located on PRIMARY | tdoser |

---

## Tabla: ea_almacenes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codalm | varchar | 2 | Código de Almacén |
| 2 | desalm | varchar | 20 | Descripción de Almacén |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_almacenes | clustered, unique, primary key located on PRIMARY | codalm |

---

## Tabla: ea_examenes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | exacod | varchar | 6 | Código del examen |
| 2 | exades | varchar | 40 | Descripción del examen |
| 3 | exagrp | varchar | 2 | Código de grupo del examen |
| 4 | exaest | varchar | 1 | Estado de vigencia del examen |
| 5 | exapri | numeric | 9 | Precio del examen |
| 6 | exadto | numeric | 9 | Descuento del examen |
| 7 | tarund | numeric | 9 | Unidad equivalente del examen |
| 8 | exatif | varchar | 1 | Tipo de formato |
| 9 | exafor | text | 16 | Formato del examen |
| 10 | taruni | numeric | 9 | Precio Único |
| 11 | tarsta | varchar | 1 | Tipo de precio |
| 12 | igvexa | numeric | 9 | Impuestos |
| 13 | tarcos | numeric | 9 | Costo del examen |
| 14 | moncod | char | 2 | Moneda |
| 15 | tarcos_d | numeric | 9 | Costo en dólares |
| 16 | exaref | numeric | 9 | Costo de referencia |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_examenes | clustered, unique, primary key located on PRIMARY | exacod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| chk_ea_formato | CHECK Table Level | ([exatif] = '' or [exatif] = 'C' or [exatif] = 'N') |

---

## Tabla: ea_examenes_reactivos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | exacod | varchar | 6 | Código examen |
| 2 | codrea | varchar | 4 | Código reactivo |
| 3 | qtyrea | numeric | 9 | Cantidad reactivo |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_exa_rea_exacod | clustered, unique, primary key located on PRIMARY | exacod, codrea |

---

## Tabla: ea_fabricantes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codfab | varchar | 4 | Código |
| 2 | desfab | varchar | 40 | Descripción |
| 3 | telfab | varchar | 25 | Teléfono |
| 4 | dirfab | varchar | 45 | Dirección |
| 5 | obsfab | varchar | 40 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_codfab | clustered, unique, primary key located on PRIMARY | codfab |

---

## Tabla: ea_grupos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | exagrp | varchar | 2 | Código del grupo |
| 2 | desgrp | varchar | 20 | Descripción del grupo |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_grupos | clustered, unique, primary key located on PRIMARY | exagrp |

---

## Tabla: ea_kardex

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numkar | int | 4 | Secuencia Kardex |
| 2 | invnum | int | 4 | Secuencia de movimiento |
| 3 | codrea | varchar | 4 | Código reactivo |
| 4 | tipkar | varchar | 2 | Tipo Movimiento |
| 5 | feckar | datetime | 8 | Fecha de movimiento |
| 6 | stkrea | numeric | 9 | Stock de reactivo |
| 7 | qtyrea | numeric | 9 | Cantidad movida |
| 8 | usecod | int | 4 | Usuario |
| 9 | frarea | int | 4 | Unidad de fraccionamiento |
| 10 | codalm | varchar | 2 | Código de almacén |
| 11 | numitm | int | 4 | Secuencia que genera el movimiento |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_kardex | nonclustered, unique, primary key located on PRIMARY | numkar |

---

## Tabla: ea_movimientos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Secuencia de movimientos |
| 2 | codalm | varchar | 2 | Código de almacén |
| 3 | tipkar | varchar | 2 | Tipo Movimiento |
| 4 | numdoc | varchar | 10 | Número de documento |
| 5 | fecmov | datetime | 8 | Fecha de movimiento |
| 6 | usecod | int | 4 | Usuario |
| 7 | codalm2 | varchar | 2 | Almacén destino |
| 8 | codprv | varchar | 4 | Código proveedor |
| 9 | obsmov | varchar | 50 | Observaciones |
| 10 | moncod | varchar | 2 | Moneda |
| 11 | purnum | int | 4 | Orden de compra |
| 12 | totmov | numeric | 9 | Monto total del movimiento |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_movimientos | clustered, unique, primary key located on PRIMARY | invnum |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_ea_mov_ref_usuarios | FOREIGN KEY | usecod REFERENCES usuarios (usecod) |

---

## Tabla: ea_movimientos_detalle

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Secuencia Movimiento |
| 2 | numitm | int | 4 | Número de item del movimiento |
| 3 | codrea | varchar | 4 | Código de reactivo |
| 4 | codund | varchar | 2 | Unidad de medida |
| 5 | codalm | varchar | 2 | Código de almacén |
| 6 | tipkar | varchar | 2 | Tipo de movimiento |
| 7 | qtyrea | int | 4 | Cantidad |
| 8 | stkalm | int | 4 | Stock |
| 9 | costod | numeric | 9 | Costo en dólares |
| 10 | coscod_a | numeric | 9 | Costo de compra |
| 11 | fecven | datetime | 8 | Fecha de vencimiento |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_movimientos_detalle | clustered, unique, primary key located on PRIMARY | invnum, numitm |

---

## Tabla: ea_orden_compra_cabecera

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Secuencia orden de compra EA |
| 2 | codprv | varchar | 4 | Código de proveedor |
| 3 | ordfec | datetime | 8 | Fecha de orden |
| 4 | ordobs | text | 16 | Observaciones |
| 5 | ordtot | numeric | 9 | Total de la orden |
| 6 | ordstd | varchar | 1 | Estado |
| 7 | codalm | varchar | 2 | Código de almacén |
| 8 | ordtip | varchar | 2 | Tipo de orden |
| 9 | moncod | varchar | 2 | Moneda |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_orden_compra_cabecera | clustered, unique, primary key located on PRIMARY | invnum |

---

## Tabla: ea_orden_compra_detalle

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Secuencia Orden de compra EA |
| 2 | numitm | int | 4 | No. de línea o de item |
| 3 | codfab | varchar | 4 | Código de fabricante |
| 4 | codrea | varchar | 4 | Código de reactivo |
| 5 | qtyrea | int | 4 | Cantidad de reactivo |
| 6 | costod | numeric | 9 | Costo |
| 7 | itmsta | varchar | 1 | Estado del item |
| 8 | totpar | numeric | 9 | Total parcial |
| 9 | stkalm | numeric | 9 | Stock |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_orden_compra_detalle | clustered, unique, primary key located on PRIMARY | invnum, numitm |

---

## Tabla: ea_ordenes_cabecera

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | prfnum | int | 4 | Número de prefactura |
| 3 | tipcli | varchar | 2 | Tipo de cliente |
| 4 | cuscod | varchar | 7 | Código del cliente o paciente |
| 5 | cusnam | varchar | 30 | Nombre del cliente o paciente |
| 6 | cusadr | varchar | 45 | Dirección del cliente o paciente |
| 7 | datord | datetime | 8 | Fecha de la orden |
| 8 | usecod | int | 4 | Código de usuario |
| 9 | pinnum | varchar | 6 | Código de plan |
| 10 | estord | varchar | 1 | Estado de la Orden |
| 11 | totord | numeric | 9 | Total de la orden (sin impuestos) |
| 12 | netord | numeric | 9 | Neto de la orden (con impuestos) |
| 13 | igvord | numeric | 9 | Impuestos |
| 14 | codstd | varchar | 2 | Código de estado de la orden |
| 15 | medcod | varchar | 4 | Código del médico |
| 16 | parcod | varchar | 2 | Código de la categoría |
| 17 | tarcod | varchar | 8 | Código de tarifa |
| 18 | invgnc | numeric | 9 | Gastos no cubiertos |
| 19 | invppac | numeric | 9 | Pago del paciente |
| 20 | invpseg | numeric | 9 | Pago de la aseguradora |
| 21 | invcoa | numeric | 9 | Coaseguro |
| 22 | invigv | numeric | 9 | Impuestos |
| 23 | destot_n | numeric | 9 | Descuento por plan |
| 24 | mednam | varchar | 40 | Nombre del médico |
| 25 | ordobs | varchar | 255 | Observaciones |
| 26 | fecanu | datetime | 8 | Fecha de anulación |
| 27 | useanu | int | 4 | Código de usuario que anuló el documento |
| 28 | codalm | varchar | 2 | Código de almacén |
| 29 | numcon | int | 4 | Secuencia de Acto Médico |
| 30 | siscod | int | 4 | Establecimiento |
| 31 | totcos | numeric | 9 | Costo |
| 32 | totcos_d | numeric | 9 | Costo en dólares |
| 33 | totcos_e | numeric | 9 | Costo por establecimiento |
| 34 | totigv_p | numeric | 9 | Total igv pago paciente |
| 35 | totigv_c | numeric | 9 | Total igv pago cía de seguros |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| ind_ord_cbecera | nonclustered located on PRIMARY | cusnam |
| pk_ea_ordenes_cabecera | clustered, unique, primary key located on PRIMARY | invnum |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| ckc_codstd_ea_orden | CHECK Table Level | ([codstd] = 'AN' or [codstd] = 'FA' or [codstd] = 'GE') |
| ckc_estord_ea_orden | CHECK Table Level | ([estord] = 'A' or [estord] = 'P' or [estord] = 'G') |
| fk_ea_orden_ref_165_usuarios | FOREIGN KEY | usecod REFERENCES usuarios (usecod) |
| fk_ea_orden_ref_306_tipo_cli | FOREIGN KEY | tipcli REFERENCES tipo_cliente (tipcli) |
| fk_ea_ordenes_cabe_siscod | FOREIGN KEY | siscod REFERENCES sistema (siscod) |

---

## Tabla: ea_ordenes_detalle

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | numitm | int | 4 | Número de item |
| 3 | exacod | varchar | 6 | Código del examen |
| 4 | exades | varchar | 40 | Descripción del examen |
| 5 | estexa | varchar | 1 | Estado del resultado |
| 6 | exapri | numeric | 9 | Precio del examen |
| 7 | qtyexa | int | 4 | Cantidad |
| 8 | parexa | numeric | 9 | Parcial del examen |
| 9 | exadto | numeric | 9 | Descuento |
| 10 | igvexa | numeric | 9 | Impuestos |
| 11 | coaexa | numeric | 9 | Coaseguro |
| 12 | totpar | numeric | 9 | Total del examen |
| 13 | medcod | varchar | 4 | Código del médico (de resultados) |
| 14 | codalm | varchar | 2 | Código de almacén |
| 15 | tarcos | numeric | 9 | Costo |
| 16 | tarcos_d | numeric | 9 | Costo en dólares |
| 17 | exacos_e | numeric | 9 | Costo en establecimiento |
| 18 | exaapr | varchar | 1 | Aprobación del examen |
| 19 | invigv_p | numeric | 9 | IGV pago paciente |
| 20 | invigv_c | numeric | 9 | IGV pago cía de seguros |
| 21 | pritab | numeric | 9 | Tarifa Referencial |
| 22 | totppac | numeric | 9 | Total pago Paciente |
| 23 | totpseg | numeric | 9 | Total pago Aseguradora |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_ordenes_detalle | clustered, unique, primary key located on PRIMARY | invnum, numitm |

---

## Tabla: ea_perfiles

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codper | varchar | 4 | Código del perfil |
| 2 | desper | varchar | 40 | Descripción del perfil |
| 3 | cosper | numeric | 9 | Costo del perfil |
| 4 | dctper | numeric | 9 | Descuento del perfil |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_perfiles | clustered, unique, primary key located on PRIMARY | codper |

---

## Tabla: ea_perfiles_tablas

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codper | varchar | 4 | Código del perfil |
| 2 | exacod | varchar | 6 | Código de examen |
| 3 | qtyper | numeric | 9 | Cantidad |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_per_tablas | clustered, unique, primary key located on PRIMARY | codper, exacod |

---

## Tabla: ea_proveedores

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codprv | varchar | 4 | Código de proveedor |
| 2 | desprv | varchar | 40 | Descripción del Proveedor |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_proveedores | clustered, unique, primary key located on PRIMARY | codprv |

---

## Tabla: ea_proveedores_tabla

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codprv | varchar | 4 | Código de Proveedor |
| 2 | codfab | varchar | 4 | Código de fabricante |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_proveedores_tabla | clustered, unique, primary key located on PRIMARY | codprv, codfab |

---

## Tabla: ea_reactivos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codrea | varchar | 4 | Código de reactivo |
| 2 | desrea | varchar | 50 | Descripción |
| 3 | inirea | varchar | 10 | Iniciales |
| 4 | codund | varchar | 2 | Unidad de medida |
| 5 | codfab | varchar | 4 | Código de fabricante |
| 6 | frarea | int | 4 | Unidad de fraccionamiento |
| 7 | obsrea | varchar | 40 | Observaciones |
| 8 | costod | numeric | 9 | Costo real |
| 9 | fecven | datetime | 8 | Fecha de vencimiento |
| 10 | moncod | varchar | 2 | Código de moneda |
| 11 | codtip | varchar | 2 | Código de tipo |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_codrea | clustered, unique, primary key located on PRIMARY | codrea |

---

## Tabla: ea_resultados

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | numitm | int | 4 | Número de item |
| 3 | codexa | varchar | 6 | Código del examen |
| 4 | datres | datetime | 8 | Fecha del resultado |
| 5 | resexa | text | 16 | Resultado(s) |
| 6 | estres | varchar | 1 | Estado del resultado |
| 7 | medcod | varchar | 4 | Código del médico |
| 8 | exaapr | varchar | 1 | Aprobación del resultado |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| datres | nonclustered located on PRIMARY | datres |
| pk_ea_resultados | clustered, unique, primary key located on PRIMARY | invnum, numitm |

---

## Tabla: ea_stock_almacenes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codalm | varchar | 2 | Código de almacén |
| 2 | codrea | varchar | 4 | Código de reactivo |
| 3 | stkalm | int | 4 | Stock en almacén |
| 4 | stkmin | int | 4 | Stock mínimo |
| 5 | stkmax | int | 4 | Stock máximo |
| 6 | ubirea | varchar | 6 | Código de ubicación |
| 7 | tarcos | numeric | 9 | Costo |
| 8 | cospro_e | numeric | 9 | Costo promedio establecimiento |
| 9 | coscom_e | numeric | 9 | Costo compra establecimiento |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_stock_almacenes | nonclustered, unique, primary key located on PRIMARY | codalm, codrea |

---

## Tabla: ea_tarifario_tablas

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | exacod | varchar | 6 | Código del examen |
| 2 | parcod | varchar | 2 | Categoría del examen |
| 3 | tarval | numeric | 9 | Precio para la categoría |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_tari_tabla | clustered, unique, primary key located on PRIMARY | exacod, parcod |

---

## Tabla: ea_tipo_movimientos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | tipkar | varchar | 2 | Tipo de Movimiento |
| 2 | destka | varchar | 30 | Descripción del Movimiento |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_tipo_kardex | clustered, unique, primary key located on PRIMARY | tipkar |

---

## Tabla: ea_tipos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codtip | varchar | 2 | Tipo de Examen |
| 2 | destip | varchar | 20 | Descripción del Tipo |
| 3 | utitip | numeric | 9 | Utilidad por tipo de examen |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_tipos_codtip | clustered, unique, primary key located on PRIMARY | codtip |

---

## Tabla: ea_unidades

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codund | varchar | 2 | Código |
| 2 | desund | varchar | 20 | Descripción |
| 3 | iniund | varchar | 6 | Iniciales |
| 4 | facund | numeric | 9 | Factor |
| 5 | obdund | varchar | 40 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ea_codund | clustered, unique, primary key located on PRIMARY | codund |

---

## Tabla: emergencia

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | tipeme | varchar | 2 | Tipo de emergencia |
| 3 | pachis | varchar | 7 | Número de historia |
| 4 | pacnam | varchar | 40 | Nombre del paciente |
| 5 | sexcod | varchar | 2 | Código del sexo |
| 6 | emedir | varchar | 45 | Dirección del paciente |
| 7 | taccod | varchar | 2 | Procedencia |
| 8 | plnnum | varchar | 6 | Código del plan de atención |
| 9 | tppcod | varchar | 2 | Tipo de paciente |
| 10 | medcod | varchar | 4 | Código del médico |
| 11 | temtip | varchar | 2 | Tipo de atención |
| 12 | usecod | int | 4 | Código de usuario |
| 13 | contip | varchar | 2 | Código de condición del paciente (llegada) |
| 14 | prfnum | int | 4 | Número de prefactura |
| 15 | stdeme | varchar | 1 | Estado de emergencia (Ingreso, Egreso) |
| 16 | egrcod | varchar | 2 | Código de tipo de egreso del paciente |
| 17 | emeing | datetime | 8 | Fecha de ingreso |
| 18 | emeobs | text | 16 | Observaciones |
| 19 | parcod | varchar | 2 | Categoría de pago |
| 20 | emecoa | numeric | 9 | Porcentaje de coaseguro |
| 21 | emeded | numeric | 9 | Monto del deducible |
| 22 | emefno | varchar | 40 | Nombre del responsable |
| 23 | emefte | varchar | 16 | Teléfono del responsable |
| 24 | emefdi | varchar | 40 | Dirección del responsable |
| 25 | codbar | varchar | 13 | Código de barras |
| 26 | cobmax | numeric | 9 | Cobertura máxima |
| 27 | tarcod | varchar | 8 | Código de tarifa |
| 28 | emecar | varchar | 10 | Carta de beneficios |
| 29 | emefca | datetime | 8 | Fecha de vigencia de la solicitud |
| 30 | fecanu | datetime | 8 | Fecha de anulación |
| 31 | useanu | int | 4 | Código de usuario que anuló el documento |
| 32 | concod | varchar | 4 | Código de la cía contratante |
| 33 | segcod | varchar | 4 | Código de la cía de seguros |
| 34 | siscod | int | 4 | Establecimiento |
| 35 | tarcos | numeric | 9 | Costo en tarifario |
| 36 | tarcos_d | numeric | 9 | Costo en tarifario en dólares |
| 37 | emepic | varchar | 80 | Foto paciente |
| 38 | tarcos_e | numeric | 9 | Costo del establecimiento |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| index_pachis | nonclustered located on PRIMARY | prfnum |
| index_pacnam | nonclustered located on PRIMARY | pacnam |
| pk_emergencia | nonclustered, unique, primary key located on PRIMARY | invnum |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| DF_Emergenci_tarco_3A3865BB | DEFAULT | on column tarcos_e (0) |
| fk_emergenc_ref_137_tipo_ate | FOREIGN KEY | temtip REFERENCES tipo_atencion_emergencia (temtip) |
| fk_emergenc_ref_176_paciente | FOREIGN KEY | pachis REFERENCES pacientes (pachis) |
| fk_emergenc_ref_208_sexo | FOREIGN KEY | sexcod REFERENCES sexo (sexcod) |
| fk_emergenc_ref_26_tipo_aco | FOREIGN KEY | taccod REFERENCES tipo_acompanante (taccod) |
| fk_emergenc_ref_445_usuarios | FOREIGN KEY | usecod REFERENCES usuarios (usecod) |
| fk_emergenc_ref_769_tipos_eg | FOREIGN KEY | egrcod REFERENCES tipos_egreso (egrcod) |
| fk_emergencia_siscod | FOREIGN KEY | siscod REFERENCES sistema (siscod) |

---

## Tabla: emergencia_epicrisis

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | epifec | datetime | 8 | Fecha de la epicrisis |
| 3 | usecod | int | 4 | Código de usuario |
| 4 | epitxt | text | 16 | Epicrisis |
| 5 | plantxt | text | 16 | Plantilla de epicrisis |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_emer_epicrisis | nonclustered, unique, primary key located on PRIMARY | invnum |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_emer_epicrisis_ref_use | FOREIGN KEY | usecod REFERENCES usuarios (usecod) |

---

## Tabla: emergencia_movimientos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | ttecod | varchar | 2 | Tipo de movimiento en emergencia |
| 3 | emefec | datetime | 8 | Fecha de movimiento |
| 4 | medcod | varchar | 4 | Código del médico (autorizador) |
| 5 | diacod | varchar | 6 | Código de diagnóstico (principal) |
| 6 | diacod1 | varchar | 6 | Código de diagnóstico (2do) |
| 7 | diacod2 | varchar | 6 | Código de diagnóstico (3ro) |
| 8 | emeobs | text | 16 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_emergencia_movimientos | nonclustered, unique, primary key located on PRIMARY | invnum, ttecod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_emergenc_ref_118_medicos | FOREIGN KEY | medcod REFERENCES medicos (medcod) |
| fk_emergenc_ref_616_tipo_tab | FOREIGN KEY | ttecod REFERENCES tipo_tabla_emergencia (ttecod) |

---

## Tabla: estado_cama

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | stacam | varchar | 2 | Estado de la cama (OC/DI) |
| 2 | descam | varchar | 15 | Descripción de la cama |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_estado_cama | clustered, unique, primary key located on PRIMARY | stacam |

---

## Tabla: estado_civil

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | ecicod | varchar | 2 | Código de estado civil |
| 2 | ecides | varchar | 25 | Descripción de estado civil |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_estado_civil | clustered, unique, primary key located on PRIMARY | ecicod |

---

## Tabla: estado_documentos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codstd | varchar | 2 | Código de estado de documentos |
| 2 | destad | varchar | 20 | Descripción del estado de documentos |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_estado_documento | nonclustered, unique located on PRIMARY | codstd |

---

## Tabla: estado_historia

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | eshcod | varchar | 2 | Código de estado de la historia |
| 2 | eshdes | varchar | 25 | Descripción del estado de la historia |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_estado_historia | clustered, unique, primary key located on PRIMARY | eshcod |

---

## Tabla: estado_tramite_facturas

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | esfcod | varchar | 2 | Código |
| 2 | esfdes | varchar | 20 | Descripción |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_estado_tramite_facturas | clustered, unique, primary key located on PRIMARY | esfcod |

---

## Tabla: examenes_costos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | siscod | int | 4 | Establecimiento |
| 2 | exacod | varchar | 6 | Código de examen |
| 3 | exacos_e | numeric | 9 | Costo establecimiento |
| 4 | exacop_e | numeric | 9 | Costo promedio establecimiento |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_examenes_costos | clustered, unique, primary key located on PRIMARY | siscod, exacod |

---

## Tabla: fa_almacenes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codalm | varchar | 2 | Código de almacén |
| 2 | desalm | varchar | 20 | Descripción del almacén |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_almacenes | clustered, unique, primary key located on PRIMARY | codalm |

---

## Tabla: fa_almacenes_tipo

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codalm | varchar | 2 | Código de Tipo |
| 2 | tipalm | varchar | 1 | Descripción |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_almacenes_tipo_codalm | clustered, unique, primary key located on PRIMARY | codalm |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| chk_fa_almacenes_tipo_tipalm | CHECK Table Level | ([tipalm] = 'F' or [tipalm] = 'M') |

---

## Tabla: fa_centro_costo

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codccs | varchar | 2 | Código de centro de costo farmacia |
| 2 | desccs | varchar | 20 | Descripción de centro de costo |
| 3 | obsccs | varchar | 20 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_centro_costos | nonclustered, unique, primary key located on PRIMARY | codccs |

---

## Tabla: fa_clientes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codcli | varchar | 7 | Código del cliente |
| 2 | nomcli | varchar | 30 | Nombre del cliente |
| 3 | ctcfar | varchar | 2 | Estado de cuenta en farmacia |
| 4 | dircli | varchar | 40 | Dirección del cliente |
| 5 | telcli | varchar | 25 | Teléfono del cliente |
| 6 | codalt | varchar | 10 | Código alterno (otros) |
| 7 | estcli | varchar | 1 | Estado de vigencia del cliente |
| 8 | pinnum | varchar | 6 | Plan asociado |
| 9 | crecli | varchar | 1 | Evaluación del crédito |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_clientes_1 | clustered, unique, primary key located on PRIMARY | codcli |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_tipo_cliente_1 | FOREIGN KEY | ctcfar REFERENCES .fa_tipo_cliente (ctcfar) |

---

## Tabla: fa_codigo_distribucion_compra

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | coddcom | varchar | 4 | Código de distribución |
| 2 | desdcom | varchar | 40 | Descripción |
| 3 | equdcom | varchar | 30 | Equivalencia |
| 4 | obsdcom | varchar | 40 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_cod_com_coddcom | clustered, unique, primary key located on PRIMARY | coddcom |

---

## Tabla: fa_control_vencimiento

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Secuencia de Movimiento |
| 2 | fecpro | datetime | 8 | Fecha de Proceso |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_invnum_fa_control_venci | clustered, unique, primary key located on PRIMARY | invnum |

---

## Tabla: fa_cotizacion_cabecera

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | codprc | varchar | 2 | Código de procedencia |
| 3 | nomcli | varchar | 40 | Nombre del cliente o paciente |
| 4 | feccot | datetime | 8 | Fecha de cotización |
| 5 | fecvig | datetime | 8 | Fecha de vigencia |
| 6 | stacot | varchar | 1 | Código de estado de Cotización |
| 7 | totcot | numeric | 9 | Total cotización |
| 8 | igvcot | numeric | 9 | Impuestos |
| 9 | netcot | numeric | 9 | Neto |
| 10 | codate | varchar | 8 | Código de atención (referencia) |
| 11 | codped | varchar | 14 | Código de pedido |
| 12 | prfnum | int | 4 | Número de prefactura |
| 13 | codalm | varchar | 2 | Código de almacén |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| nomcli | nonclustered located on PRIMARY | nomcli |
| pk_fa_cotizacion_cabecera | clustered, unique, primary key located on PRIMARY | invnum |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_fa_cotiz_ref_93_fa_proce | FOREIGN KEY | codprc REFERENCES .fa_cotizacion_procedencia (codprc) |

---

## Tabla: fa_cotizacion_detalle

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | coditm | int | 4 | Número de item |
| 3 | codpro | varchar | 5 | Código del producto |
| 4 | despro | varchar | 30 | Descripción del producto |
| 5 | qtypro | int | 4 | Cantidad entera |
| 6 | qtypro_m | int | 4 | Cantidad menudeo |
| 7 | dtopro | numeric | 9 | Porcentaje de descuento |
| 8 | prisal | numeric | 9 | Precio |
| 9 | totpar | numeric | 9 | Total parcial |
| 10 | stkfra | numeric | 9 | Fracción del producto |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_cotiza_detalle | clustered, unique, primary key located on PRIMARY | invnum, coditm |

---

## Tabla: fa_cotizacion_procedencia

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codprc | varchar | 2 | Código de procedencia de cotización |
| 2 | desprc | varchar | 20 | Descripción de procedencia |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_procedencia | clustered, unique, primary key located on PRIMARY | codprc |

**Referencia desde:**
- `fa_cotizacion_cabecera`: `fk_fa_cotiz_ref_9`

---

## Tabla: fa_entidades

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codent | varchar | 7 | Código de entidad clínica |
| 2 | desent | varchar | 40 | Descripción de entidad clínica |
| 3 | obsent | varchar | 40 | Observaciones |
| 4 | codfam | varchar | 7 | Código de almacén |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_entidades | nonclustered, unique, primary key located on PRIMARY | codent |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_fa_entid_ref_49_fa_famil | FOREIGN KEY | codfam REFERENCES .fa_familias (codfam) |

---

## Tabla: fa_faltantes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numfal | int | 4 | Número de secuencia |
| 2 | datfal | datetime | 8 | Fecha de petición |
| 3 | usecod | int | 4 | Código de usuario |
| 4 | codpro | varchar | 5 | Código del producto |
| 5 | qtypro | int | 4 | Cantidad entera |
| 6 | qtypro_m | int | 4 | Cantidad menudeo |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_faltantes | nonclustered, unique, primary key located on PRIMARY | numfal |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_fa_produc_fal | FOREIGN KEY | codpro REFERENCES .fa_productos (codpro) |
| fk_fa_usuari_fal | FOREIGN KEY | usecod REFERENCES usuarios (usecod) |

---

## Tabla: fa_familias

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codfam | varchar | 7 | Código de familia |
| 2 | desfam | varchar | 40 | Descripción de la familia |
| 3 | obsfam | varchar | 40 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_familias | nonclustered, unique, primary key located on PRIMARY | codfam |

**Referencia desde:**
- `.fa_entidades`: `fk_fa_entid_ref_49_fa_famil`
- `.fa_productos`: `fk_fa_produ_ref_37_fa_famil`
- `.fa_sintomas`: `fk_fa_sinto_ref_52_fa_famil`

---

## Tabla: fa_genericos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codgen | varchar | 7 | Código de genérico |
| 2 | desgen | varchar | 40 | Descripción del genérico |
| 3 | obsgen | varchar | 40 | Observaciones |
| 4 | tipgen | varchar | 2 | Tipo Vac |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_genericos | nonclustered, unique, primary key located on PRIMARY | codgen |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| chk_tipgen | CHECK on column tipgen | ([tipgen] = 'NC' or [tipgen] = 'CO') |
| DF_fa_generi_tipge_78BFA819 | DEFAULT | on column tipgen ('CO') |

**Referencia desde:**
- `fa_productos`: `fk_fa_produ_ref_46_fa_gener`

---

## Tabla: fa_informacion

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codinf | varchar | 5 | Código de información |
| 2 | desinf | varchar | 30 | Descripción de la información |
| 3 | picinf | varchar | 13 | Gráfico del producto(s) asociado |
| 4 | txtinf | text | 16 | Contenido de la información |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_informaci | nonclustered, unique, primary key located on PRIMARY | codinf |

**Referencia desde:**
- `fa_productos`: `fk_fa_produ_ref_43_fa_infor`

---

## Tabla: fa_kardex

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | numkar | int | 4 | Secuencia de kardex |
| 2 | invnum | int | 4 | Secuencia de origen |
| 3 | codpro | varchar | 5 | Código del producto |
| 4 | codalm | varchar | 2 | Código de almacén |
| 5 | tipkar | varchar | 2 | Tipo de movimiento en kardex |
| 6 | feckar | datetime | 8 | Fecha del movimiento |
| 7 | stkalm | int | 4 | Stock en almacén entero |
| 8 | stkalm_m | int | 4 | Stock en almacén menudeo |
| 9 | qtypro | int | 4 | Cantidad entera |
| 10 | qtypro_m | int | 4 | Cantidad menudeo |
| 11 | descto | numeric | 9 | Descuento |
| 12 | prisal | numeric | 9 | Precio |
| 13 | usecod | int | 4 | Código de usuario |
| 14 | stkfra | int | 4 | Fracción del producto |
| 15 | cospro_d | numeric | 9 | Costo promedio en dólares |
| 16 | cospro_s | numeric | 9 | Costo promedio en soles |
| 17 | fecdoc | datetime | 8 | Fecha de documento |
| 18 | modfar | varchar | 2 | Módulo de farmacia |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| ind_feckar | nonclustered located on PRIMARY | feckar |
| pk_fa_kardex_01 | nonclustered, unique, primary key located on PRIMARY | numkar |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_fa_productos_k | FOREIGN KEY | codpro REFERENCES .fa_productos (codpro) |
| fk_fa_tip_movim_k | FOREIGN KEY | tipkar REFERENCES .fa_tipo_movimientos (tipkar) |
| fk_fa_usuarios_k | FOREIGN KEY | usecod REFERENCES usuarios (usecod) |

---

## Tabla: fa_laboratorios

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codlab | varchar | 4 | Código de laboratorio |
| 2 | deslab | varchar | 40 | Nombre del laboratorio |
| 3 | obslab | varchar | 40 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_laboratorios | nonclustered, unique, primary key located on PRIMARY | codlab |

**Referencia desde:**
- `fa_orden_compra_detalle`: `fk_fa_orden_ref_`
- `fa_productos`: `fk_fa_produ_ref_40_fa_labor`
- `.fa_proveedores_tabla`: `fk_fa_prove_ref_109`

---

## Tabla: fa_movimientos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | codalm | varchar | 2 | Código de almacén |
| 3 | tipkar | varchar | 2 | Tipo de movimiento |
| 4 | numdoc | varchar | 21 | Número de documento |
| 5 | purnum | int | 4 | Número de orden de compra |
| 6 | fecdoc | datetime | 8 | Fecha de la orden de compra |
| 7 | fecmov | datetime | 8 | Fecha del movimiento |
| 8 | tpacod | varchar | 2 | Tipo de paciente |
| 9 | totmov | numeric | 9 | Total movimiento |
| 10 | dtomov | numeric | 9 | Descuento |
| 11 | bonmov | numeric | 9 | Bonificación |
| 12 | igvmov | numeric | 9 | Impuestos |
| 13 | vvfmov | numeric | 9 | Valor venta farmacia |
| 14 | obsmov | varchar | 50 | Observaciones |
| 15 | usecod | int | 4 | Código de usuario |
| 16 | codalm2 | varchar | 2 | Código de almacén (transferencia) |
| 17 | ubipro | varchar | 2 | Ubicación |
| 18 | codprv | varchar | 4 | Código del proveedor |
| 19 | dtopro1 | numeric | 9 | Descuento 1 |
| 20 | dtopro2 | numeric | 9 | Descuento 2 |
| 21 | moncod | varchar | 2 | Código de Moneda |
| 22 | codccs | varchar | 2 | Código de centro de costo |
| 23 | tipcam | numeric | 9 | Tipo de cambio |
| 24 | invkar | int | 4 | Secuencia de Kardex |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| codprv | nonclustered located on PRIMARY | codprv |
| numdoc | nonclustered located on PRIMARY | numdoc |
| pk_fa_movimientos | nonclustered, unique, primary key located on PRIMARY | invnum |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| pk_fa_movi_ref_tipomov | FOREIGN KEY | tipkar REFERENCES .fa_tipo_movimientos (tipkar) |
| pk_fa_movi_ref_usuario | FOREIGN KEY | usecod REFERENCES usuarios (usecod) |

**Referencia desde:**
- `.fa_movimientos_detalle`: `pk_fa_movi_ref_mo`

---

## Tabla: fa_movimientos_detalle

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | numitm | int | 4 | Número de item |
| 3 | codpro | varchar | 5 | Código de producto |
| 4 | qtypro | int | 4 | Cantidad entera |
| 5 | qtppro | int | 4 | Cantidad producto |
| 6 | qtbpro | int | 4 | Cantidad de bonificación |
| 7 | prisal | numeric | 9 | Precio de venta al público |
| 8 | vvfsal | numeric | 9 | Valor venta farmacia |
| 9 | pvfsal | numeric | 9 | Precio de venta a farmacia |
| 10 | dtopro | numeric | 9 | Descuentos |
| 11 | fecven | datetime | 8 | Fecha de venta |
| 12 | stkalm | int | 4 | Stock en almacén entera |
| 13 | stkalm_m | int | 4 | Stock en almacén menudeo |
| 14 | parpro | numeric | 9 | Parcial del producto |
| 15 | totpro | numeric | 9 | Total producto |
| 16 | covtip | varchar | 1 | Estado de cambio de precios |
| 17 | igvpro | numeric | 9 | Impuestos |
| 18 | dtopro1 | numeric | 9 | Descuentos % 1 |
| 19 | dtopro2 | numeric | 9 | Descuentos % 2 |
| 20 | dtopro3 | numeric | 9 | Descuentos % 3 |
| 21 | dtopro4 | numeric | 9 | Descuentos % 4 |
| 22 | costod | numeric | 9 | Valor venta farmacia (actual) |
| 23 | coscom | numeric | 9 | Costo de compra |
| 24 | stamod | varchar | 1 | Estado de modificación de precios |
| 25 | qtypro_m | int | 4 | Cantidad en menudeo |
| 26 | usecod | int | 4 | Código de usuario |
| 27 | tipkar | varchar | 2 | Tipo de movimiento en kardex |
| 28 | stkfra | int | 4 | Fracción del producto |
| 29 | codalm | varchar | 2 | Código de almacén |
| 30 | cospro | numeric | 9 | Costo del producto |
| 31 | cvvf | numeric | 9 | VVF nuevo |
| 32 | cpvf | numeric | 9 | PVF nuevo |
| 33 | cprisal2 | numeric | 9 | Precio nuevo |
| 34 | coscom_d | numeric | 9 | Costo compra en dólares |
| 35 | cospro_d | numeric | 9 | Costo promedio en dólares |
| 36 | codlot | varchar | 15 | Código de lotes |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_movimientos_detalle | nonclustered, unique, primary key located on PRIMARY | invnum, numitm |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| DF_fa_movimi_codlo_0ABE5CC3 | DEFAULT | on column codlot (0) |
| pk_fa_movi_ref_movimiento | FOREIGN KEY | invnum REFERENCES .fa_movimientos (invnum) |
| pk_fa_movi_ref_productos | FOREIGN KEY | codpro REFERENCES .fa_productos (codpro) |

---

## Tabla: fa_orden_compra_cabecera

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | codprv | varchar | 4 | Código de proveedor |
| 3 | ordfec | datetime | 8 | Fecha de la orden de compra |
| 4 | ordobs | text | 16 | Observaciones |
| 5 | ordtot | numeric | 9 | Total de la orden (sin impuestos) |
| 6 | ordnet | numeric | 9 | Neto de la orden (con impuestos) |
| 7 | ordigv | numeric | 9 | Impuestos |
| 8 | ordstd | varchar | 1 | Estado de la orden |
| 9 | tippag | varchar | 2 | Tipo de pago |
| 10 | codalm | varchar | 2 | Código de almacén |
| 11 | ordtip | varchar | 2 | Código de tipo de orden |
| 12 | moncod | varchar | 2 | Código de moneda |
| 13 | coddcom | varchar | 4 | Código de Tipo de Orden |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_orden_compra_cabecera | clustered, unique, primary key located on PRIMARY | invnum |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_fa_orden_ref_31_fa_prove | FOREIGN KEY | codprv REFERENCES .fa_proveedores (codprv) |

**Referencia desde:**
- `.fa_orden_compra_detalle`: `fk_fa_orden_ref_`

---

## Tabla: fa_orden_compra_detalle

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | numitm | int | 4 | Número de item |
| 3 | codlab | varchar | 4 | Código de laboratorio |
| 4 | codpro | varchar | 5 | Código de producto |
| 5 | qtypro | int | 4 | Cantidad entera |
| 6 | qtybon | int | 4 | Cantidad de bonificación |
| 7 | costod | numeric | 9 | Precio de venta a farmacia |
| 8 | itmsta | varchar | 1 | Estado del item |
| 9 | pordsc1 | numeric | 9 | Descuento % 1 |
| 10 | pordsc2 | numeric | 9 | Descuento % 2 |
| 11 | pordsc3 | numeric | 9 | Descuento % 3 |
| 12 | pordsc4 | numeric | 9 | Descuento % 4 |
| 13 | coscom | numeric | 9 | Costo de compra |
| 14 | totpar | numeric | 9 | Total parcial |
| 15 | stkalm | numeric | 9 | Stock en almacén entera |
| 16 | stkalm_m | numeric | 9 | Stock en almacén menudeo |
| 17 | qtyppro | int | 4 | Cantidad pedida del producto |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_orden_compra_detalle | clustered, unique, primary key located on PRIMARY | invnum, numitm |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_fa_orden_ref_121_fa_labor | FOREIGN KEY | codlab REFERENCES .fa_laboratorios (codlab) |
| fk_fa_orden_ref_124_fa_produ | FOREIGN KEY | codpro REFERENCES .fa_productos (codpro) |
| fk_fa_orden_ref_25_fa_orden | FOREIGN KEY | invnum REFERENCES .fa_orden_compra_cabecera (invnum) |

---

## Tabla: fa_paquetes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | nompaq | varchar | 50 | Descripción del paquete |
| 3 | codalm | varchar | 2 | Código de almacén |
| 4 | obspaq | varchar | 50 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_paquete | nonclustered, unique, primary key located on PRIMARY | invnum |

**Referencia desde:**
- `.fa_paquetes_productos`: `pk_fa_paq_pro_ref_`

---

## Tabla: fa_paquetes_productos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | numitm | int | 4 | Número de item |
| 3 | codpro | varchar | 5 | Código del producto |
| 4 | despro | varchar | 30 | Descripción del producto |
| 5 | qtypro | int | 4 | Cantidad entera |
| 6 | qtypro_m | int | 4 | Cantidad menudeo |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_fa_paquete_pro | nonclustered, unique, primary key located on PRIMARY | invnum, numitm |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| pk_fa_paq_pro_ref_paq | FOREIGN KEY | invnum REFERENCES .fa_paquetes (invnum) |
| pk_fa_paq_pro_ref_pro | FOREIGN KEY | codpro REFERENCES .fa_productos (codpro) |

---

## Tabla: fa_productos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codpro | varchar | 5 | Código del producto |
| 2 | despro | varchar | 30 | Descripción del producto |
| 3 | codlab | varchar | 4 | Código de laboratorio |
| 4 | codgen | varchar | 7 | Código de genérico |
| 5 | codfam | varchar | 7 | Código de familia |
| 6 | prisal | numeric | 9 | Precio de venta al público |
| 7 | codinf | varchar | 5 | Código de información |
| 8 | tmppro | varchar | 7 | Código de tipo o línea del producto |
| 9 | codtip | varchar | 1 | Valor de venta a farmacia |
| 10 | costod | numeric | 9 | Costo |
| 11 | cospro | numeric | 9 | Costo promedio del producto |
| 12 | datmov | datetime | 8 | Fecha de movimiento |
| 13 | datpri | datetime | 8 | Fecha de actualización de precio |
| 14 | datinc | datetime | 8 | Fecha de registro del producto |
| 15 | datven | datetime | 8 | Fecha de vencimiento |
| 16 | prosta | varchar | 1 | Estado de vigencia del producto |
| 17 | dtopro | numeric | 9 | Descuento del producto |
| 18 | datind | datetime | 8 | Fecha de inicio de descuento |
| 19 | datfid | datetime | 8 | Fecha de fin de descuento |
| 20 | qtypur | int | 4 | Cantidad de la última compra |
| 21 | codbar | varchar | 16 | Código de barras |
| 22 | costod_r | numeric | 9 | Costo de compra del producto |
| 23 | stkfra | int | 4 | Fracción del producto |
| 24 | igvpro | numeric | 9 | Impuestos |
| 25 | utipro | numeric | 9 | Porcentaje de utilidad |
| 26 | moncod | varchar | 2 | Código de moneda |
| 27 | coscom | numeric | 9 | Costo de compra del producto |
| 28 | codpro_m | varchar | 12 | Código de producto alterno 1 |
| 29 | codpro_a | varchar | 16 | Código alterno 2 (INTERNET) |
| 30 | coscom_d | numeric | 9 | Costo de compra en dólares |
| 31 | cospro_d | numeric | 9 | Costo promedio en dólares |
| 32 | despre | varchar | 30 | Presentación |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| ind_despro | nonclustered located on PRIMARY | despro |
| pk_fa_productos | nonclustered, unique, primary key located on PRIMARY | codpro |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| ckc_stkfra_productos | CHECK on column stkfra | ([stkfra] > 0) |
| fk_fa_produ_ref_37_fa_famil | FOREIGN KEY | codfam REFERENCES .fa_familias (codfam) |
| fk_fa_produ_ref_37_fa_tipos | FOREIGN KEY | codtip REFERENCES .fa_tipos (codtip) |
| fk_fa_produ_ref_40_fa_labor | FOREIGN KEY | codlab REFERENCES .fa_laboratorios (codlab) |
| fk_fa_produ_ref_43_fa_infor | FOREIGN KEY | codinf REFERENCES .fa_informacion (codinf) |
| fk_fa_produ_ref_46_fa_gener | FOREIGN KEY | codgen REFERENCES .fa_genericos (codgen) |

**Referencia desde:**
- `am_farmacos`: `fk_am_farmacos_codpro`
- `fa_faltantes`: `fk_fa_produc_fal`

---

## Tabla: liquidacion_detalle

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | numitm | int | 4 | Número de secuencia |
| 3 | nomcli | varchar | 40 | Nombre del cliente |
| 4 | facnum | int | 4 | Número de factura |
| 5 | prfnum | int | 4 | Prefactura |
| 6 | invnum_r | int | 4 | Secuencia de referencia |
| 7 | facfec | datetime | 8 | Fecha de factura |
| 8 | factot | numeric | 9 | Total de la factura |
| 9 | honstd | varchar | 1 | Estado del documento |
| 10 | prfitm | int | 4 | Item Prefactura |
| 11 | itmsta | varchar | 1 | Estado Item |
| 12 | stapri | varchar | 1 | stapri |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_liquidacion_detalle | clustered, unique, primary key located on PRIMARY | invnum, numitm |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| DF_liquidaci_itmst_62D066FA | DEFAULT | on column itmsta ('G') |
| df_stapri_liquidacion_detalle | DEFAULT | on column stapri ('N') |
| fk_liquidac_ref_18_liquidac | FOREIGN KEY | invnum REFERENCES liquidacion_cabecera (invnum) |
| fk_liquidac_ref_52_prefactu | FOREIGN KEY | prfnum REFERENCES prefacturas (prfnum) |

---

## Tabla: medicos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | medcod | varchar | 4 | Código del médico |
| 2 | mednam | varchar | 40 | Nombre del médico |
| 3 | tprcod | varchar | 2 | Tipo de especialidad |
| 4 | sercod | varchar | 4 | Código de servicio |
| 5 | medcol | varchar | 20 | Número de Colegiatura |
| 6 | meddir | varchar | 45 | Dirección del médico |
| 7 | ubicod | varchar | 6 | Código de ubicación |
| 8 | medfon | numeric | 9 | Fonavi |
| 9 | medigv | numeric | 9 | Impuestos |
| 10 | trecod | varchar | 2 | Código de tipo de profesional |
| 11 | medfin | datetime | 8 | Fecha de ingreso |
| 12 | comcod | varchar | 2 | Tipo de contrato |
| 13 | medobs | varchar | 30 | Observaciones |
| 14 | medppv | numeric | 9 | Porcentaje de pacientes privados (no hábiles) |
| 15 | medruc | varchar | 8 | Número de RUC |
| 16 | medtel | varchar | 25 | Teléfono del médico |
| 17 | medsta | varchar | 1 | Estado de vigencia del médico |
| 18 | codcon | varchar | 4 | Código de consultorio |
| 19 | honpar | varchar | 2 | Tipo de pago por honorarios profesionales |
| 20 | tipprs | varchar | 1 | Tipo de Staff |
| 21 | ruc | varchar | 20 | Nuevo RUC |
| 22 | usecodx | int | 4 | Usuario Asociado |
| 23 | numcit | int | 4 | No. de Citados |
| 24 | medffi | datetime | 8 | Fecha Inicio de ausencia |
| 25 | medffn | datetime | 8 | Fecha Fin de ausencia |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_medicos | clustered, unique, primary key located on PRIMARY | medcod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_medicos_ref_31_servicio | FOREIGN KEY | sercod REFERENCES servicios (sercod) |
| fk_medicos_ref_40_tipo_pro | FOREIGN KEY | tprcod REFERENCES .tipo_profesional (tprcod) |

**Referencias desde:**
- `.am_consulta`: `fk_am_consu_ref_529_medicos`
- `.citas`: `fk_citas_ref_107_medicos`
- `.citas_ea`: `fk_medcod_citas_ea_medicos`
- `.emergencia_movimientos`: `fk_emergenc_ref_1`
- `.fa_productos_top`: `fk_medcod`
- `honorarios_movimientos`: `fk_honorari_ref_3`
- `hospitalizacion_movimientos`: `fk_hospital_`
- `.incapacidades`: `fk_medcod_incapacidades_me`
- `.intervenciones_participantes`: `fk_int_par_`
- `.liquidacion_cabecera`: `fk_liquidac_ref_40_`
- `.medicos_tabla`: `fk_medicos_ref_37_medicos`
- `medicos_tabla_tarifario`: `fk_medicos_ref_`
- `medicos_tabla_tipo_tarifa`: `fk_medicos_re`
- `movimientos_historias`: `fk_mov_histori_med`
- `prefacturas`: `fk_prefactu_ref_228_medicos`
- `turnos`: `fk_turnos_ref_104_medicos`
- `turnos_ea`: `fk_turnos_ea_medicos`
- `turnos_servicios`: `fk_turnos_ser_ref_104_m`

---

## Tabla: medicos_almacenes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | medcod | varchar | 4 | Código Médico |
| 2 | codalm | varchar | 2 | Código Almacén |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_medicos_almacenes | clustered, unique, primary key located on PRIMARY | medcod, codalm |

---

## Tabla: medicos_tabla

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | medcod | varchar | 4 | Código de médico |
| 2 | ttacod | varchar | 2 | Tipo de cobro |
| 3 | medmcr | numeric | 9 | Monto por pago al crédito |
| 4 | medmco | numeric | 9 | Monto por pago al contado |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_medicos_tabla | clustered, unique, primary key located on PRIMARY | medcod, ttacod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_medicos_ref_116_tipo_tar | FOREIGN KEY | ttacod REFERENCES tipo_tarifa (ttacod) |
| fk_medicos_ref_37_medicos | FOREIGN KEY | medcod REFERENCES medicos (medcod) |

---

## Tabla: medicos_tabla_intervencion

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | medcod | varchar | 4 | Código de médico |
| 2 | tippar | varchar | 2 | Código de tipo de participante |
| 3 | partip | varchar | 1 | Estado de participante |
| 4 | medmon | numeric | 9 | Monto del médico |
| 5 | medpor | numeric | 9 | Porcentaje de intervención |
| 6 | moncod | varchar | 2 | Código de moneda |
| 7 | medpri | numeric | 9 | medpri |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_medicos_t_intervencion | clustered, unique, primary key located on PRIMARY | medcod, tippar |

---

## Tabla: medicos_tabla_parametros

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | medcod | varchar | 4 | Código de médico |
| 2 | codparm | varchar | 2 | Concepto del Parámetro |
| 3 | tipparm | varchar | 2 | Tipo de Parámetro (+/-) |
| 4 | partip | varchar | 1 | Flag forma de pago (Monto, Porcentaje) |
| 5 | medmon | numeric | 9 | Monto del Médico |
| 6 | medpor | numeric | 9 | Porcentaje de pago |
| 7 | moncod | varchar | 2 | Código de moneda |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_medicos_t_parametros | clustered, unique, primary key located on PRIMARY | medcod, codparm |

---

## Tabla: medicos_tabla_tarifario

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | medcod | varchar | 4 | Código del Médico |
| 2 | tarcod | varchar | 8 | Código de la tarifa |
| 3 | partip | varchar | 1 | Flag forma de pago (Monto, Porcentaje) |
| 4 | medmcr | numeric | 9 | Monto al Crédito |
| 5 | medmco | numeric | 9 | Monto al Contado |
| 6 | medpcr | numeric | 9 | Porcentaje Crédito |
| 7 | medpco | numeric | 9 | Porcentaje Contado |
| 8 | moncod | varchar | 2 | Código de moneda |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_medicos_t_tarifario | clustered, unique, primary key located on PRIMARY | medcod, tarcod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_medicos_ref_118_tipo_tar | FOREIGN KEY | tarcod REFERENCES tarifario (tarcod) |
| fk_medicos_ref_39_medicos | FOREIGN KEY | medcod REFERENCES medicos (medcod) |

---

## Tabla: medicos_tabla_tipo_tarifa

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | medcod | varchar | 4 | Código del Médico |
| 2 | ttacod | varchar | 2 | Tipo de Tarifa |
| 3 | partip | varchar | 1 | Flag forma de pago (Monto, Porcentaje) |
| 4 | medmcr | numeric | 9 | Monto al Crédito |
| 5 | medmco | numeric | 9 | Monto al Contado |
| 6 | medpcr | numeric | 9 | Porcentaje Crédito |
| 7 | medpco | numeric | 9 | Porcentaje Contado |
| 8 | moncod | varchar | 2 | Código de moneda |
| 9 | porpri | numeric | 9 | porpri |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_medicos_t_tipo_tarifa | clustered, unique, primary key located on PRIMARY | medcod, ttacod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| df_propri_medicos_tabla | DEFAULT | on column porpri (100) |
| fk_medicos_ref_117_tipo_tar | FOREIGN KEY | ttacod REFERENCES tipo_tarifa (ttacod) |
| fk_medicos_ref_38_medicos | FOREIGN KEY | medcod REFERENCES medicos (medcod) |

---

## Tabla: moneda

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | moncod | varchar | 2 | Código de moneda |
| 2 | mondes | varchar | 25 | Descripción de moneda |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_moneda | clustered, unique, primary key located on PRIMARY | moncod |

**Referencias desde:**
- `tarifario`: `fk_tarifari_ref_56_moneda`

---

## Tabla: movimientos_historias

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | mvhnum | int | 4 | Número de Item |
| 3 | tmmcod | varchar | 2 | Código de tipo de movimiento |
| 4 | mvhfec | datetime | 8 | Fecha de movimiento |
| 5 | mvhsta | varchar | 2 | Estado del movimiento |
| 6 | pachis | varchar | 7 | Número de historia |
| 7 | sercod | varchar | 4 | Código de servicio |
| 8 | medcod | varchar | 4 | Código de médico |
| 9 | usecod | int | 4 | Código de usuario |
| 10 | codcon | varchar | 4 | Código de consultorio |
| 11 | codbar | varchar | 13 | Código de barras |
| 12 | mvhobs | varchar | 30 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| mvhfec | nonclustered located on PRIMARY | mvhfec |
| mvhnum | nonclustered located on PRIMARY | mvhnum |
| pk_movimientos_historias | nonclustered, unique, primary key located on PRIMARY | invnum |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| chk_mov_histori_estado | CHECK Table Level | ([mvhsta] = 'IA' or [mvhsta] = 'IN' or [mvhsta] = 'SA' or [mvhsta] = 'SN') |
| fk_mov_histori_medicos | FOREIGN KEY | medcod REFERENCES medicos (medcod) |
| fk_mov_histori_pacientes | FOREIGN KEY | pachis REFERENCES pacientes (pachis) |
| fk_mov_histori_servicios | FOREIGN KEY | sercod REFERENCES servicios (sercod) |
| fk_mov_histori_tipos | FOREIGN KEY | tmmcod REFERENCES .tipo_movimiento_historia (tmmcod) |
| fk_mov_histori_usuario | FOREIGN KEY | usecod REFERENCES usuarios (usecod) |

---

## Tabla: nivel_riesgo_social

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | nriecod | varchar | 2 | Código Nivel de Riesgo |
| 2 | nriedes | varchar | 25 | Descripción |
| 3 | nriemin | int | 4 | Puntaje Mínimo |
| 4 | nriemax | int | 4 | Puntaje Máximo |
| 5 | plnnum | varchar | 6 | Plan descuentos asociado |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_nriecod_nivel_riesgo_social | clustered, unique, primary key located on PRIMARY | nriecod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_plnnum_nivel_riesgo_social | FOREIGN KEY | plnnum REFERENCES .planes_imp (plnnum) |

---

## Tabla: ocupacion

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | ocucod | varchar | 2 | Código de ocupación |
| 2 | ocudes | varchar | 25 | Descripción de la ocupación |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ocupaci | clustered, unique, primary key located on PRIMARY | ocucod |

**Referencias desde:**
- `.pacientes`: `fk_paciente_ref_72_ocupacio`

---

## Tabla: origen_atencion

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | oricod | varchar | 2 | Código de origen de atención |
| 2 | orides | varchar | 25 | Descripción de origen de atención |
| 3 | oriprf | varchar | 1 | Estado de origen de atención |
| 4 | moncod | varchar | 2 | Código de moneda |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_origen_atenci | clustered, unique, primary key located on PRIMARY | oricod |

*No se ha definido ninguna restricción para este objeto.*

**Referencias desde:**
- `facturas`: `fk_fact_ref_origen_a`
- `planes_origen_historico`: `fk_origen_atenci`
- `.prefactura_detalle1`: `fk_prefactu_ref_53_o`
- `.prefacturas`: `fk_prefactu_ref_227_origen_a`
- `.planes_origen_historico`: `fk_origen_at`

---

## Tabla: pacientes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | pachis | varchar | 7 | Número de historia |
| 2 | pacpat | varchar | 20 | Apellido paterno del paciente |
| 3 | pacmat | varchar | 20 | Apellido materno del paciente |
| 4 | pacnam | varchar | 20 | Nombres del paciente |
| 5 | sexcod | varchar | 2 | Código de sexo |
| 6 | pacfen | datetime | 8 | Fecha de nacimiento |
| 7 | oricod | varchar | 2 | Código de origen |
| 8 | pacuci | datetime | 8 | Fecha de última cita |
| 9 | pacult | datetime | 8 | Fecha de último movimiento |
| 10 | eshcod | varchar | 2 | Código de estado de historia |
| 11 | prfnum | int | 4 | Número de prefactura (última) |
| 12 | invnum | int | 4 | Secuencia de origen |
| 13 | pacfin | datetime | 8 | Fecha de ingreso o filiación |
| 14 | usecod | int | 4 | Código de usuario |
| 15 | estcod | varchar | 2 | Estado de historia (versión anterior) |
| 16 | pacobs | varchar | 40 | Observaciones |
| 17 | pacdoc | varchar | 20 | Documento de identidad |
| 18 | tppcod | varchar | 2 | Tipo de paciente (PA, CS) |
| 19 | sercod | varchar | 4 | Código de servicio |
| 20 | medcod | varchar | 4 | Código de médico |
| 21 | pactet | varchar | 25 | Teléfono del centro de trabajo |
| 22 | pacdit | varchar | 45 | Dirección del trabajo |
| 23 | ocucod | varchar | 2 | Código de ocupación |
| 24 | ubicod | varchar | 6 | Código de ubicación para lugar de nacimiento |
| 25 | pactel | varchar | 25 | Teléfono del paciente |
| 26 | pacdir | varchar | 45 | Dirección del paciente |
| 27 | ecicod | varchar | 2 | Estado civil |
| 28 | paclun | varchar | 6 | Código de ubicación geográfica |
| 29 | procod | varchar | 2 | Código de procedencia |
| 30 | pacpic | varchar | 13 | Archivo (foto paciente) |
| 31 | prfold | int | 4 | Número de prefactura (anterior) |
| 32 | pacres | varchar | 40 | Código de asegurado |
| 33 | paceda | numeric | 5 | Edad del paciente |
| 34 | plnnum | varchar | 6 | Código de plan |
| 35 | parcod | varchar | 2 | Código de categoría de pago |
| 36 | ttmcod | varchar | 2 | Código de movimiento de historia (versión anterior) |
| 37 | tmmcod | varchar | 2 | Código de tipo de movimiento de historia |
| 38 | codbar | varchar | 13 | Código de barras |
| 39 | ubicot | varchar | 6 | Código de ubicación del centro de trabajo |
| 40 | pacsta | varchar | 1 | Estado de vigencia de la historia |
| 41 | invhos | int | 4 | Secuencia de hospitalización (última) |
| 42 | inveme | int | 4 | Secuencia de emergencia (última) |
| 43 | pactit | varchar | 30 | Nombre del titular |
| 44 | pacpmn | varchar | 40 | Apellidos y nombres del paciente |
| 45 | pacpic_p | varchar | 80 | Ubicación y nombre del archivo de fotografía |
| 46 | fecprc | datetime | 8 | Fecha Proceso |
| 47 | codtas | varchar | 2 | Tipo Asegurado |
| 48 | pacdas | varchar | 200 | Código Asegurado |
| 49 | pacref | varchar | 20 | Referencia |
| 50 | fecbaj | datetime | 8 | Fecha Baja |
| 51 | fecfingr | datetime | 8 | Fecha Control Inicio |
| 52 | fecfcesa | datetime | 8 | Fecha Control Fin |
| 53 | pacmail | varchar | 70 | E-mail |
| 54 | pacmail_t | varchar | 70 | E-mail trabajo |
| 55 | codrel | varchar | 2 | Código Religión |
| 56 | pacpri | varchar | 1 | Flag de Paciente Privado |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| in_pacpat_pacmat | nonclustered located on PRIMARY | pacpat, pacmat |
| ind_pacres | nonclustered located on PRIMARY | pacres |
| d_pactit_codtas | nonclustered located on PRIMARY | pactit, codtas |
| ind_plnnum_tppcod | nonclustered located on PRIMARY | plnnum, tppcod |
| index_pacuci | nonclustered located on PRIMARY | pacuci |
| pacpmn_ind | nonclustered located on PRIMARY | pacpmn |
| pk_pacientes | nonclustered, unique, primary key located on PRIMARY | pachis |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_paciente_ref_345_usuarios | FOREIGN KEY | usecod REFERENCES usuarios (usecod) |
| fk_paciente_ref_53_tipo_pac | FOREIGN KEY | tppcod REFERENCES tipo_paciente (tppcod) |
| fk_paciente_ref_56_sexo | FOREIGN KEY | sexcod REFERENCES sexo (sexcod) |
| fk_paciente_ref_72_ocupacio | FOREIGN KEY | ocucod REFERENCES ocupacion (ocucod) |
| fk_paciente_ref_96_ubigeo | FOREIGN KEY | ubicod REFERENCES .ubigeo (ubicod) |

**Referencias desde:**
- `.am_antecedentes`: `fk_am_antec_ref_466_paci`
- `.am_consulta`: `fk_am_consu_ref_509_paciente`
- `.am_notas_paciente`: `fk_pachis_am_notas_pac`
- `.citas_ea`: `fk_pachis_citas_ea_pacientes`
- `emergencia`: `fk_emergenc_ref_176_paciente`
- `hospitalizacion`: `fk_hospital_ref_653_paci`
- `.intervenciones_cabecera`: `fk_in_cab_ref_01`
- `.movimientos_historias`: `fk_mov_histori_pac`
- `.pacientes_exclusiones`: `fk_pac_exclusi_ref`
- `.pacientes_vigencia`: `fk_pac_restric_ref_pa`
- `.prefacturas`: `fk_prefactu_ref_175_paciente`
- `.seguridad_historias`: `fk_pachis_segu_histo`

---

## Tabla: pacientes_exclusiones

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | pachis | varchar | 7 | Número de historia |
| 2 | numitm | int | 4 | Número de item |
| 3 | diacod | varchar | 6 | Código de diagnóstico |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_pac_exclusi | clustered, unique, primary key located on PRIMARY | pachis, numitm |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_pac_exclusi_ref_pac | FOREIGN KEY | pachis REFERENCES pacientes (pachis) |

---

## Tabla: pacientes_riesgo_social_cab

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Secuencia de Evaluación |
| 2 | pachis | varchar | 7 | Historia |
| 3 | pacnam | varchar | 45 | Paciente |
| 4 | diacod | varchar | 6 | Diagnóstico |
| 5 | evafec | datetime | 8 | Fecha de Registro |
| 6 | oricod | varchar | 2 | Origen de atención |
| 7 | usecod | int | 4 | Usuario |
| 8 | evatot | numeric | 9 | Puntaje total de la evaluación |
| 9 | nriecod | varchar | 2 | Nivel de riesgo asignado |
| 10 | plnnum | varchar | 6 | Plan descuento asociado |
| 11 | evacom | text | 16 | Motivo de evaluación |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_invnum_pacientes_riesgo_cab | clustered, unique, primary key located on PRIMARY | invnum |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_plnnum_pacientes_riesgo_cab | FOREIGN KEY | plnnum REFERENCES .planes_imp (plnnum) |

**Referencia desde:**
- `pacientes_riesgo_social_det`: `fk_invnum_pa`

---

## Tabla: pacientes_riesgo_social_det

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Secuencia de evaluación |
| 2 | triecod | varchar | 3 | Tipo de riesgo social |
| 3 | triecod_2 | varchar | 3 | Sub-tipo de riesgo social |
| 4 | riecod | varchar | 2 | Código de riesgo |
| 5 | rieval | int | 4 | Puntaje asignado |
| 6 | riechk | varchar | 1 | Si fue marcado o no. |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_invnum_pacientes_riesgo_det | clustered, unique, primary key located on PRIMARY | invnum, triecod, triecod_2, riecod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_invnum_pacientes_riesgo_det | FOREIGN KEY | invnum REFERENCES pacientes_riesgo_social_cab (invnum) |

---

## Tabla: pacientes_vigencia

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | pachis | varchar | 7 | Número de historia |
| 2 | fecvig | datetime | 8 | Fecha de vigencia |
| 3 | estvig | varchar | 1 | Estado de vigencia |
| 4 | plnnum | varchar | 6 | Código de plan |
| 5 | pacpmn | varchar | 40 | Apellidos y nombres del paciente |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_pac_restric | clustered, unique, primary key located on PRIMARY | pachis |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| chk_pac_restric | CHECK Table Level | ([estvig] = 'N' or [estvig] = 'S') |
| fk_pac_restric_ref_pac | FOREIGN KEY | pachis REFERENCES pacientes (pachis) |

---

## Tabla: paquetes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codpaq | varchar | 6 | Código del paquete |
| 2 | despaq | varchar | 40 | Descripción del paquete |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_paquetes | clustered, unique, primary key located on PRIMARY | codpaq |

**Referencia desde:**
- `.paquetes_tabla`: `fk_paquet_tab_ref_102_paq`

---

## Tabla: paquetes_tabla

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codpaq | varchar | 6 | Código del paquete |
| 2 | numitm | int | 4 | Número de item |
| 3 | tippaq | varchar | 2 | Tipo de servicio |
| 4 | codprs | varchar | 8 | Código común de los servicios |
| 5 | despro | varchar | 70 | Descripción del servicio |
| 6 | nveces | int | 4 | Cantidad entera |
| 7 | qtymen | int | 4 | Cantidad menudeo |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_paquetes_tabla | clustered, unique, primary key located on PRIMARY | codpaq, numitm |

---

## Tabla: planes_eventos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | pinnum | varchar | 6 | Código de plan |
| 2 | numitm | int | 4 | Número de item |
| 3 | tarcod | varchar | 8 | Código de tarifa |
| 4 | tardes | varchar | 70 | Descripción de tarifa |
| 5 | numvez | int | 4 | Número de veces |
| 6 | montmax | numeric | 9 | Monto máximo |
| 7 | diavig | int | 4 | Días de vigencia |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_planes_eventos | nonclustered, unique, primary key located on PRIMARY | pinnum, numitm |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_planes_eventos_ref_planes | FOREIGN KEY | pinnum REFERENCES planes_seguro (pinnum) |
| fk_planes_eventos_ref_tarifa | FOREIGN KEY | tarcod REFERENCES tarifario (tarcod) |

---

## Tabla: planes_imp

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | pinnum | varchar | 6 | Código de Plan |
| 2 | pindes | varchar | 30 | Descripción del Plan |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_pinnum_planes_imp | clustered, unique, primary key located on PRIMARY | pinnum |

**Referencias desde:**
- `.nivel_riesgo_social`: `fk_plnnum_nivel_ries`
- `.pacientes_riesgo_social_cab`: `fk_pinnum_pa`
- `.planes_descuentos_imp`: `fk_plnnum_planes_d`

---

## Tabla: planes_origen

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | pinnum | varchar | 6 | Código de plan |
| 2 | oricod | varchar | 2 | Origen de atención |
| 3 | pincoa | numeric | 9 | Porcentaje de coaseguro |
| 4 | pinded | numeric | 9 | Monto de deducible |
| 5 | moncod | varchar | 2 | Código de moneda |

---

## Tabla: planes_origen_historico

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Secuencia |
| 2 | pinnum | varchar | 6 | Código Plan |
| 3 | oricod | varchar | 2 | Origen Atención |
| 4 | pincoa | numeric | 9 | Coaseguro % |
| 5 | pinded | numeric | 9 | Deducible |
| 6 | moncod | varchar | 2 | Moneda |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_planes_origen_historico | clustered, unique, primary key located on PRIMARY | invnum, pinnum, oricod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_origen_atencion_oricod | FOREIGN KEY | oricod REFERENCES .origen_atencion (oricod) |
| fk_planes_seguro_historico_inv | FOREIGN KEY | invnum, pinnum REFERENCES .planes_seguro_historico (invnum, pinnum) |

---

## Tabla: planes_restriccion

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | plnnum | varchar | 6 | Código de plan |
| 2 | numitm | int | 4 | Número de item |
| 3 | tpscod | varchar | 2 | Tipo de servicio |
| 4 | codprs | varchar | 8 | Código común de servicio |
| 5 | tpccod | varchar | 2 | Tipo de clasificación |
| 6 | porcot | numeric | 9 | Porcentaje de pago del paciente |
| 7 | desres | varchar | 50 | Descripción del servicio |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_planes_restriccion | clustered, unique, primary key located on PRIMARY | plnnum, numitm |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_planes_r_ref_101_planes_s | FOREIGN KEY | pinnum REFERENCES .planes_seguro (pinnum) |

---

## Tabla: planes_seguro

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | pinnum | varchar | 6 | Código de plan |
| 2 | concod | varchar | 4 | Código de contratante |
| 3 | segcod | varchar | 4 | Código de compañía de seguros |
| 4 | pindes | varchar | 40 | Descripción del plan |
| 5 | pincnt | varchar | 15 | Número de póliza o contrato |
| 6 | plnfin | datetime | 8 | Fecha de inicio de vigencia del plan |
| 7 | plnffi | datetime | 8 | Fecha de fin de vigencia del plan |
| 8 | plncti | numeric | 9 | Cobertura máxima del plan |
| 9 | plnctm | numeric | 9 | Monto de cobertura mínima |
| 10 | moncod | varchar | 2 | Código de moneda |
| 11 | pindla | numeric | 9 | Porcentaje de descuento en laboratorio |
| 12 | plndea | numeric | 9 | Porcentaje de descuento en exámenes |
| 13 | pindfa | numeric | 9 | Porcentaje de descuento en farmacia |
| 14 | plnobs | text | 16 | Observaciones |
| 15 | plnamp | numeric | 9 | Monto de ampliación |
| 16 | plnfac | datetime | 8 | Fecha de actualización |
| 17 | plnsta | char | 1 | Estado de vigencia |
| 18 | pincde | char | 1 | Estado de Aceptación de coaseguros y deducibles |
| 19 | pindgr | int | 4 | Número de Días de Gracia |
| 20 | plnicc | varchar | 2 | Flag modifica prefactura en Citas |
| 21 | plnice | varchar | 2 | Flag modifica prefactura en Emergencia |
| 22 | plndsb | int | 4 | Número de días de duración de solicitud |
| 23 | plneigv | varchar | 1 | Flag exclusión IGV pago a terceros |
| 24 | plncpm | varchar | 1 | Check Pago Paciente Mes |
| 25 | plncpm_m | numeric | 9 | Monto Pago Paciente Mes |
| 26 | pindseg | varchar | 1 | Check Descuento para Aseguradora |
| 27 | pindseg_p | numeric | 5 | Porcentaje descuento para Aseguradora |
| 28 | pincpr | varchar | 1 | Check coaseguro procedimientos |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| ind_pincnt | nonclustered located on PRIMARY | pincnt |
| pk_planes_seguro | nonclustered, unique, primary key located on PRIMARY | pinnum |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| chk_plnicc | CHECK Table Level | ([plnicc] = 'CT' or [plnicc] = 'CD' or [plnicc] = 'NO') |
| chk_plnice | CHECK Table Level | ([plnice] = 'CT' or [plnice] = 'CD' or [plnice] = 'NO') |

**Referencias desde:**
- `planes_categoria`: `fk_planes_c_ref_98_plan`
- `planes_eventos`: `fk_planes_eventos_ref_pla`
- `planes_restriccion`: `fk_planes_r_ref_101_p`

---

## Tabla: planes_seguro_historico

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Secuencia |
| 2 | pinnum | varchar | 6 | Código Plan |
| 3 | concod | varchar | 4 | Código Contratante |
| 4 | segcod | varchar | 4 | Código Aseguradora |
| 5 | pindes | varchar | 40 | Descripción Plan |
| 6 | plncnt | varchar | 15 | No. Contrato |
| 7 | plnfin | datetime | 8 | Fecha Inicio Vigencia |
| 8 | plnffi | datetime | 8 | Fecha Fin Vigencia |
| 9 | plncti | numeric | 9 | Monto Mínimo |
| 10 | plnctm | numeric | 9 | Monto Máximo |
| 11 | moncod | varchar | 2 | Moneda |
| 12 | pindla | numeric | 9 | Descuento LA % |
| 13 | pindea | numeric | 9 | Descuento EA % |
| 14 | plndfa | numeric | 9 | Descuento FA % |
| 15 | plnobs | text | 16 | Observaciones |
| 16 | plnamp | numeric | 9 | Monto Ampliación |
| 17 | pinfac | datetime | 8 | Fecha Actualización |
| 18 | plnsta | char | 1 | Estado |
| 19 | pincde | char | 1 | Modifica Coaseguro y Deducible |
| 20 | plndgr | int | 4 | Días Gracia |
| 21 | plnicc | varchar | 2 | Incluye Coaseguro en Consulta |
| 22 | plnice | varchar | 2 | Incluye Coaseguro en Emergencia |
| 23 | plndsb | int | 4 | Días Solicitud Beneficios |
| 24 | plneigv | varchar | 1 | Cobranza a Terceros |
| 25 | usemod | int | 4 | Usuario Modificación |
| 26 | fecmod | datetime | 8 | Fecha Modificación |
| 27 | plncpm | varchar | 1 | Pago Paciente Mes |
| 28 | plncpm_m | numeric | 9 | Monto Pago Paciente Mes |
| 29 | pindseg | varchar | 1 | Check Descuento para Aseguradora |
| 30 | pindseg_p | numeric | 5 | Porcentaje descuento para Aseguradora |
| 31 | plncpr | varchar | 1 | Check coaseguro procedimientos |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_planes_seguro_historico | nonclustered, unique, primary key located on PRIMARY | invnum, pinnum |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| chk_his_plnicc | CHECK on column plnicc | ([plnicc] = 'CT' or [plnicc] = 'CD' or [plnicc] = 'NO') |
| chk_his_plnice | CHECK on column plnice | ([plnice] = 'CT' or [plnice] = 'CD' or [plnice] = 'NO') |
| fk_contratantes_concod | FOREIGN KEY | concod REFERENCES .contratantes (concod) |

**Referencias desde:**
- `planes_categoria_historico`: `fk_planes_seg`
- `.planes_origen_historico`: `fk_planes_seguro`

---

## Tabla: plantillas_informes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | siscod | int | 4 | Código Establecimiento |
| 2 | infope | text | 16 | Plantilla Informe Operatorio |
| 3 | infane | text | 16 | Plantilla Informe Anestesia |
| 4 | infepi | text | 16 | Plantilla Epicrisis Hospitalización |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_plantillas_informes | nonclustered, unique, primary key located on PRIMARY | siscod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_plantillas_informes_siscod | FOREIGN KEY | siscod REFERENCES sistema (siscod) |

---

## Tabla: prefactura_detalle1

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | prfnum | int | 4 | Número de prefactura |
| 2 | prfitm | int | 4 | Número de item |
| 3 | oricod | varchar | 2 | Código de origen de atención |
| 4 | tarcod | varchar | 8 | Código de tarifa |
| 5 | tardes | varchar | 70 | Descripción de la tarifa |
| 6 | prfint | varchar | 2 | Tipo de interviniente (PA, CS) |
| 7 | prffmo | datetime | 8 | Fecha de movimiento |
| 8 | prfnve | numeric | 5 | Número de veces |
| 9 | prftar | numeric | 9 | Monto total del servicio |
| 10 | prfpar | numeric | 9 | Monto parcial del servicio |
| 11 | prfres | varchar | 1 | Habilitado para Caja (S/N) |
| 12 | parcod | varchar | 2 | Código de categoría de pago |
| 13 | codstd | varchar | 2 | Código de estado del movimiento |
| 14 | invnum | int | 4 | Número de secuencia de origen |
| 15 | tarcos | numeric | 9 | Costo del servicio |
| 16 | totser | numeric | 9 | Monto total del servicio (impuestos) |
| 17 | prfigv | numeric | 9 | Monto de impuestos |
| 18 | prfigvp | numeric | 9 | Porcentaje de impuestos |
| 19 | prfdto | numeric | 9 | Descuento del servicio |
| 20 | prfppac | numeric | 9 | Pago del paciente |
| 21 | prfpseg | numeric | 9 | Pago de la aseguradora |
| 22 | prfcoa | numeric | 9 | Porcentaje de coaseguro |
| 23 | prfgnc | numeric | 9 | Gastos no cubiertos |
| 24 | prfded | numeric | 9 | Monto del deducible |
| 25 | tarpcta | char | 1 | Flag pago a cuenta |
| 26 | tarint | varchar | 1 | Tiene o no Intervinientes |
| 27 | tarcos_d | numeric | 9 | Costo en dólares |
| 28 | totpcta | numeric | 9 | Total Pago a Cuenta |
| 29 | tarcos_e | numeric | 9 | Costo Establecimiento |
| 30 | igvpac | numeric | 9 | Impuestos Paciente |
| 31 | igvseg | numeric | 9 | Impuesto Aseguradora |
| 32 | prftab | numeric | 9 | Tarifa referencial |
| 33 | dtoadi_pac | numeric | 9 | Dscto adicional para paciente |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| ind_prffmo_prfnum_tarcod_codst | nonclustered located on PRIMARY | prffmo, prfnum, tarcod, codstd |
| pk_prefactura_detalle1 | nonclustered, unique, primary key located on PRIMARY | prfnum, prfitm |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| ckc_prefactura_detalle1 | CHECK Table Level | ([prfint] = 'CS' or [prfint] = 'PA') |
| fk_prefactu_ref_221_tarifari | FOREIGN KEY | tarcod REFERENCES tarifario (tarcod) |
| fk_prefactu_ref_52_prefactu | FOREIGN KEY | prfnum REFERENCES prefacturas (prfnum) |
| fk_prefactu_ref_53_origen_a | FOREIGN KEY | oricod REFERENCES .origen_atencion (oricod) |

**Referencia desde:**
- `.prefactura_detalle2`: `fk_prf_deta2_ref_prf`

---

## Tabla: prefactura_detalle2

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | prfnum | int | 4 | Número de prefactura |
| 2 | prfitm | int | 4 | Número de item (referencia de origen) |
| 3 | prfnli | int | 4 | Número de item (referencia de origen) |
| 4 | medcod | varchar | 4 | Código de médico |
| 5 | montot | numeric | 9 | Monto parcial del servicio (con impuesto) |
| 6 | staliq | varchar | 1 | Estado de liquidación (pago de honorarios) |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_prefactura_detalle2 | clustered, unique, primary key located on PRIMARY | prfnum, prfitm, prfnli |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_prf_deta2_ref_prf_deta1 | FOREIGN KEY | prfnum, prfitm REFERENCES prefactura_detalle1 (prfnum, prfitm) |

---

## Tabla: prefacturas

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | prfnum | int | 4 | Número de prefactura |
| 2 | pachis | varchar | 7 | Número de historia |
| 3 | pacnam | varchar | 40 | Apellidos y Nombres del paciente |
| 4 | prftot | numeric | 9 | Monto total de la prefactura (sin impuestos) |
| 5 | pinnum | varchar | 6 | Código de plan |
| 6 | parcod | varchar | 2 | Código de categoría de pago |
| 7 | oricod | varchar | 2 | Código de origen de atención |
| 8 | tdofac | varchar | 2 | Tipo de documento de cierre de prefactura |
| 9 | prfigv | numeric | 9 | Impuestos |
| 10 | prffin | datetime | 8 | Fecha de fin de vigencia |
| 11 | prfini | datetime | 8 | Fecha de inicio de vigencia |
| 12 | invnum | int | 4 | Número de secuencia de origen de la prefactura |
| 13 | usecod | int | 4 | Código de usuario |
| 14 | prfnli | int | 4 | Número de líneas de la prefactura |
| 15 | prfcar | varchar | 10 | Número de la carta de garantía (solicitud) |
| 16 | prfsta | varchar | 1 | Estado de la prefactura |
| 17 | tppcod | varchar | 2 | Tipo de paciente |
| 18 | pinded | numeric | 9 | Monto del deducible |
| 19 | pincoa | numeric | 9 | Porcentaje de coaseguro |
| 20 | medcod | varchar | 4 | Código de médico (referencia) |
| 21 | prfnet | numeric | 9 | Monto total de la prefactura (con impuestos) |
| 22 | cobmax | numeric | 9 | Cobertura máxima |
| 23 | pacdir | varchar | 40 | Dirección del paciente |
| 24 | pactel | varchar | 25 | Teléfono del paciente |
| 25 | invnum_r | int | 4 | Documento de facturación |
| 26 | pactit | varchar | 40 | Nombre del titular |
| 27 | tipcli | varchar | 2 | Tipo de cliente (PA, CL) |
| 28 | invnum_pre | int | 4 | Número de presupuesto |
| 29 | invnum_c | int | 4 | Número de prefactura de canje |
| 30 | plneigv | varchar | 1 | Flag exclusión IGV pago a terceros |
| 31 | fecanu | datetime | 8 | Fecha de anulación |
| 32 | useanu | int | 4 | Código de usuario que anuló el documento |
| 33 | concod | varchar | 4 | Código de cía contratante |
| 34 | segcod | varchar | 4 | Código de cía de seguros |
| 35 | siscod | int | 4 | Código Establecimiento |
| 36 | fecamp | datetime | 8 | Fecha Ampliación Cobertura |
| 37 | useamp | int | 4 | Usuario Ampliación Cobertura |
| 38 | totcos_e | numeric | 9 | Total Costo Establecimiento |
| 39 | totigv_p | numeric | 9 | Total Impuesto Paciente |
| 40 | totigv_c | numeric | 9 | Total Impuesto Aseguradora |
| 41 | plncpm | varchar | 1 | Flag de Pago Paciente Mes (Al generar factura) |
| 42 | pindseg | varchar | 1 | Check Descuento para Aseguradora |
| 43 | prfdseg_p | numeric | 9 | Porcentaje descuento para Aseguradora |
| 44 | prfdseg | numeric | 9 | Monto descuento para Aseguradora |
| 45 | emepro | varchar | 1 | Flag de si se continúa EMERGENCIA |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| in_prfnum | nonclustered located on PRIMARY | pacnam |
| pk_prefacturas | nonclustered, unique, primary key located on PRIMARY | prfnum |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| chk_prefacturas_siscod | CHECK on column siscod | (((not([siscod] is null)))) |
| fk_prefactu_ref_175_paciente | FOREIGN KEY | pachis REFERENCES pacientes (pachis) |
| fk_prefactu_ref_178_usuarios | FOREIGN KEY | usecod REFERENCES usuarios (usecod) |
| fk_prefactu_ref_227_origen_a | FOREIGN KEY | oricod REFERENCES origen_atencion (oricod) |
| fk_prefactu_ref_228_medicos | FOREIGN KEY | medcod REFERENCES .medicos (medcod) |
| fk_prefacturas_siscod | FOREIGN KEY | siscod REFERENCES sistema (siscod) |

**Referencias desde:**
- `facturas_detalle`: `fk_factu_ref_02_factu`
- `liquidacion_detalle`: `fk_liquidac_ref_52_p`
- `prefactura_detalle1`: `fk_prefactu_ref_52_p`

---

## Tabla: prendas

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | prencod | varchar | 2 | Código de prenda |
| 2 | prendes | varchar | 30 | Descripción de prenda |
| 3 | prenpes | numeric | 5 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_prencod | nonclustered, unique, primary key located on PRIMARY | prencod |

---

## Tabla: presupuesto_prefactura

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | parcod | varchar | 2 | Código de categoría de pago |
| 3 | segraz | varchar | 70 | Descripción de la cía de seguros |
| 4 | pacnam | varchar | 40 | Nombre del paciente |
| 5 | pachis | varchar | 7 | Número de historia |
| 6 | segcod | varchar | 4 | Código de cía de seguros |
| 7 | fecpre | datetime | 8 | Fecha de presupuesto |
| 8 | totpre | numeric | 9 | Total presupuesto (sin impuestos) |
| 9 | igvpre | numeric | 9 | Impuestos |
| 10 | netpre | numeric | 9 | Total presupuesto (con impuestos) |
| 11 | cabpre | text | 16 | Texto de la cabecera de presupuestos |
| 12 | piepre | text | 16 | Texto de pie de presupuestos |
| 13 | tppcod | varchar | 2 | Tipo Paciente |
| 14 | pinnum | varchar | 6 | Código Plan |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pachis | nonclustered located on PRIMARY | pachis |
| pk_presupuesto | clustered, unique, primary key located on PRIMARY | invnum |

**Referencia desde:**
- `.presupuesto_prefactura_tabla`: `fk_presup_t`

---

## Tabla: presupuesto_prefactura_tabla

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | numitm | int | 4 | Número de item |
| 3 | tippro | varchar | 2 | Origen del servicio |
| 4 | codprs | varchar | 8 | Código común del servicio |
| 5 | despro | varchar | 70 | Descripción del servicio |
| 6 | nveces | int | 4 | Cantidad entera |
| 7 | qtymen | int | 4 | Cantidad menudeo |
| 8 | precpro | numeric | 9 | Precio del servicio |
| 9 | prepar | numeric | 9 | Total parcial del servicio |
| 10 | dscpro | numeric | 9 | Descuento del servicio |
| 11 | igvpro | numeric | 9 | Impuestos |
| 12 | stkfra | int | 4 | Fracción del servicio |
| 13 | portar | numeric | 9 | Porcentaje de la tarifa a cobrar |
| 14 | totsis | numeric | 9 | Total calculado por el sistema |
| 15 | totuse | numeric | 9 | Total por el usuario |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_presupuesto_tabla | nonclustered, unique, primary key located on PRIMARY | invnum, numitm |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_presup_tab_ref_103_presup | FOREIGN KEY | invnum REFERENCES presupuesto_prefactura (invnum) |

---

## Tabla: procedencia_filiacion

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | procod | varchar | 2 | Código de procedencia en filiación |
| 2 | prodes | varchar | 25 | Descripción de procedencia de filiación |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_procedencia_filiaci | clustered, unique, primary key located on PRIMARY | procod |

---

## Tabla: procedencia_hospitalizacion

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | hprcod | varchar | 2 | Código de procedencia hospitalización |
| 2 | hprdes | varchar | 20 | Descripción de procedencia hospitalización |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_procedencia_hospitalizaci | clustered, unique, primary key located on PRIMARY | hprcod |

**Referencia desde:**
- `hospitalizacion`: `fk_hospital_ref_85_proce`

---

## Tabla: procedimientos_cabecera

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | prodat | datetime | 8 | Fecha de movimiento |
| 3 | pachis | varchar | 7 | Número de historia |
| 4 | pacnam | varchar | 30 | Nombre del paciente o cliente |
| 5 | prfnum | int | 4 | Número de prefactura |
| 6 | tipcli | varchar | 2 | Tipo de cliente (PA, CL) |
| 7 | tppcod | varchar | 2 | Tipo de paciente (PA, CS) |
| 8 | usecod | int | 4 | Código de usuario |
| 9 | pinnum | varchar | 6 | Código de plan |
| 10 | parcod | varchar | 2 | Código de categoría de pago |
| 11 | proded | numeric | 9 | Monto del deducible |
| 12 | procoa | numeric | 9 | Porcentaje de coaseguro |
| 13 | protot | numeric | 9 | Monto total del servicio (sin impuestos) |
| 14 | pronet | numeric | 9 | Monto total del servicio (con impuestos) |
| 15 | proigv | numeric | 9 | Impuestos |
| 16 | prodes | numeric | 9 | Descuentos |
| 17 | obscit | varchar | 30 | Observaciones |
| 18 | pacdir | varchar | 45 | Dirección del paciente o cliente |
| 19 | pactel | varchar | 25 | Teléfono del paciente o cliente |
| 20 | codstd | varchar | 2 | Código de estado del documento |
| 21 | procar | varchar | 10 | Número de la Carta de Garantía |
| 22 | profvc | datetime | 8 | Fecha de vencimiento de la carta |
| 23 | fecanu | datetime | 8 | Fecha de anulación |
| 24 | useanu | int | 4 | Código de usuario que anuló el documento |
| 25 | concod | varchar | 4 | Código de cía contratante |
| 26 | segcod | varchar | 4 | Código de cía de seguros |
| 27 | siscod | int | 4 | Código Establecimiento |
| 28 | totcos | numeric | 9 | Total Costo |
| 29 | totcos_d | numeric | 9 | Total Costo Dólares |
| 30 | totcos_e | numeric | 9 | Total Costo Establecimiento |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_procedimi_cab_001 | nonclustered, unique, primary key located on PRIMARY | invnum |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_proce_ref_001_tipo_cli | FOREIGN KEY | tipcli REFERENCES .tipo_cliente (tipcli) |
| fk_proce_ref_001_usuarios | FOREIGN KEY | usecod REFERENCES usuarios (usecod) |
| fk_procedimientos_cabe_siscod | FOREIGN KEY | siscod REFERENCES sistema (siscod) |

**Referencia desde:**
- `procedimientos_detalle`: `fk_proce_ref_001_`

---

## Tabla: procedimientos_detalle

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Número de secuencia |
| 2 | numitm | int | 4 | Número de item |
| 3 | tarcod | varchar | 8 | Código de tarifa |
| 4 | tardes | varchar | 70 | Descripción de la tarifa |
| 5 | procan | numeric | 5 | Cantidad |
| 6 | propar | numeric | 9 | Monto parcial |
| 7 | protot | numeric | 9 | Monto total |
| 8 | proigv | numeric | 9 | Porcentaje de impuestos |
| 9 | prodes | numeric | 9 | Descuento del servicio |
| 10 | tarint | varchar | 1 | Considera interviniente (S, N) |
| 11 | medcod | varchar | 4 | Código de médico |
| 12 | dtopro | numeric | 9 | Descuento del servicio |
| 13 | propri | numeric | 9 | Precio del servicio |
| 14 | procoa | numeric | 9 | % Coaseguro |
| 15 | tarcos | numeric | 9 | Costo |
| 16 | tarcos_d | numeric | 9 | Costo en dólares |
| 17 | tarcos_e | numeric | 9 | Costo Establecimiento |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_procedimi_deta_001 | nonclustered, unique, primary key located on PRIMARY | invnum, numitm |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_proce_ref_001_proc_cabe | FOREIGN KEY | invnum REFERENCES procedimientos_cabecera (invnum) |
| fk_proce_ref_001_tarifario | FOREIGN KEY | tarcod REFERENCES tarifario (tarcod) |

---

## Tabla: programas

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | chrmod | varchar | 1 | Asociación del módulo (letra) |
| 2 | codprg | varchar | 3 | Código de programa en el menú |
| 3 | tipprg | varchar | 1 | Tipo de programa |
| 4 | namprg | varchar | 40 | Nombre del programa |
| 5 | hlpprg | varchar | 50 | Nombre del programa para la Ayuda |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_programas | clustered, unique, primary key located on PRIMARY | chrmod, codprg |

---

## Tabla: rangos_categoria_farmacia

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Secuencia |
| 2 | ranini | numeric | 9 | Inicio de rango |
| 3 | ranfin | numeric | 9 | Fin de rango |
| 4 | utiadic | numeric | 9 | Utilidad adicional |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_rangos_categoria_fa_invnum | clustered, unique, primary key located on PRIMARY | invnum |

---

## Tabla: religiones

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codrel | varchar | 2 | Código Religión |
| 2 | desrel | varchar | 25 | Descripción |
| 3 | obsrel | varchar | 40 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_religiones | clustered, unique, primary key located on PRIMARY | codrel |

---

## Tabla: reportes_cliente

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | rpccod | int | 4 | Código Reporte |
| 2 | rpcdesl | varchar | 50 | Descripción Larga |
| 3 | rpcndw | varchar | 40 | Nombre DataWindow |
| 4 | rpcdes | varchar | 50 | Descripción corta |
| 5 | rpcmod | varchar | 2 | Módulo |
| 6 | rpctip | varchar | 1 | Tipo Reporte Script |
| 7 | rpcest | varchar | 1 | Estilo Grid - Tabular |
| 8 | rpcprn | varchar | 100 | Cliente Asociado |
| 9 | rpcfec | datetime | 8 | Fecha |
| 10 | rpcsol | varchar | 40 | Referencia 1 |
| 11 | rpcacc | text | 16 | Accesos restringido |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_rpc_reportes_clientes | clustered, unique, primary key located on PRIMARY | rpccod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| ckc_reportes_clientes_rpcest | CHECK Table Level | ([rpcest] = 'S' or [rpcest] = 'N') |

---

## Tabla: reportes_usuario

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | rpucod | int | 4 | Código del reporte |
| 2 | rpudes | varchar | 60 | Descripción del reporte |
| 3 | rputig | varchar | 40 | Título general del reporte |
| 4 | rputim | varchar | 40 | Título del menú del reporte |
| 5 | rpumod | varchar | 2 | Nombre del Módulo (Intervinientes, Pacientes...) |
| 6 | rpuscr | text | 16 | Código para generar Reporte del usuario |
| 7 | rputip | varchar | 1 | Tipo de Reporte (Reporte, Script) |
| 8 | rpusty | varchar | 1 | Estilo |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| PK_rpucod | clustered, unique, primary key located on PRIMARY | rpucod |

**Referencia desde:**
- `reportes_usuario_argumentos`: `fk_rep_usu_a`

---

## Tabla: reportes_usuario_argumentos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | rpucod | int | 4 | Código Reporte |
| 2 | numitm | int | 4 | No. Item |
| 3 | desarg | varchar | 40 | Descripción Parámetro |
| 4 | nargdw | varchar | 40 | Nombre Argumento |
| 5 | targdw | varchar | 25 | Tipo Argumento |
| 6 | maskarg | varchar | 40 | Máscara |
| 7 | tdatarg | varchar | 1 | Tipo Dato |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_rep_usu_arg | clustered, unique, primary key located on PRIMARY | rpucod, numitm |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_rep_usu_arg_rpucod | FOREIGN KEY | rpucod REFERENCES reportes_usuario (rpucod) |

---

## Tabla: riesgo_social

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | triecod | varchar | 3 | Tipo de Riesgo Social |
| 2 | triecod_2 | varchar | 2 | Sub-tipo de Riesgo |
| 3 | triedes_2 | varchar | 20 | Descripción del sub-tipo |
| 4 | riecod | varchar | 2 | Código del riesgo |
| 5 | riedes | varchar | 30 | Descripción del riesgo |
| 6 | rieval | int | 4 | Puntaje |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_triecod_riecod_riesgo_socia | clustered, unique, primary key located on PRIMARY | triecod, triecod_2, riecod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_triecod_tipo_riesgo_social | FOREIGN KEY | triecod REFERENCES .tipo_riesgo_social (triecod) |

---

## Tabla: secuencias

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | tsecod | varchar | 2 | Código de secuencia |
| 2 | tsedes | int | 4 | Correlativo de secuencia |
| 3 | tsedef | varchar | 30 | Descripción de la secuencia |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_secuencias | clustered, unique, primary key located on PRIMARY | tsecod |

---

## Tabla: seguridad_historias

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | pachis | varchar | 7 | No. Historia |
| 2 | useaut | int | 4 | Usuario Autorizado |
| 3 | fecini | datetime | 8 | Fecha Inicio Autorización |
| 4 | fecfin | datetime | 8 | Fecha Fin Autorización |
| 5 | usecod | int | 4 | Código Usuario |
| 6 | usenam | varchar | 30 | Nombre Usuario |
| 7 | fecaut | datetime | 8 | Fecha Autorización |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_pachis_useaut_segu_histo | clustered, unique, primary key located on PRIMARY | pachis, useaut |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_pachis_segu_histo | FOREIGN KEY | pachis REFERENCES pacientes (pachis) |
| fk_useaut_segu_histo | FOREIGN KEY | useaut REFERENCES usuarios (usecod) |

---

## Tabla: seguros

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | segcod | varchar | 4 | Código de la cía de seguro |
| 2 | segraz | varchar | 70 | Razón social de la cía |
| 3 | segdir | varchar | 90 | Dirección de la cía de seguro |
| 4 | ubicod | varchar | 6 | Código de ubicación |
| 5 | segtel | varchar | 25 | Teléfono de la cía de seguro |
| 6 | segruc | varchar | 8 | Número de RUC de la cía de seguro |
| 7 | segres | varchar | 30 | Responsable de la cía seguro |
| 8 | segobs | varchar | 40 | Observaciones |
| 9 | ruc | varchar | 20 | Nuevo RUC |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_seguros | clustered, unique, primary key located on PRIMARY | segcod |

---

## Tabla: servicios

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | sercod | varchar | 4 | Código del servicio |
| 2 | serdes | varchar | 40 | Descripción del servicio |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_servicios | clustered, unique, primary key located on PRIMARY | sercod |

*No se ha definido ninguna restricción para este objeto.*

**Referencias desde:**
- `.am_consulta`: `fk_am_consu_ref_684_servicio`
- `.incapacidades`: `fk_sercod_incapacidades_se`
- `.medicos`: `fk_medicos_ref_31_servicio`
- `.movimientos_historias`: `fk_mov_histori_ser`
- `servicios_usuarios`: `FK_SERVICIO_USUAR_RE`

---

## Tabla: servicios_camas

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | sercod | varchar | 4 | Código Servicio |
| 2 | numcam | int | 4 | No. Camas |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_SER_CAMAS | clustered, unique, primary key located on PRIMARY | sercod |

---

## Tabla: servicios_usuarios

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | sercod | varchar | 4 | Código del servicio |
| 2 | usecod | int | 4 | Código de usuario |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_servcios_usuarios | clustered, unique, primary key located on PRIMARY | sercod, usecod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| FK_SERVICIO_USUAR_REF_SERVICI | FOREIGN KEY | sercod REFERENCES servicios (sercod) |
| FK_SERVICIO_USUAR_REF_USUARIO | FOREIGN KEY | usecod REFERENCES usuarios (usecod) |

---

## Tabla: sexo

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | sexcod | varchar | 2 | Código del sexo |
| 2 | sexdes | varchar | 25 | Descripción del sexo |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_sexo | clustered, unique, primary key located on PRIMARY | sexcod |

**Referencias desde:**
- `emergencia`: `fk_emergenc_ref_208_sexo`
- `pacientes`: `fk_paciente_ref_56_sexo`

---

## Tabla: sistema

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | siscod | int | 4 | Código del sistema |
| 2 | sistit | varchar | 70 | Título del sistema |
| 3 | sisent | varchar | 70 | Entidad asociada al sistema |
| 4 | sisdir | varchar | 70 | Dirección de la entidad |
| 5 | sisruc | varchar | 8 | No de RUC de la Entidad |
| 6 | sisigv | numeric | 9 | Impuestos |
| 7 | sisenc | varchar | 40 | Encargado del sistema |
| 8 | sisusr | int | 4 | Usuarios del sistema |
| 9 | siscat | varchar | 2 | Categoría de pago por defecto |
| 10 | sistel | varchar | 25 | Teléfono de la entidad |
| 11 | sisnpac | varchar | 1 | Flag de codificación automática de paciente |
| 12 | sisprt | varchar | 100 | Impresora por defecto para el sistema |
| 13 | sismproc | varchar | 1 | Flag de aceptación de planes en procedimiento |
| 14 | sisnmed | char | 1 | Flag de codificación automática de médicos |
| 15 | sistur | char | 1 | Flag de verificación de cruce de turnos |
| 16 | sisttacit | varchar | 60 | Selección múltiple Tipo Tarifa |
| 17 | sisdcaj | varchar | 2 | Flag incluir descuento en Caja |
| 18 | sisdmmx | numeric | 9 | Monto máximo de descuento en caja |
| 19 | sisafar | varchar | 1 | Restringe acceso por vigencia de solicitud |
| 20 | sisalab | varchar | 1 | Restringe acceso por vigencia de solicitud |
| 21 | sisaexa | varchar | 1 | Restringe acceso por vigencia de solicitud |
| 22 | sisapro | varchar | 1 | Restringe acceso por vigencia de solicitud |
| 23 | sistede | char | 1 | Porcentaje de Descuentos para Externos |
| 24 | sisfpede | numeric | 9 | Porcentaje |
| 25 | sisrsu | char | 1 | Restricción de Servicios por Usuario (S/N) |
| 26 | sisnlfar | int | 4 | Número de líneas para la venta Farmacia |
| 27 | sisprfac | char | 1 | Tipo Impresión Facturación Normal |
| 28 | pacpal | int | 4 | Dimensiones: Alto Foto |
| 29 | pacpan | int | 4 | Dimensiones: Ancho Foto |
| 30 | sisnsfar | varchar | 10 | Nombre Solicitante FARMACIA |
| 31 | sisacfar | varchar | 10 | Nombre Autorización FARMACIA |
| 32 | sisccfar | varchar | 10 | Centro Costo FARMACIA |
| 33 | sisver | varchar | 10 | Versión |
| 34 | sisfot | varchar | 1 | Directorio por Paciente / Único |
| 35 | sisfir_m | varchar | 80 | Ubicación firma Médico |
| 36 | sisfot_p | varchar | 80 | Ubicación Foto Paciente |
| 37 | sisfir_t | varchar | 1 | Formato de Firma BMP |
| 38 | ruc | varchar | 20 | Código Contribuyente |
| 39 | sisrescit | varchar | 1 | Acepta Restricciones Plan CITAS |
| 40 | sisdrecit | int | 4 | No. Días Restricción Por Paciente |
| 41 | sisdrmcit | varchar | 1 | Tipo de Mensaje (Advertencia, Confirmación...) |
| 42 | sisnmicit | int | 4 | No. de Integrantes Familia Permitidos |
| 43 | sisndicit | int | 4 | No. de Días por familia |
| 44 | sisnmmcit | varchar | 1 | Tipo Mensaje (Advertencia, Confirmación...) |
| 45 | sisdpocit | int | 4 | No. Días permitidos Otorgamiento Citas |
| 46 | sisbcit | varchar | 1 | Bloquea Atención Pacientes sin Historia |
| 47 | sismedcit | varchar | 1 | Acepta No. Citas por Médico |
| 48 | sismedcitx | varchar | 1 | Tipo Mensaje No. Citas por Médico |
| 49 | sispath_update | varchar | 80 | Ruta de archivo de actualización |
| 50 | sisgproc | varchar | 1 | Genera orden procedimientos en ACTO MÉDICO |
| 51 | sisegrcajem | varchar | 1 | Pide Egreso EMERGENCIA en Caja |
| 52 | sisegrcajho | varchar | 1 | Pide Egreso HOSPITALIZACIÓN en Caja |
| 53 | sisgamproc | varchar | 1 | Genera Orden Procedimientos en ACTO MÉDICO |
| 54 | sismpr | numeric | 9 | Porcentaje Mora (COTIZACIONES) |
| 55 | sismul | numeric | 9 | Porcentaje Multa (COTIZACIONES) |
| 56 | sisfmi | numeric | 9 | Factor de Interés (COTIZACIONES) |
| 57 | siseti | varchar | 1 | Visualiza etiqueta en ACTO MÉDICO |
| 58 | sisasm | varchar | 1 | Alerta Stock Mínimo |
| 59 | sisrep | varchar | 1 | Alerta Nivel Reposición |
| 60 | sispre | varchar | 1 | Muestra Precios Fármacos en Acto Médico |
| 61 | sisvlo | varchar | 1 | Acepta Venta por Lotes FARMACIA |
| 62 | sismlo | varchar | 1 | Lote Automático o Manual |
| 63 | siscem | varchar | 1 | Control Entrega de Medicamentos FARMACIA |
| 64 | sisshc | varchar | 1 | Considera Seguridad en Historias Clínicas |
| 65 | sisorh | varchar | 1 | Ordenamiento en H.C. Número o Fecha |
| 66 | sisbrel | varchar | 1 | Bloquea resultados aprobados en LABORATORIO |
| 67 | sisbree | varchar | 1 | Bloquea resultados aprobados en EXÁMENES |
| 68 | sisvre | varchar | 1 | Verifica Restricciones por Eventos |
| 69 | siscih | varchar | 1 | Considera Impuestos en Honorarios |
| 70 | siscll_hc | varchar | 1 | Considera hora de llegada en Historia Clínica |
| 71 | sisvad_ce | varchar | 1 | Considera Verificaciones Adicionales en CE |
| 72 | sisvad_ho | varchar | 1 | Considera verificaciones adicionales en HO |
| 73 | sisvad_em | varchar | 1 | Considera verificaciones adicionales en EM |
| 74 | sisfgr | varchar | 1 | Tipo de Impresión en FACTURACIÓN EN GRUPO |
| 75 | siscth | varchar | 1 | Considera Farmacia en Honorarios |
| 76 | sisvpr | varchar | 1 | Verifica Protocolos de Atención |
| 77 | sischc | varchar | 1 | Considera Hospitalización |
| 78 | sishch | int | 4 | Hora de Corte en Hospitalización |
| 79 | sissprt | varchar | 1 | Controla copias documentos CAJA |
| 80 | sisvif | varchar | 1 | Visualiza Ícono Filiación |
| 81 | sisapre_ce | varchar | 1 | Siempre abre prefactura en CITAS |
| 82 | sisadp_ce | varchar | 1 | Actualiza datos paciente en CITAS |
| 83 | sisadp_em | varchar | 1 | Actualiza datos paciente en EMERGENCIA |
| 84 | sisadp_ho | varchar | 1 | Actualiza datos paciente en HOSPITALIZACIÓN |
| 85 | sisadp_pm | varchar | 1 | Actualiza datos paciente en PROCEDIMIENTOS |
| 86 | sisvstk_fa | varchar | 1 | Verifica Stocks Farmacia en ACTO MÉDICO |
| 87 | sisvstk_ho | varchar | 1 | Verifica Stocks Farmacia en ACTO MÉDICO |
| 88 | sishmp_qx | varchar | 1 | Modifica Porcentaje o Monto en Honorarios |
| 89 | sismmed_fa | varchar | 1 | Modifica Médico en FARMACIA |
| 90 | siscat_fa | varchar | 1 | Considera Categorías en Farmacia |
| 91 | sistdcto | varchar | 8 | Tarifa descuento Asegurado |
| 92 | sisfar_m | varchar | 1 | Modifica coaseguro y distrib. de pago en Farmacia |
| 93 | sisexa_m | varchar | 1 | Modifica coaseguro y distrib. de pago en Exámenes |
| 94 | sislab_m | varchar | 1 | Modifica coaseguro y distrib. de pago en Laboratorio |
| 95 | sisiqx_m | varchar | 1 | Modifica coaseguro y distrib. de pago en Quirófano |
| 96 | sismiv | numeric | 9 | Monto máximo Ingresos Varios |
| 97 | sismev | numeric | 9 | Monto máximo Egresos Varios |
| 98 | sismdam | varchar | 1 | Muestra datos default en AM |
| 99 | siscap_pa | varchar | 1 | Cierre automático de prefacturas particulares |
| 100 | sistequi | varchar | 2 | Tarifa asociada a equipos quirúrgicos |
| 101 | sissmn | varchar | 5 | Símbolo monetario |
| 102 | sisnmn | varchar | 15 | Moneda |
| 103 | siscdfin_ho | varchar | 1 | Considera deducibles y coaseguros finales en HO |
| 104 | sistded_fin | varchar | 8 | Tarifa deducibles final |
| 105 | sistcoa_fin | varchar | 8 | Tarifa coaseguros final |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_sistema | clustered, unique, primary key located on PRIMARY | siscod |

**Referencias desde:**
- `.citas`: `fk_citas_siscod`
- `.citas_ea`: `fk_siscod_citas_ea_sistema`
- `.contratantes`: `fk_siscod_contratantes_sist`
- `.ea_ordenes_cabecera`: `fk_ea_ordenes_cabe_s`
- `emergencia`: `fk_emergencia_siscod`
- `.fa_ventas_cabecera`: `fk_fa_ventas_cabe_sis`
- `.hospitalizacion`: `fk_hospitalizacion_sisco`
- `.intervenciones_cabecera`: `fk_intervencione`
- `la_ordenes_cabecera`: `fk_la_ordenes_cabe_s`
- `.plantillas_informes`: `fk_plantillas_inform`
- `.prefacturas`: `fk_prefacturas_siscod`
- `procedimientos_cabecera`: `fk_procedimiento`
- `usuarios`: `fk_usuarios_siscod`

---

## Tabla: tarifario_tabla

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | tarcod | varchar | 8 | Código de tarifa |
| 2 | parcod | varchar | 2 | Código de categoría |
| 3 | tarval | numeric | 9 | Precio del servicio |
| 4 | tarcos | numeric | 9 | Costo por Categoría |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tarifario_tabla | clustered, unique, primary key located on PRIMARY | tarcod, parcod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_tarifari_ref_328_tarifari | FOREIGN KEY | tarcod REFERENCES tarifario (tarcod) |

---

## Tabla: tipo_acompanante

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | taccod | varchar | 2 | Código de tipo de acompañante |
| 2 | tacdes | varchar | 20 | Descripción del tipo de acompañante |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tipo_acompanante | clustered, unique, primary key located on PRIMARY | taccod |

**Referencias desde:**
- `emergencia`: `fk_emergenc_ref_26_tipo_aco`

---

## Tabla: tipo_actividad

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | taccod | varchar | 2 | Código |
| 2 | tacdes | varchar | 40 | Descripción |
| 3 | tacpor | numeric | 9 | Porcentaje de Contribución (COTIZACIONES) |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_taccod_tipo_actividad | clustered, unique, primary key located on PRIMARY | taccod |

---

## Tabla: tipo_anestesia

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codanes | varchar | 4 | Código Anestesia |
| 2 | desanes | varchar | 40 | Descripción |
| 3 | obsanes | varchar | 40 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tipo_anestesia_codanes | clustered, unique, primary key located on PRIMARY | codanes |

**Referencias desde:**
- `.intervenciones_cabecera`: `fk_intervencione`

---

## Tabla: tipo_asegurado

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codtas | varchar | 2 | Código |
| 2 | destas | varchar | 20 | Descripción |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tipo_asegurado | clustered, unique, primary key located on PRIMARY | codtas |

---

## Tabla: tipo_atencion_emergencia

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | temtip | varchar | 2 | Código de tipo de atención en emergencia |
| 2 | temdes | varchar | 20 | Descripción de tipo de atención en emergencia |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tipo_atencion_emergencia | clustered, unique, primary key located on PRIMARY | temtip |

**Referencias desde:**
- `emergencia`: `fk_emergenc_ref_137_tipo_ate`

---

## Tabla: tipo_cambio

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | datcam | datetime | 8 | Fecha de cambio |
| 2 | tipcam | numeric | 9 | Valor de cambio |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tipo_cambio | clustered, unique, primary key located on PRIMARY | datcam |

---

## Tabla: tipo_citado

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | tcicod | varchar | 2 | Código de tipo de citado |
| 2 | tcides | varchar | 20 | Descripción de tipo de citado |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tipo_citado | clustered, unique, primary key located on PRIMARY | tcicod |

**Referencias desde:**
- `citas`: `fk_citas_ref_41_tipo_cit`

---

## Tabla: tipo_cliente

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | tipcli | varchar | 2 | Código de tipo de cliente |
| 2 | descli | varchar | 20 | Descripción de tipo de cliente |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tipo_cliente | clustered, unique, primary key located on PRIMARY | tipcli |

**Referencias desde:**
- `.ea_ordenes_cabecera`: `fk_ea_orden_ref_306_`
- `fa_ventas_cabecera`: `fk_fa_venta_ref_104_t`
- `la_ordenes_cabecera`: `fk_la_orden_ref_306_`
- `.procedimientos_cabecera`: `fk_proce_ref_001`

---

## Tabla: tipo_complicacion

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codcmpl | varchar | 5 | Código Tipo de Complicación |
| 2 | descmpl | varchar | 140 | Descripción |
| 3 | obscmpl | varchar | 40 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tipo_complicacion_codcmpl | clustered, unique, primary key located on PRIMARY | codcmpl |

---

## Tabla: tipo_egreso

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codigo | varchar | 2 | Código Tipo de Egreso |
| 2 | tipo_egreso | varchar | 20 | Descripción |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tipo_egreso | nonclustered, unique, primary key located on PRIMARY | codigo |

---

## Tabla: tipo_incapacidad

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | tincod | varchar | 2 | Código de Tipo Incapacidad |
| 2 | tindes | varchar | 20 | Descripción |
| 3 | tinpor | numeric | 9 | Porcentaje de Contribución |
| 4 | tinobs | varchar | 40 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tincod_tip_incapacidad | clustered, unique, primary key located on PRIMARY | tincod |

**Referencias desde:**
- `.aportes_empresariales_detalle`: `fk_tincod_`
- `.incapacidades`: `fk_tincod_incapacidades_ti`

---

## Tabla: tipo_intervenciones

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | inttip | varchar | 2 | Código de tipo de interviniente |
| 2 | destip | varchar | 60 | Descripción de tipo de interviniente |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tipo_intervencion | nonclustered, unique, primary key located on PRIMARY | inttip |

**Referencias desde:**
- `intervenciones_quirurgicas`: `pk_int_quir_0`

---

## Tabla: tipo_movimiento_historia

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | tmmcod | varchar | 2 | Código de tipo de movimiento de historia |
| 2 | tmmdes | varchar | 30 | Descripción de tipo de movimiento de historia |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tipo_movimiento_historia | clustered, unique, primary key located on PRIMARY | tmmcod |

**Referencias desde:**
- `movimientos_historias`: `fk_mov_histori_tip`

---

## Tabla: tipo_operacion_honorarios

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | tipope | varchar | 2 | Código de tipo de operación de honorarios |
| 2 | descripcion | varchar | 40 | Descripción de tipo de operación de honorarios |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tipo_operacion_honorarios | clustered, unique, primary key located on PRIMARY | tipope |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| ckc_tipope_tipo_ope | CHECK Table Level | (substring([tipope],1,1) = 'I' or substring([tipope],1,1) = 'E') |

---

## Tabla: tipo_paciente

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | tppcod | varchar | 2 | Código de tipo de paciente |
| 2 | tppdes | varchar | 25 | Descripción de tipo de paciente |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tipo_paciente | clustered, unique, primary key located on PRIMARY | tppcod |

**Referencias desde:**
- `.hospitalizacion`: `fk_hospital_ref_684_tipo`
- `.pacientes`: `fk_paciente_ref_53_tipo_pac`

---

## Tabla: tipo_pago_facturacion

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | tpacod | varchar | 2 | Código de tipo de pago |
| 2 | tpades | varchar | 20 | Descripción de tipo de pago |
| 3 | tpadias | int | 4 | Número de días asociados para crédito |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tipo_pago_fac | clustered, unique, primary key located on PRIMARY | tpacod |

**Referencias desde:**
- `facturas`: `fk_fact_ref_tipo_pago`

---

## Tabla: tipo_participantes

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | tippar | varchar | 2 | Código de tipo de participante |
| 2 | despar | varchar | 30 | Descripción de tipo de participante |
| 3 | monpor | numeric | 9 | Porcentaje del monto base |
| 4 | obserpar | varchar | 60 | Observaciones |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_interv_participantes | nonclustered, unique, primary key located on PRIMARY | tippar |

**Referencias desde:**
- `.intervenciones_participantes`: `fk_int_par_`
- `.intervenciones_perfiles`: `pk_interven_perf`

---

## Tabla: tipo_profesional

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | tprcod | varchar | 2 | Código de tipo de profesional |
| 2 | tprdes | varchar | 25 | Descripción de tipo de profesional |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_tipo_profesional | clustered, unique, primary key located on PRIMARY | tprcod |

**Referencias desde:**
- `medicos`: `fk_medicos_ref_40_tipo_pro`

---

## Tabla: traslado

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | invnum | int | 4 | Secuencia de Hospitalización |
| 2 | fecha | datetime | 8 | Fecha del Traslado |
| 3 | pachis | varchar | 7 | Número de historia |
| 4 | hoscam | varchar | 4 | Código de cama |
| 5 | obstra | varchar | 40 | Observación |
| 6 | sercod | varchar | 4 | Código del servicio |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_traslado | nonclustered, unique, primary key located on PRIMARY | invnum, fecha |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_traslado_ref_1057_hospital | FOREIGN KEY | invnum REFERENCES hospitalizacion (invnum) |

---

## Tabla: turnos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | medcod | varchar | 4 | Código del médico |
| 2 | turcod | varchar | 2 | Código de turno (01,02) |
| 3 | turini | datetime | 8 | Hora de inicio de turno |
| 4 | turfin | datetime | 8 | Hora de fin de turno |
| 5 | turrat | int | 4 | Ratio o Número de citas por hora |
| 6 | turdis | varchar | 1 | Estado de disponibilidad del turno |
| 7 | turlun | varchar | 1 | Estado de atención (Lunes) |
| 8 | turmar | varchar | 1 | Estado de atención (Martes) |
| 9 | turmie | varchar | 1 | Estado de atención (Miércoles) |
| 10 | turjue | varchar | 1 | Estado de atención (Jueves) |
| 11 | turvie | varchar | 1 | Estado de atención (Viernes) |
| 12 | tursab | varchar | 1 | Estado de atención (Sábado) |
| 13 | turdom | varchar | 1 | Estado de atención (Domingo) |
| 14 | turobs | varchar | 80 | Observaciones |
| 15 | codcon | varchar | 4 | Código de consultorio |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_turnos | clustered, unique, primary key located on PRIMARY | medcod, turcod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_turnos_ref_104_medicos | FOREIGN KEY | medcod REFERENCES medicos (medcod) |

---

## Tabla: turnos_ea

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | medcod | varchar | 4 | Código del Médico |
| 2 | turcod | varchar | 2 | Código de Turno (01, 02) |
| 3 | turini | datetime | 8 | Hora de Inicio de Turno |
| 4 | turfin | datetime | 8 | Hora de Fin de Turno |
| 5 | turrat | int | 4 | Ratio o Número de citas por Hora |
| 6 | turdis | varchar | 1 | Estado de disponibilidad del Turno |
| 7 | turlun | varchar | 1 | Estado de atención (Lunes) |
| 8 | turmar | varchar | 1 | Estado de atención (Martes) |
| 9 | turmie | varchar | 1 | Estado de atención (Miércoles) |
| 10 | turjue | varchar | 1 | Estado de atención (Jueves) |
| 11 | turvie | varchar | 1 | Estado de atención (Viernes) |
| 12 | tursab | varchar | 1 | Estado de atención (Sábado) |
| 13 | turdom | varchar | 1 | Estado de atención (Domingo) |
| 14 | turobs | varchar | 80 | Observaciones |
| 15 | codcon | varchar | 4 | Código del Consultorio |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_turnos_ea_medcod_turcod | clustered, unique, primary key located on PRIMARY | medcod, turcod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_turnos_ea_medicos | FOREIGN KEY | medcod REFERENCES medicos (medcod) |

---

## Tabla: turnos_servicios

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | medcod | varchar | 4 | Código de médico |
| 2 | turtse | varchar | 2 | Servicio de referencia (HO, EM) |
| 3 | turcod | varchar | 2 | Código de turno |
| 4 | turini | datetime | 8 | Hora de inicio |
| 5 | turfin | datetime | 8 | Hora de fin |
| 6 | turrat | int | 4 | Ratio o número de citas por hora |
| 7 | turdis | varchar | 1 | Estado de disponibilidad |
| 8 | turlun | varchar | 1 | Estado de atención (Lunes) |
| 9 | turmar | varchar | 1 | Estado de atención (Martes) |
| 10 | turmie | varchar | 1 | Estado de atención (Miércoles) |
| 11 | turjue | varchar | 1 | Estado de atención (Jueves) |
| 12 | turvie | varchar | 1 | Estado de atención (Viernes) |
| 13 | tursab | varchar | 1 | Estado de atención (Sábado) |
| 14 | turdom | varchar | 1 | Estado de atención (Domingo) |
| 15 | turobs | varchar | 80 | Observaciones |
| 16 | codcon | varchar | 4 | Código de consultorio |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_turnos_servicios | clustered, unique, primary key located on PRIMARY | medcod, turtse, turcod |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| chk_turnos_ser | CHECK Table Level | ([turtse] = 'EM' or [turtse] = 'HO') |
| fk_turnos_ser_ref_104_medicos | FOREIGN KEY | medcod REFERENCES medicos (medcod) |

---

## Tabla: ubigeo

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | ubicod | varchar | 6 | Código de ubicación geográfica |
| 2 | ubides | varchar | 40 | Descripción de ubicación geográfica |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_ubigeo | clustered, unique, primary key located on PRIMARY | ubicod |

**Referencias desde:**
- `cobradores_zonas`: `fk_cobradores_zonas_cob`
- `pacientes`: `fk_paciente_ref_96_ubigeo`

---

## Tabla: usuarios

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | usecod | int | 4 | Código de usuario |
| 2 | usepas | varchar | 6 | Clave de acceso del usuario |
| 3 | usenam | varchar | 30 | Nombre del usuario |
| 4 | useusr | varchar | 10 | Nombre corto del usuario |
| 5 | usesgl | varchar | 3 | Siglas del usuario (3) |
| 6 | grucod | varchar | 6 | Código de grupo asociado |
| 7 | usprt | varchar | 2 | Impresora 1 (versión anterior) |
| 8 | useprf | varchar | 2 | Impresora 2 (versión anterior) |
| 9 | useprb | varchar | 2 | Impresora 3 (versión anterior) |
| 10 | siscod | int | 4 | Código Establecimiento |
| 11 | usefor | varchar | 20 | Modelos de Historia Restringidos |
| 12 | useprt | varchar | 1 | useprt |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_usuarios | clustered, unique, primary key located on PRIMARY | usecod |
| usuarios_usepas_usenam | nonclustered, unique located on PRIMARY | usepas, usenam |

### Constraints
| Nombre Constraint | Tipo Constraint | Constraint Keys |
|---|---|---|
| fk_usuarios_siscod | FOREIGN KEY | siscod REFERENCES sistema (siscod) |

**Referencias desde:**
- `.cajeros_usuarios`: `FK_CAJEROS_REF_195_USU`
- `citas`: `fk_citas_ref_432_usuarios`
- `.citas_ea`: `fk_usecod_citas_ea_usuarios`
- `.ea_movimientos`: `fk_ea_mov_ref_usuarios`
- `.ea_movimientos_temp`: `fk_ea_mov_ref_usuari`
- `.ea_ordenes_cabecera`: `fk_ea_orden_ref_165_`
- `emergencia`: `fk_emergenc_ref_445_usuarios`
- `emergencia_epicrisis`: `fk_emer_epicrisis_r`
- `.fa_faltantes`: `fk_fa_usuari_fal`
- `fa_kardex`: `fk_fa_usuarios_k`
- `.fa_movimientos`: `pk_fa_movi_ref_usuario`
- `.fa_movimientos_temp`: `pk_fa_movi_temp_ref_`
- `fa_variacion_descuentos`: `fk_fa_usuarios_d`
- `fa_variacion_precios`: `fk_fa_usuarios`
- `.fa_ventas_cabecera`: `fk_fa_venta_ref_102_u`
- `facturas`: `fk_fact_ref_usuarios`
- `hospitalizacion`: `fk_hospital_ref_614_usua`
- `hospitalizacion_epicrisis`: `fk_hosp_epicri`
- `intervenciones_epicrisis`: `fk_interven_epi`
- `la_movimientos`: `fk_la_mov_ref_usuarios`
- `la_movimientos_temp`: `fk_la_mov_ref_usuari`
- `la_ordenes_cabecera`: `fk_la_orden_ref_165_`
- `.movimientos_historias`: `fk_mov_histori_usu`
- `pacientes`: `fk_paciente_ref_345_usuarios`
- `prefacturas`: `fk_prefactu_ref_178_usuarios`
- `procedimientos_cabecera`: `fk_proce_ref_001`
- `.seguridad_historias`: `fk_useaut_segu_histo`
- `servicios_usuarios`: `FK_SERVICIO_USUAR_RE`

---

## Tabla: videos

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codvid | varchar | 12 | Código del video |
| 2 | nivvid | int | 4 | Nivel del video para la agrupación |
| 3 | desvid | varchar | 40 | Descripción del video |
| 4 | filvid | varchar | 20 | Nombre del archivo gráfico (sin directorio) |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_videos | nonclustered, unique, primary key located on PRIMARY | codvid |

---

## Tabla: visuales

| No | Campo | Tipo | Longitud | Descripción |
|---|---|---|---|---|
| 1 | codgra | varchar | 12 | Código del gráfico |
| 2 | nivgra | int | 4 | Nivel del gráfico por la agrupación |
| 3 | desgra | varchar | 40 | Descripción del gráfico |
| 4 | filgra | varchar | 20 | Nombre del archivo gráfico (sin directorio) |

### Índices
| Nombre Índice | Descripción | Index Keys |
|---|---|---|
| pk_visuales | nonclustered, unique, primary key located on PRIMARY | codgra |