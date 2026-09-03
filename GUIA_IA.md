# Guía de contexto para IA — PaletsWebApp

## Propósito de este archivo

Este documento proporciona a una IA el contexto mínimo necesario para trabajar correctamente en el proyecto sin tener que reinterpretarlo desde cero. No contiene credenciales reales.

Al iniciar una conversación nueva, adjunta este archivo y pide a la IA que también lea `CLAUDE.md` antes de modificar código.

## Resumen del proyecto

`PaletsWebApp` es una aplicación web de logística para administrar pallets y controlar transferencias entre usuarios. Incluye interfaz web MVC y endpoints de API dentro del mismo proyecto ASP.NET Core.

El sistema permite:

- Registrar, editar, buscar, filtrar y consultar pallets.
- Transferir uno o varios pallets entre usuarios.
- Consultar transferencias enviadas y recibidas.
- Aceptar, rechazar o anular transferencias.
- Gestionar reclamos relacionados con pallets.
- Administrar usuarios, perfiles, roles y estados de cuenta.
- Adjuntar evidencias fotográficas.
- Enviar notificaciones por correo y Firebase Cloud Messaging.
- Consumir la lógica desde clientes móviles mediante la API.

## Tecnologías principales

- .NET 6 y ASP.NET Core MVC.
- Razor Views.
- Entity Framework Core 6.
- SQL Server.
- ASP.NET Core Identity.
- API mediante controladores MVC.
- Firebase Admin, Firebase Cloud Messaging y Google Cloud Storage.
- AspNetCoreHero ToastNotification.
- Newtonsoft.Json y System.Text.Json.
- Paginación propia mediante `PaginatedList<T>`.

## Arquitectura actual

El proyecto es una aplicación monolítica MVC con API incluida:

```text
Controllers/   Acciones MVC y endpoints HTTP
Data/          DbContext, vistas SQL y credenciales locales ignoradas
Migrations/    Migraciones de Entity Framework Core
Models/        Entidades y modelos asociados a vistas SQL
Utilites/      Inicialización, roles, correo y notificaciones
ViewModels/    Modelos para formularios y respuestas
Views/         Interfaz Razor
wwwroot/       CSS, JavaScript, imágenes y archivos estáticos
```

No crear capas, patrones o abstracciones adicionales salvo que una necesidad concreta lo justifique. Seguir el principio de cambios pequeños indicado en `CLAUDE.md`.

## Archivos principales

- `Program.cs`: registra servicios, Identity, SQL Server, sesión, notificaciones y rutas MVC.
- `Data/ApplicationDbContext.cs`: contexto de Entity Framework e integración con Identity.
- `Controllers/PaletsController.cs`: gestión web de pallets.
- `Controllers/TransfersController.cs`: gestión web de transferencias y reclamos.
- `Controllers/UsersController.cs`: autenticación, perfiles y administración de usuarios.
- `Controllers/ApiController.cs`: endpoints consumidos por aplicaciones externas.
- `FirebaseStorageService.cs`: carga de evidencias a Firebase Storage.
- `Utilites/Utils.cs`: correo, notificaciones y utilidades compartidas.
- `Utilites/DbInitializer.cs`: creación de roles, usuarios y catálogos iniciales.
- `PaginatedList.cs`: paginación utilizada por las vistas.

## Modelo de datos

### ApplicationUser

Extiende ASP.NET Core Identity y contiene nombres, apellidos, documento, dirección, razón social, teléfono, fotografía, estado activo, eliminado y token de Firebase.

### Palet

Campos principales:

- `Id`
- `Descripcion`
- `Observaciones`
- `Estado`
- `FechaCreacion`
- `ApplicationUserId`

El propietario se relaciona con `ApplicationUser`.

### Transferencia

Campos principales:

- `Id`
- `CodigoInterno`
- Fechas de envío, recibo, rechazo y anulación
- Usuario que envía
- Usuario que recibe
- Estado
- Fotografía
- Observaciones

### Detalle

Relaciona una transferencia con cada pallet incluido mediante `IdTransferencia` e `IdPalet`.

### Catalogo

Almacena estados y tipos de documento. Parte de la lógica actual localiza estados mediante su descripción; no renombrar valores de catálogo sin revisar todas esas consultas.

## Vistas SQL requeridas

`ApplicationDbContext` espera que existan:

- `View_Users`
- `View_Palets`
- `View_Transferencias`

Sus modelos correspondientes son `View_User`, `View_Palet` y `View_Transferencia`.

Las migraciones crean tablas, pero se debe confirmar por separado la existencia de estas vistas al preparar una base de datos nueva.

## Roles

Los roles previstos son:

- `Admin`
- `Cliente`
- `Bodeguero`
- `Chofer`
- `Invitado`

La autorización se implementa mediante atributos `[Authorize]`, roles de Identity y verificaciones dentro de algunos controladores.

## Reglas principales del negocio

1. Una transferencia contiene uno o varios pallets.
2. Al crear una transferencia, los pallets pasan al estado de transferencia correspondiente.
3. Al aceptar, los pallets válidos pasan al usuario receptor y quedan disponibles.
4. Al rechazar o anular, los pallets vuelven a estar disponibles cuando corresponde.
5. Un pallet reclamado no debe reasignarse como si fuera un pallet normal.
6. Los reclamos usan estados de transferencia y pallet específicos.
7. Los cambios de transferencia, detalle y pallet deben conservar consistencia entre sí.
8. Las fechas operativas se registran actualmente desde el servidor; revisar cuidadosamente cualquier cambio de zona horaria.

## API

Los endpoints usan principalmente el prefijo:

```text
/api/ApiAccess/
```

Operaciones relevantes:

