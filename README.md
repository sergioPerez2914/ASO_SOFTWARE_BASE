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

La aplicación arranca con datos de ejemplo en memoria (`"UseMock": true` en
`appsettings.local.json`), así que no hace falta base de datos para probarla.
Usuario de prueba: `admin` / `admin123`.

## Estructura

```
ASO/
├── ASO.slnx
└── ASO.Desktop/          # Aplicación WPF (MVVM ligero)
    ├── Models/           # Entidades del dominio
    ├── Services/         # Servicios de dominio + fuentes de datos (interfaces y mocks)
    ├── ViewModels/       # Lógica de presentación
    ├── Views/            # Pantallas y editores
    ├── Navigation/       # Catálogo de módulos y submódulos (fuente única)
    ├── Configuration/    # Configuración y composición de fuentes de datos
    ├── BD/               # EF Core / SQL Server
    ├── Controls/         # Sidebar y componentes reutilizables
    └── Styles/           # Paleta y estilos
```

## Módulos

| Módulo | Submódulos |
|---|---|
| Operaciones | Registro de Operación · Seguimiento |
| Flota | Gestión de Flota · Mantenimiento · Telemetría *(pendiente)* |
| Inventario | Repuestos · Combustible · Producto |
| Nómina | Liquidaciones · Empleados · Gestión de Horarios |
| Finanzas | Cuentas por Cobrar · Cuentas por Pagar · Tarifas |

## Estado del proyecto

Los 13 submódulos están construidos y funcionan sobre datos en memoria; falta **Flota · Telemetría**.

Pendiente:

- Persistencia real (Entity Framework Core + SQL Server), en construcción por el socio.
- Matriz de roles y permisos: los comandos ya piden su permiso, pero la sesión los concede todos.
- Reglas de negocio de Nómina y Finanzas pendientes de confirmación (tarifario, formatos y turnos
  reales). Están marcadas en el código con `// PROVISIONAL:`.
