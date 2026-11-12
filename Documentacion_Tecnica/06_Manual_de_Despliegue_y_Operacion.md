# 6. Manual de Despliegue y Operación — SIARAWEB

## 6.1 Requisitos del Entorno

### Entorno de Desarrollo / Servidor
- **Sistema Operativo:** Windows Server 2019/2022, Windows 10/11 o Linux (Ubuntu 22.04+ / Debian 12+).
- **Runtime:** `.NET 9.0 SDK` (para compilación) y `ASP.NET Core 9.0 Runtime / Hosting Bundle` (para ejecución en IIS o Kestrel).
- **Motor de Base de Datos:** SQL Server 2019/2022 (Express, Standard, Developer o LocalDB).

---

## 6.2 Configuración de `appsettings.json`

El archivo `SIARAWEB/appsettings.json` define la cadena de conexión principal hacia SQL Server:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SiaraDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

> [!IMPORTANT]
> En entornos de producción con SQL Server dedicado, reemplace `Server=(localdb)\\mssqllocaldb` por la instancia correspondiente y configure credenciales SQL o autenticación integrada del pool de aplicaciones de IIS.

---

## 6.3 Aplicación de Migraciones e Inicialización de Base de Datos

Para crear o actualizar la base de datos `SiaraDB` con todas las tablas y relaciones:

```bash
dotnet ef database update --project SIARAWEB/SIARAWEB.csproj
```

Al iniciar la aplicación por primera vez (`dotnet run --project SIARAWEB/SIARAWEB.csproj`), el bloque de **Data Seeding** en `Program.cs` creará de forma automática:
1. Los 4 roles base en `AspNetRoles`:
   - `Administrador`
   - `JefeGeneral`
   - `JefeCarrera`
   - `Docente`
2. La cuenta inicial de administración técnica si no existe.

---

## 6.4 Almacenamiento de Archivos y Configuración de Periodos

1. **Repositorio de Evidencias PDF (`wwwroot/uploads/documentos/`):**
   - El controlador `DocumentsController` verifica y crea automáticamente el directorio `wwwroot/uploads/documentos` al recibir el primer archivo PDF.
   - Cada archivo se guarda con un prefijo `Guid.NewGuid()` para evitar colisiones de nombres y se elimina físicamente del disco cuando es reemplazado por una corrección o eliminado por el docente.
   - Asegúrese de que el usuario del proceso web (`IIS_IUSRS` o servicio de Kestrel) cuente con permisos de lectura, escritura y modificación sobre la carpeta `wwwroot/uploads`.

2. **Archivo de Configuración de Periodos (`period_settings.json`):**
   - El modelo `PeriodConfig` almacena en `SIARAWEB/period_settings.json` los nombres personalizables de los periodos escolares (`Periodo1`, `Periodo2`, `PeriodoExtra`), editables en caliente desde el panel del Administrador Técnico (`/AcademicPeriods/Index`).

---

## 6.5 Compilación y Publicación para Producción

Para generar el paquete compilado optimizado para despliegue (`Release`):

```bash
dotnet publish SIARAWEB/SIARAWEB.csproj -c Release -o ./publish
```

El directorio `./publish` contendrá los binarios listos para alojarse en **IIS** (mediante `AspNetCoreModuleV2`), **Azure App Service** o un contenedor **Docker** sobre Linux/Windows.