```text
GET  Login
GET  GetUsers
GET  GetUserById
GET  GetPaletsByUser
GET  GetAllPalets
GET  GetTransfers
GET  GetTransferById
GET  GetTransferByPallet
GET  GetCatalogoByCategory
GET  GetRoles
POST AddTransfer
POST AddReclamo
POST ProcessTransfer
POST AnularTransfer
POST AddClient
POST ChangePasswordClient
```

Consultar siempre las firmas reales de `ApiController.cs` antes de cambiar contratos, parámetros o respuestas. La API convive con MVC y no está implementada como un proyecto independiente.

## Base de datos

La conexión se registra en `Program.cs`:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(connectionString));
```

Los archivos reales de configuración no deben estar en Git. Para preparar el entorno:

```powershell
Copy-Item appsettings.Example.json appsettings.json
Copy-Item Data\serviceAccountKey.Example.json Data\serviceAccountKey.json
```

Después se debe configurar `ConnectionStrings:DefaultConnection` y reemplazar la cuenta de servicio de ejemplo por un archivo descargado desde Firebase/Google Cloud.

## Archivos sensibles

Nunca mostrar, copiar a respuestas ni confirmar en Git los valores de:

- `appsettings.json`
- `appsettings.Development.json`
- `Data/serviceAccountKey.json`
- Contraseñas SMTP
- Claves privadas de Firebase
- Cadenas de conexión de producción
- Perfiles de publicación con credenciales

Los archivos seguros que documentan la estructura son:

- `appsettings.Example.json`
- `Data/serviceAccountKey.Example.json`

`Utilites/Utils.cs` es útil y debe conservarse, pero actualmente requiere especial cuidado porque integra correo y notificaciones. No eliminarlo como parte de limpiezas generales. Si se trabaja en seguridad, mover credenciales embebidas a configuración segura sin cambiar innecesariamente su comportamiento.

## Estado de limpieza del código

Se realizó una limpieza de controladores:

- Se eliminaron versiones antiguas comentadas de endpoints.
- Se eliminaron acciones y vistas `Index_old` sin referencias.
- Se retiraron rutas aparentes duplicadas provenientes de código comentado.
- Se eliminaron imports y dependencias que quedaron huérfanos.
- No existen rutas explícitas duplicadas en `ApiController` al momento de esta guía.
- La última verificación terminó con `0 errores` y `0 advertencias`.
- `Utilites/Utils.cs` se dejó intacto durante esa limpieza.

No volver a introducir implementaciones alternativas comentadas dentro de los controladores. Git debe conservar el historial de versiones anteriores.

## Rendimiento y timeout

Hubo un error previo de timeout de SQL causado por consultas pesadas. Se optimizaron consultas importantes para:

- Paginar transferencias antes de cargar la información asociada.
- Consultar solamente pallets pertenecientes a la página visible.
- Agrupar conteos de pallets en base de datos.
- Resolver `GetTransferByPallet` mediante una consulta con unión y proyección.

No aumentar el timeout ni agregar reintentos de SQL sin una razón nueva y medible. El usuario decidió conservar la configuración original porque las consultas optimizadas resolvieron el problema observado.

## Criterios para realizar cambios

Antes de editar:

1. Leer completamente `CLAUDE.md`.
2. Revisar el estado de Git y preservar cambios existentes del usuario.
3. Localizar el flujo real antes de asumir que una acción está activa.
4. Evitar cambios en `Utilites/Utils.cs` salvo petición explícita.
5. No modificar timeout o conexión de SQL salvo petición explícita.
6. No crear migraciones ni modificar la base de datos sin autorización clara.
7. No cambiar contratos públicos de la API sin explicar compatibilidad.
8. No eliminar estados o catálogos por nombre sin revisar todas sus referencias.
9. Mantener cambios quirúrgicos; no refactorizar áreas ajenas.
10. Compilar y reportar errores y advertencias al terminar.

## Comandos de preparación

```powershell
dotnet restore
dotnet ef database update
dotnet run
```

Si `dotnet ef` no está disponible:

```powershell
dotnet tool restore
```

Para verificar cambios:

```powershell
dotnet build
```

Visual Studio o IIS Express pueden bloquear `bin/Debug/net6.0/PaletsWebApp.dll`. Si ocurre, detener la aplicación o compilar hacia una salida temporal dentro del proyecto y eliminarla después.

## Limitaciones y mejoras pendientes conocidas

Estas observaciones no autorizan a cambiarlas automáticamente:

- No existe un proyecto de pruebas automatizadas.
- Parte de la lógica de negocio reside directamente en controladores.
- Algunos estados se comparan mediante texto.
- Debe revisarse la política de autorización de cada endpoint antes de producción.
- Deben externalizarse todas las credenciales SMTP restantes.
- Conviene verificar transacciones atómicas al modificar transferencia, detalles y pallets.
- Debe revisarse la creación de usuarios y catálogos de demostración antes de desplegar.

## Prompt recomendado para un chat nuevo

Puedes iniciar otro chat con este mensaje:

> Lee completamente los archivos `GUIA_IA.md` y `CLAUDE.md` antes de actuar. Analiza también los archivos directamente relacionados con mi solicitud. Respeta la lógica existente de pallets, transferencias, reclamos, usuarios y API. No expongas credenciales, no aumentes timeouts, no cambies contratos públicos ni modifiques `Utilites/Utils.cs` sin consultarme. Haz cambios pequeños, preserva trabajo existente y verifica con compilación. Mi solicitud actual es: [ESCRIBIR AQUÍ LA TAREA].

## Regla final

Este documento es contexto, no reemplaza la inspección del código actual. Si existe una diferencia entre esta guía y el repositorio, prevalece el código actual y la IA debe señalar la discrepancia antes de modificarlo.
