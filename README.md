# PaletsWebApp

Sistema web y API para administrar pallets y controlar sus transferencias entre usuarios. La aplicación centraliza el registro de pallets, el seguimiento de estados, la recepción o rechazo de transferencias, los reclamos y la gestión de usuarios.

## Funcionalidades

- Autenticación, recuperación de contraseña y control de acceso mediante ASP.NET Core Identity.
- Administración de usuarios, perfiles, roles y estados de cuenta.
- Registro, edición, búsqueda, filtrado y paginación de pallets.
- Creación de transferencias con uno o varios pallets.
- Consulta de transferencias enviadas y recibidas.
- Aceptación, rechazo y anulación de transferencias.
- Gestión de reclamos sobre pallets.
- Evidencias fotográficas almacenadas en Firebase Storage.
- Notificaciones por correo electrónico y Firebase Cloud Messaging.
- API para la integración con clientes móviles u otros sistemas.

## Roles

El sistema contempla los siguientes roles:

- `Admin`: administración general de usuarios, pallets y transferencias.
- `Cliente`: gestión de sus pallets y transferencias.
- `Bodeguero`: operaciones relacionadas con bodega.
- `Chofer`: operaciones asignadas al transporte.
- `Invitado`: acceso limitado según las autorizaciones configuradas.

Los permisos concretos se aplican mediante atributos de autorización en los controladores y comprobaciones de rol.

## Flujo principal

1. Un usuario registra o consulta sus pallets.
2. Selecciona uno o varios pallets disponibles y crea una transferencia hacia otro usuario.
3. Los pallets pasan al estado correspondiente mientras la transferencia está pendiente.
4. El receptor acepta o rechaza la operación.
5. Si se acepta, la propiedad de los pallets se asigna al receptor; si se rechaza o anula, se restablece su disponibilidad cuando corresponde.
6. Los casos observados pueden continuar mediante el flujo de reclamos.

## Tecnologías

- ASP.NET Core 6 MVC
- Razor Views
- API REST sobre controladores MVC
- Entity Framework Core 6
- ASP.NET Core Identity
- SQL Server
- Firebase Admin, Firebase Cloud Messaging y Google Cloud Storage
- AspNetCoreHero ToastNotification
- Newtonsoft.Json
- X.PagedList

## Estructura del proyecto

```text
Controllers/   Controladores MVC y endpoints de la API
Data/          DbContext y elementos de acceso a datos
Migrations/    Historial y snapshot de migraciones de Entity Framework Core
Models/        Entidades y modelos asociados a vistas SQL
Services/      Servicios auxiliares de la aplicación
Utilites/      Inicialización, roles, correo y notificaciones
ViewModels/    Modelos utilizados por vistas y solicitudes
Views/         Interfaz Razor organizada por módulo
wwwroot/       CSS, JavaScript, imágenes y archivos estáticos
```

## Requisitos

- .NET 6 SDK
- SQL Server o SQL Server Express
- Visual Studio 2022, VS Code o un editor compatible con .NET
- Credenciales de Firebase solamente si se utilizarán imágenes y notificaciones push
- Un servidor SMTP válido si se utilizarán notificaciones por correo

Puedes comprobar el SDK instalado con:

```powershell
dotnet --version
```

## Configuración de la base de datos

La conexión se obtiene en `Program.cs` mediante la clave `DefaultConnection`:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(connectionString));
```

Para desarrollo local, configura `ConnectionStrings:DefaultConnection` en `appsettings.Development.json` o mediante secretos de usuario. Ejemplo:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SERVIDOR;Database=paletsdb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

También puedes evitar guardar la cadena en archivos usando User Secrets:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=SERVIDOR;Database=paletsdb;Trusted_Connection=True;TrustServerCertificate=True"
```

El `ApplicationDbContext` utiliza las tablas de Identity y las entidades `Catalogos`, `Palets`, `Transferencias` y `Detalles`. Además, espera que la base de datos disponga de estas vistas:

- `View_Users`
- `View_Palets`
- `View_Transferencias`

## Instalación y ejecución

1. Restaura las dependencias:

```powershell
dotnet restore
```

2. Configura la cadena de conexión.

3. Aplica las migraciones pendientes:

```powershell
dotnet ef database update
```

Si `dotnet ef` no está instalado:

```powershell
dotnet tool install --global dotnet-ef --version 6.*
```

4. Ejecuta la aplicación:

```powershell
dotnet run
```

También puedes abrir `PaletsWebApp.sln` y ejecutarlo desde Visual Studio mediante el perfil del proyecto o IIS Express.

> Al iniciar, `DbInitializer` comprueba datos base, roles y catálogos. Revisa esta inicialización y cambia cualquier credencial temporal antes de desplegar el sistema.

## API

Los endpoints están agrupados bajo el prefijo `/api/ApiAccess`. Las operaciones principales incluyen:

- Inicio de sesión y consulta de usuarios.
- Consulta general o paginada de pallets.
- Consulta de transferencias y sus detalles.
- Búsqueda de una transferencia por pallet.
- Creación, procesamiento y anulación de transferencias.
- Creación de reclamos.
- Consulta de catálogos y roles.
- Registro de clientes y cambio de contraseña.

Ejemplos de rutas:

```text
GET  /api/ApiAccess/GetPaletsByUser
GET  /api/ApiAccess/GetAllPalets
GET  /api/ApiAccess/GetTransfers
GET  /api/ApiAccess/GetTransferById
GET  /api/ApiAccess/GetTransferByPallet
POST /api/ApiAccess/AddTransfer
POST /api/ApiAccess/AddReclamo
POST /api/ApiAccess/ProcessTransfer
POST /api/ApiAccess/AnularTransfer
```

Los parámetros, cuerpos y respuestas se definen en `ApiController` y en los modelos de `ViewModels`.

## Estados y catálogos

Los estados se almacenan como registros de catálogo. La inicialización incluye valores base para pallets, transferencias y tipos de documento. Entre los estados utilizados por los flujos se encuentran:

- Pallets: disponible, en transferencia, en reclamo, reclamado y dado de baja, según los catálogos existentes.
- Transferencias: por recibir, por reclamar, recibida o aceptada, rechazada y anulada, según el flujo correspondiente.

Evita cambiar manualmente las descripciones sin revisar las consultas que actualmente identifican algunos estados por su texto.

## Configuración externa

Para habilitar completamente las integraciones debes configurar fuera del repositorio:

- Credenciales de Firebase/Google Cloud.
- Identificador del proyecto y bucket de Firebase Storage.
- Cuenta y credenciales SMTP.
- Cadena de conexión de producción.

## Seguridad antes de publicar

Antes de subir o desplegar el proyecto:

- No publiques cadenas de conexión con usuario y contraseña.
- No publiques contraseñas SMTP, claves API ni cuentas de servicio de Firebase.
- Sustituye las credenciales iniciales o de demostración.
- Usa User Secrets en desarrollo y variables de entorno o un almacén seguro en producción.
- Excluye `bin/`, `obj/`, `.vs/`, archivos `*.user` y configuraciones locales del control de versiones.
- Revisa las políticas de contraseña y permisos de cada endpoint antes de exponer la API públicamente.

## Compilación

```powershell
dotnet build
```

## Estado del proyecto

El proyecto contiene una interfaz web MVC y una API en la misma aplicación. Actualmente no incluye un proyecto automatizado de pruebas; antes de producción se recomienda incorporar pruebas para transferencias, cambios de propietario, estados, autorización y reclamos.

## Licencia

Este repositorio no declara todavía una licencia. Agrega un archivo `LICENSE` antes de distribuirlo públicamente si corresponde.
