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

| Módulo | Submódulos | Estado |
|---|---|---|
| Operaciones | Registro de Operación · Seguimiento | funcionales |
| Flota | Gestión de Flota · Mantenimiento · **Telemetría** | funcionales, salvo Telemetría (pendiente) |
| Inventario | Repuestos · Combustible · Producto | funcionales |
| Nómina | Liquidaciones · Empleados · Gestión de Horarios | funcionales |
| Finanzas | Cuentas por Cobrar · Cuentas por Pagar · Tarifas | funcionales |

**Los 13 submódulos están construidos sobre mocks; el único que falta es Flota · Telemetría.**
Las reglas de Nómina y Finanzas se implementaron con supuestos provisionales (ver más abajo),
porque el socio todavía no aportó tarifario real ni formatos de liquidación y factura.

- `Navigation/ModuloCatalogo.cs` — **fuente única** de la estructura (clave, nombre, descripción, icono,
  indicadores y submódulos). Sidebar, Inicio, dashboards y enrutado leen de aquí: agregar o renombrar
  un submódulo se hace en un solo lugar.
- `Controls/Sidebar.xaml(.cs)` + `ViewModels/SidebarViewModel.cs` — menú de dos niveles; solo emite
  `NavegacionSolicitada`. `MainWindow.Navegar(modulo, submodulo)` decide la vista y llama a
  `Sincronizar` para reflejar la selección (evita ciclos de navegación).
- `Views/InicioView` — lanzador con una tarjeta por módulo.
- `Views/ModuloDashboardView` — resumen del módulo: indicadores + tarjeta por submódulo. Los valores
  los calcula `ModuloDashboardViewModel.CalcularIndicadores` (un `switch` por clave de módulo); todos
  los módulos tienen datos reales salvo el "Tiempo muerto" de Operaciones, sin definir.
- `Views/SubmoduloView` — submódulo en construcción, con ruta `Módulo · Submódulo` y "Volver al módulo".
- `Styles/Colors.xaml` + `Theme.xaml` — paleta y estilos (CardStyle, CardButtonStyle, NavItemStyle,
  NavSubItemStyle, DataGridStyle). Iconos = Segoe MDL2 Assets.
- Framework CRUD reutilizable (`CrudViewModelBase`, `CrudEditorViewModelBase`, `CrudEditorWindow`,
  `IServicioDialogo`), login/sesión y capa de datos (`Models`, `Services`, `BD`, `DataSourceFactory`):
  hoy los usan los 13 submódulos.
- El estado previo al reset quedó en la rama `respaldo/pre-reset-modulos`.

## Cómo se agrega un submódulo (receta)

Archivos a **crear**: `Models/<X>.cs` (con `Clonar()`, snapshots de texto de los catálogos que cita y
props `…Texto`; los modelos NO implementan INotifyPropertyChanged) · `Services/I<X>DataSource.cs` +
`Mock<X>DataSource` · `Services/<X>Service.cs` si es documento (métodos `PuedeX` puros para el
`CanExecute` + transiciones que revalidan y lanzan `InvalidOperationException` en español) ·
`ViewModels/<Submodulo>ViewModel.cs` · editores · vistas XAML.

Archivos a **modificar** siempre: `Configuration/DataSourceFactory.cs` (campo cacheado `??=`),
`MainWindow.CrearVistaSubmodulo` (un `case` por clave), `Styles/EditorTemplates.xaml` (un
`DataTemplate` por editor, o la ventana sale vacía) y `Styles/Theme.xaml` (un `Chip…Style` por enum
de estado nuevo).

Arquetipos a copiar: **A** listado CRUD (`RegistroOperacionViewModel` + `RegistroOperacionView`),
**B** tarjetas/detalle (`GestionFlotaViewModel`), **C** reporte de solo lectura (`ProductoViewModel`),
y **contenedor de dos padrones** (`EmpleadosViewModel`, `CuentasPorPagarViewModel`).

## Patrones agregados al construir Inventario, Nómina y Finanzas

- **Documento con líneas**: `Liquidacion` y `FacturaCliente` llevan una `List<…Linea>` dentro. Su
  `Clonar()` hace **copia profunda** de la lista — `MemberwiseClone` la compartiría y editar la copia
  mutaría lo que está en pantalla.
- **Tarifa única con ámbito y vigencia** (`Models/Tarifa.cs`): una sola entidad para lo que se cobra
  al ingenio y lo que se paga por destajo (`AmbitoTarifa`), con `VigenteDesde`/`VigenteHasta`.
  `TarifaService.ObtenerVigente` es la única puerta de consulta; **los documentos copian el monto**
  (`TarifaMonto`), nunca guardan solo el Id: una factura reimpresa no puede cambiar de importe.
- **Facturación sin tocar la máquina de estados de la remesa**: `Remesa.FacturaClienteId` es un campo
  aparte, no un valor nuevo de `EstadoRemesa`. Sirve de control antifacturación doble y deja
  "Recibida" como estado terminal de la operación.
