# Software ASO

Sistema de gestión para una empresa de cosecha mecanizada y transporte de caña de azúcar (zafra).

## Requisitos

- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11

## Ejecutar

```bash
cd ASO.Desktop
dotnet run
```

O abrir `ASO.slnx` en Visual Studio 2022.

Hace falta **SQL Server**: copia `ASO.Desktop/appsettings.local.example.json` como
`appsettings.local.json` y pon ahí tu cadena de conexión. Luego aplica el esquema:

```bash
cd ASO.Desktop
dotnet ef database update
```

En el primer arranque contra una base sin usuarios, la aplicación pide el nombre del núcleo y crea
el usuario desarrollador. No hay usuarios ni contraseñas por defecto.

## Estructura

```
ASO/
├── ASO.slnx
└── ASO.Desktop/          # Aplicación WPF (MVVM ligero)
    ├── Models/           # Entidades del dominio
    ├── Services/         # Servicios de dominio, sesión/permisos y contratos de datos
    ├── ViewModels/       # Lógica de presentación
    ├── Views/            # Pantallas y editores
    ├── Navigation/       # Catálogo de módulos y submódulos (fuente única)
    ├── Configuration/    # Configuración y composición de fuentes de datos
    ├── BD/               # EF Core / SQL Server (DbContext + fuentes Sql…)
    ├── Migrations/       # Migraciones EF Core
    ├── Controls/         # Sidebar y componentes reutilizables
    └── Styles/           # Paleta y estilos
```

## Módulos

| Módulo | Submódulos |
|---|---|
| Operaciones | Registro de Operación · Seguimiento · Fincas y Núcleos |
| Flota | Gestión de Flota · Mantenimiento · Telemetría *(pendiente)* |
| Inventario | Repuestos · Combustible · Producto |
| Nómina | Liquidaciones · Empleados · Gestión de Horarios |
| Finanzas | Cuentas por Cobrar · Cuentas por Pagar · Tarifas |

Más tres secciones fijas según el rol: **Inicio**, **Peticiones** y **Administración** (núcleos,
usuarios y permisos).

## Roles

| Rol | Qué puede |
|---|---|
| **Remesero** | Lo del día a día en campo: remesas, seguimiento, flota, mantenimiento, horarios y combustible. Lo sensible (anular, recepción, alta de flota, recarga de cisterna) lo **solicita** al administrador |
| **Administrador de núcleo** | Todo dentro de su núcleo, incluidos los usuarios y la bandeja de peticiones |
| **Desarrollador** | Todo, y es el único que puede cambiar de núcleo sin cerrar sesión |

Cada núcleo es una instalación aislada: sus datos no se cruzan con los de ningún otro.

## Estado del proyecto

Los 14 submódulos están construidos; falta **Flota · Telemetría**. Las entidades de dominio
persisten en SQL Server (EF Core Migrations) y los datos están aislados por núcleo. La matriz de
roles y permisos está conectada: los comandos que piden un permiso ahora lo exigen de verdad.

Pendiente:

- La autorización se comprueba en la interfaz, no dentro de los servicios de dominio.
- Reglas de negocio de Nómina y Finanzas pendientes de confirmación (tarifario, formatos y turnos
  reales). Están marcadas en el código con `// PROVISIONAL:`.
