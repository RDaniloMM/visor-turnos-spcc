# Despliegue en IIS

Documento preparatorio. No se ha realizado ningun despliegue.

## Precondiciones

- Windows Server autorizado y Hosting Bundle de .NET LTS aprobado.
- IIS con certificado HTTPS corporativo.
- Driver ODBC y DSN de la misma arquitectura que el Application Pool.
- Identidad de servicio o gMSA validada contra SQL Server con `SELECT` minimo.
- Firewall de IIS a SQL Server y de los televisores a IIS.
- Decisiones de `docs/decisions.md` resueltas.

## Configuracion

1. Crear `appsettings.Production.json` fuera del control de versiones.
2. Configurar primero un `Site:Code` y nombre de sede validados; sin ellos la aplicacion falla al iniciar incluso con la fuente deshabilitada.
3. Configurar el DSN autorizado `LOLCLI9000`, las reglas aprobadas y la cadena ODBC integrada antes de iniciar la aplicacion.
4. Confirmar la conexion ODBC en la ventana coordinada con el DBA.
5. Configurar el Application Pool como `No Managed Code`, una sola instancia y la identidad aprobada.
6. Verificar proceso en `/health/live`, fuente en `/health/ready`, pantalla en `/turnos` y contrato en `/api/turnos/actuales` sin exponer detalles internos.

## Publicacion

```powershell
dotnet publish .\visor_turnos.csproj --configuration Release --output .\artifacts\publish
```

Produccion no necesita Node.js: el JavaScript compilado queda dentro del resultado publicado.

## Rollback

Conservar el artefacto anterior, detener el Application Pool, restaurar el directorio anterior y arrancar el pool. Si existe duda sobre datos o permisos, detener el sitio hasta resolverla; la aplicacion no escribe en LOLCLI ni muestra datos demo.