- **Anti-doble-pago en nómina**: `Liquidacion.RemesaIdsIncluidas` registra qué remesas ya se
  liquidaron; al generar se descartan las que estén en otra liquidación no anulada del mismo sujeto.
- **Costo de taller derivado, no capturado**: al confirmar una salida de inventario,
  `InventarioService` recalcula `MantenimientoRegistro.CostoRepuestos` sumando sus salidas
  confirmadas (resuelve el TODO del modelo). El registro de mantenimiento sigue siendo inmutable
  desde la UI.
- **El vale de combustible es la fuente principal de lecturas**: al confirmarlo se descuenta la
  cisterna, se calcula el consumo del período y se adelanta el horómetro/odómetro del activo.
  Anularlo repone los litros pero **no** revierte la lectura (el instrumento marca lo que marca).
  `FlotaService` recibe los vales por constructor opcional y los suma al historial de uso.
- **Registros de solo inserción**: las jornadas de trabajo no se editan ni se borran (`HorarioService`);
  de esas horas sale un pago, y el criterio favorece la futura sincronización offline.
- **Dos padrones de personal sin unificar**: `Empleado` (nómina/taller, entidad EF) y `PersonalCampo`
  (quien firma la remesa, con núcleo C.O.D). Nómina · Empleados los administra por separado en una
  vista conmutable.
- **`MotivoEditorViewModel`**: editor genérico de "decir por qué"; lo reutilizan todas las anulaciones
  nuevas (salida, vale, liquidación, factura de cliente y de proveedor).
- **Umbral configurable** (`appsettings.json` → `Combustible:UmbralAlertaConsumo`, 0.25 = 25 %):
  cuánto puede superar un vale al promedio histórico del activo antes de marcarse con alerta.

## Aviso al socio (base de datos)

- `InventoryItem.StockActual` y `StockMinimo` pasaron de `int` a **`decimal(18,2)`** (hay artículos por
  metro, kilo y litro). La tabla `Inventarios` necesita un `ALTER COLUMN`; ya está reflejado en
  `BD/DbContext.cs`.
- Entidades nuevas pendientes de persistir: `Tarifa`, `SalidaInventario`, `TanqueCombustible`,
  `ValeCombustible`, `RecargaCombustible`, `JornadaTrabajo`, `Liquidacion` (+ `LiquidacionLinea`),
  `ConceptoNomina`, `FacturaCliente` (+ `FacturaClienteLinea`), `Proveedor`, `FacturaProveedor`, y el
  campo `Remesa.FacturaClienteId`.
- Todas las fuentes se registran en `Configuration/DataSourceFactory.cs`: ahí es donde el socio
  enchufa las implementaciones EF sin tocar ViewModels ni vistas.

## Decisiones PROVISIONALES pendientes del socio

Están marcadas en el código con el comentario `// PROVISIONAL:` (búsqueda: `grep -rn "PROVISIONAL"`).
Lo que hace falta para cerrarlas:

1. **Tarifario real** (lo que paga el ingenio por tonelada y lo que se paga a cada núcleo). Los montos
   de `MockTarifaDataSource` son inventados.
2. **Formatos en papel**: vale de combustible, salida de almacén, recarga de cisterna.
3. **Ejemplo de una liquidación ya hecha** y **de una factura al ingenio**, para validar líneas,
   períodos y desglose.
4. **Cuadro de turnos** real (hoy solo Diurno/Nocturno) y qué cuenta como "tiempo muerto".
5. Si el chofer lleva núcleo (C.O.D) y si una persona puede estar en los dos padrones.
6. Qué pasa con las remesas confirmadas **sin pesaje**: hoy no aportan toneladas y no se liquidan.
7. Cliente único vs. maestro de clientes, plazo de crédito real, y el tratamiento de notas de crédito
   y reverso de cobros.
8. Medición de la cisterna (contómetro vs. aforo) y de dónde saldría el kilometraje para la unidad
   `Kilometro` de las tarifas.

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

1. **Flota · Telemetría**, el único submódulo sin construir. Es también lo que permitiría desglosar el
   L/ton por máquina y frente, hoy calculado solo de forma global.
2. **Matriz RBAC real**: `SesionActual.Puede()` sigue devolviendo `true` para cualquier usuario
   autenticado. Todos los comandos ya piden su permiso (`Inventario.ConfirmarSalida`,
   `Finanzas.Facturar`, `Nomina.Cerrar`…), así que conectar la matriz no obliga a tocar ViewModels;
   sin ella, la segregación de funciones que exige el diseño de autorización no es efectiva.
3. **Conexión a base de datos** con el socio (ver "Aviso al socio" más arriba) y resolución de las
   decisiones provisionales.

Permisos ya en uso, por si sirven de base a la matriz: `Remesas.*`, `Flota.*`, `Mantenimiento.*`,
`Seguimiento.AgregarNota`, `Inventario.*` (incluye `OverrideStock`), `Combustible.*`, `Empleados.*`,
`Horarios.*`, `Nomina.*`, `Finanzas.*`, `Tarifas.*`.
