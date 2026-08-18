# ASO — Software ASO (Gestión de Zafra)

Sistema de gestión para una empresa de **cosecha mecanizada y transporte de caña de azúcar** (zafra).
Aplicación de escritorio **WPF · .NET 8** (`net8.0-windows`), instalación local en LAN, un solo centro.

- Repo: https://github.com/sergioPerez2914/ASO_SOFTWARE_BASE (rama `main`)
- Se desarrolla en colaboración con un socio. El socio construye la **base de datos** (EF Core Code-First / SQL Server).
- UI en **español**. El proyecto se llamó "SAO" por error; ya está todo renombrado a "ASO".

> ⚠️ **Decisión de arquitectura abierta (2026-08-17):** se evalúa mover la persistencia a
> **Supabase (PostgreSQL gestionado)** para dar acceso remoto desde varios sitios. Eso pondría en
> revisión las dos líneas de arriba: "local en LAN, un solo centro" y "SQL Server". El análisis, el
> impacto y el plan por fases están en **`docs/decision-supabase.md`**; **no está acordado con el
> socio todavía**. Hasta que se decida, seguir asumiendo EF Core / SQL Server local.

## Documentación de referencia

`docs/` guarda los documentos que aporta el equipo (especificaciones, modelo de datos del socio,
requisitos, formatos de ticket, tarifas). **Revisarla antes de implementar reglas de negocio,
modelos o pantallas**: lo que hay ahí manda sobre lo asumido aquí o en el código.

## Cómo ejecutar

Es escritorio, no web (no hay dev server / puerto). `dotnet run` dentro de `ASO.Desktop`, o F5 en Visual Studio (`ASO.slnx`).

## Decisiones de arquitectura acordadas

- **Se mantiene WPF** y se usa el shell actual como base (no se migra a WinForms pese al plan original).
- **MVVM ligero**: `ViewModels/ViewModelBase.cs` (INotifyPropertyChanged). Lógica fuera del code-behind.
- **Datos mock detrás de interfaces** para poder cambiar a EF Core sin tocar UI ni ViewModel.
  Ejemplo hecho: `Services/IInventoryDataSource.cs` (impl. `MockInventoryDataSource`). Cuando exista BD,
  el socio crea `EfInventoryDataSource : IInventoryDataSource` y se inyecta — sin cambiar la vista.
- **Regla de oro**: toda regla de negocio vive en servicios de dominio, nunca en eventos de botones.

## Estructura de módulos (reset de 2026-08-17)

Se eliminaron las pantallas anteriores (inventario, empleados, nómina/tarifas) y se rehízo la
presentación. Ahora son **cinco módulos con submódulos**; al entrar a un módulo se ve su dashboard
resumen y se despliega su lista de submódulos en el menú lateral.

| Módulo | Submódulos |
|---|---|
| Operaciones | Registro de Operación · Seguimiento |
| Flota | Gestión de Flota · Mantenimiento · Telemetría |
| Inventario | Repuestos · Combustible · Producto |
| Nómina | Liquidaciones · Empleados · Gestión de Horarios |
| Finanzas | Cuentas por Cobrar · Cuentas por Pagar · Tarifas |

- `Navigation/ModuloCatalogo.cs` — **fuente única** de la estructura (clave, nombre, descripción, icono,
  indicadores y submódulos). Sidebar, Inicio, dashboards y enrutado leen de aquí: agregar o renombrar
  un submódulo se hace en un solo lugar.
- `Controls/Sidebar.xaml(.cs)` + `ViewModels/SidebarViewModel.cs` — menú de dos niveles; solo emite
  `NavegacionSolicitada`. `MainWindow.Navegar(modulo, submodulo)` decide la vista y llama a
  `Sincronizar` para reflejar la selección (evita ciclos de navegación).
- `Views/InicioView` — lanzador con una tarjeta por módulo.
- `Views/ModuloDashboardView` — resumen del módulo: indicadores (aún en "—", sin datos) + tarjeta por
  submódulo.
- `Views/SubmoduloView` — submódulo en construcción, con ruta `Módulo · Submódulo` y "Volver al módulo".
- `Styles/Colors.xaml` + `Theme.xaml` — paleta y estilos (CardStyle, CardButtonStyle, NavItemStyle,
  NavSubItemStyle, DataGridStyle). Iconos = Segoe MDL2 Assets.
- Se conserva la infraestructura reutilizable: framework CRUD (`CrudViewModelBase`,
  `CrudEditorViewModelBase`, `CrudEditorWindow`, `IServicioDialogo`), login/sesión y la capa de datos
  (`Models`, `Services`, `BD`, `DataSourceFactory`) — todavía sin pantallas que la usen.
- El estado previo al reset quedó en la rama `respaldo/pre-reset-modulos`.

## El plan (SIGZ / ASO) — fases

Fase 0 fundación (auth, roles, maestros, shell) · **Fase 1** núcleo operativo (operaciones + flota +
combustible) · Fase 2 taller e inventario · Fase 3 finanzas (CxC/CxP/bancos) · Fase 4 nómina por destajo ·
Fase 5 dashboard gerencial + reportes · Fase 6 (post-MVP) offline, API REST, app móvil.

Roles: Admin, Operaciones, Taller, Finanzas, RRHH, Consulta. Todo se filtra por la **zafra activa**.

## Diseño del sistema de tickets ("documento de movimiento")

Los tres tickets comparten un mismo patrón: cabecera + líneas, máquina de estados, inmutabilidad tras
confirmar, efectos en una sola transacción, auditoría, `ZafraId`.

1. **Ticket de pesaje** (caña): bruto − tara = neto → toneladas. Efectos: acumula destajo, marca
   "no facturado" (→ FacturaCxC, sin doble facturación), KPIs de producción.
2. **Vale de combustible**: litros + horómetro. Efectos: descuenta cisterna, calcula L/ton y L/h,
   alerta si supera el promedio histórico × (1 + umbral%).
3. **Salida de inventario**: artículo + cantidad. Efectos: descuenta `StockActual`, costo a la hoja de
   vida del activo/OT, no permite salida sin stock (override Admin).

Las toneladas del pesaje alimentan L/ton, destajo y facturación. Los costos convergen en Finanzas y en la
hoja de vida del activo.

## Diseño de autorización de tickets (5 capas)

Estado: `Borrador → Pendiente aprobación → Confirmado (aplica efectos) → Facturado`, con ramas
`Rechazado`/`Anulado`. **Se valida en el servicio de dominio, no en el botón** (defensa en profundidad).

1. Permisos RBAC (`Módulo.Acción`, ej. `Pesaje.Aprobar`, `Finanzas.Facturar`).
2. Flujo de aprobación por niveles, configurable por tipo de doc y umbral (no hardcode).
3. Segregación de funciones: aprobador ≠ creador; quien registra no factura.
4. Autorización por excepción/umbral (override de stock = Admin; anular factura = Finanzas).
5. Auditoría de cada firma (quién, cuándo, decisión, comentario).

Entidades para la BD: Usuario/Rol/Permiso (+N:M), ReglaAprobacion, Aprobacion, y en cada documento
`Estado` + `CreadoPorId` + `ZafraId` + registro en `Auditoria`. En WPF: `ISesionActual` inyectado; comandos
con `CanExecute = sesion.Puede("...")`; secciones del sidebar filtradas por rol.

## Próximo paso sugerido

Con el shell ya reestructurado, el primer submódulo real sería **Operaciones · Registro de Operación**
(el ticket de pesaje): mock + interfaz + estados + `ISesionActual` + comandos con `CanExecute`, para
dejar la plantilla "documento de movimiento" que luego replican combustible e inventario.
