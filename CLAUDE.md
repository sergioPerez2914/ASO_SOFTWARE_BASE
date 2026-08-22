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
- **Datos detrás de interfaces** (`Services/I<X>DataSource.cs`), resueltas en
  `Configuration/DataSourceFactory.cs`. La UI y los ViewModels no conocen EF Core.
- **Ya no hay mocks** (2026-08-20). Las 19 clases `Mock…DataSource`, `MockAuthService` y el flag
  `UseMock` se eliminaron: el único camino es SQL Server. El modo "sin BD" no sobrevivía al
  aislamiento por núcleo, porque los mocks no pasan por el filtro global de EF y habrían dejado
  ver los datos de todas las organizaciones.
- **Regla de oro**: toda regla de negocio vive en servicios de dominio, nunca en eventos de botones.

## Estructura de módulos (reset de 2026-08-17)

Se eliminaron las pantallas anteriores (inventario, empleados, nómina/tarifas) y se rehízo la
presentación. Ahora son **cinco módulos con submódulos**; al entrar a un módulo se ve su dashboard
resumen y se despliega su lista de submódulos en el menú lateral.

| Módulo | Submódulos | Estado |
|---|---|---|
| Operaciones | Registro de Operación · Seguimiento · Fincas | funcionales |
| Flota | Gestión de Flota · Mantenimiento · **Telemetría** | funcionales, salvo Telemetría (pendiente) |
| Inventario | Repuestos · Combustible · Producto | funcionales |
| Nómina | Liquidaciones · Empleados · Gestión de Horarios | funcionales |
| Finanzas | Cuentas por Cobrar · Cuentas por Pagar · Tarifas | funcionales |

Además hay **tres módulos fijados** fuera de esa lista, sin submódulos, que se muestran en el menú
según el permiso: **Inicio**, **Peticiones** (bandeja de solicitudes de cambio) y **Administración**
(usuarios con sus permisos, y los datos del núcleo).

**Los 14 submódulos están construidos; el único que falta es Flota · Telemetría.**
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

Archivos a **crear**: `Models/<X>.cs` (con `Clonar()`, snapshots de texto de los catálogos que cita,
props `…Texto`, y **`: IDeOrganizacion` con su `OrganizacionId`** si la entidad pertenece a un núcleo;
los modelos NO implementan INotifyPropertyChanged) · `Services/I<X>DataSource.cs` +
`BD/Sql<X>DataSource.cs` · `Services/<X>Service.cs` si es documento (métodos `PuedeX` puros para el
`CanExecute` + transiciones que revalidan y lanzan `InvalidOperationException` en español) ·
`ViewModels/<Submodulo>ViewModel.cs` · editores · vistas XAML.

La fuente de datos es una línea, no una clase: hereda de `SqlCrudDataSource<T, TId>` y solo declara
las consultas que sean suyas. Si la entidad es una raíz con hijos que se guardan juntos, hereda de
`SqlAgregadoDataSource<T, TId>` en su lugar. El ViewModel de la pantalla hereda de
`PantallaViewModelBase`, o de `PantallaCrudViewModel<T, TId>` si además es el listado CRUD de un
maestro; las dos ramas cumplen `IPantalla`, que es lo que el shell enruta.

Archivos a **modificar** siempre: `Configuration/DataSourceFactory.cs` (campo cacheado `??=`),
`BD/DbContext.cs` (`DbSet` + configuración), la tabla `Pantallas` de `MainWindow.xaml.cs` (una línea
por clave de submódulo), `Styles/PantallaTemplates.xaml` (un `DataTemplate` por pantalla, o el área
de contenido muestra el nombre del tipo del ViewModel), `Styles/EditorTemplates.xaml` (un
`DataTemplate` por editor, o la ventana sale vacía) y `Styles/Theme.xaml` (un `Chip…Style` por enum
de estado nuevo, con `BasedOn="{StaticResource ChipBaseStyle}"`). Después, `dotnet ef migrations add`.

El permiso de navegación **no se declara**: `Submodulo.Permiso` lo deriva de la clave
(`Ver.<Clave>`), así que basta con dar de alta el submódulo en `ModuloCatalogo`. Lo que sí hay que
hacer es sumarlo al rol que corresponda en `Services/MatrizPermisos.cs`, o no lo verá nadie salvo
el Desarrollador.

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

## Persistencia

Las entidades de dominio persisten en **SQL Server vía EF Core Migrations**. Desde el 2026-08-20 no
hay mocks: `Configuration/DataSourceFactory.cs` devuelve siempre la implementación `Sql…`.

- **Migraciones** en `ASO.Desktop/Migrations/`, por fases:
  `Baseline00_EmpleadoInventario` → `Fase1_CatalogosSimples` → `Fase2_CatalogosConRelaciones` →
  `Fase3_DocumentosPlanos` → `Fase4_ColeccionesAnidadas` → `Fase5_EventoOperacion` →
  `FixStockActualStockMinimoDecimal` → `Fase6_OrganizacionYSeguridad` → `Fase7_NucleoUnico`.
- **La cadena de conexión vive solo en `appsettings.local.json`** (por máquina, en `.gitignore`).
- **No hay claves foráneas reales** en las tablas planas: las relaciones son `int` sueltos y la
  integridad es de la aplicación, con snapshots de texto (`…Nombre`, `…Codigo`) en cada documento.

## Núcleos: un solo núcleo por instalación (2026-08-21)

**Una instalación atiende a un solo núcleo.** El núcleo es la `Organizacion`: la empresa donde está
instalado el sistema, y a la vez el ámbito de aislamiento. Lo que sí es de uno a muchos es
**núcleo → fincas**, y cada finca tiene sus lotes y tablones.

Hasta el 2026-08-21 convivían dos entidades llamadas "núcleo": `Organizacion` y un catálogo `Nucleo`
de núcleos **de productores**. Se fusionaron, porque dentro de un núcleo todas las referencias
apuntan a ese mismo núcleo:

- **`Nucleo` ya no existe** (entidad, fuente de datos, CRUD, editor y tabla). El **C.O.D** del CAM
  vive ahora en **`Organizacion.CodigoCam`**, aparte de `Organizacion.Codigo`, que es la etiqueta
  corta de uso interno.
- **Ningún formulario pregunta por el núcleo.** Ni la remesa (que pedía tres), ni el personal de
  campo, ni la liquidación: el C.O.D se estampa desde `Ambito.ExigirCodigoCam()`.
- **Los tres servicios siguen vivos.** La remesa genera corte + alza y empuje + transporte porque
  cada uno tiene su tarifa; lo que desapareció es la pregunta de a qué núcleo pertenece cada uno.
- **Los campos de texto se conservan** (`Remesa.Nucleo*Codigo`, `PersonalCampo.NucleoCodigo`,
  `JornadaTrabajo.NucleoCodigo`, `FacturaClienteLinea.NucleoCodigo`): un documento guarda lo que
  decía el papel, así que un C.O.D renombrado no reescribe las remesas viejas.
- **El multi-núcleo salió de la interfaz**: sin botón "Cambiar núcleo", sin padrón de Organizaciones
  y sin elegir núcleo al crear un usuario. El núcleo nace en `Views/PrimerArranqueView`, que ahora
  pide también el C.O.D, y sus datos se corrigen después en **Administración · Núcleo**
  (`DatosNucleoViewModel`, permiso `Nucleo.Editar`) — que es una ficha de una sola fila, no un
  padrón. Hace falta porque el C.O.D es lo que se estampa en cada documento: sin sitio donde
  corregirlo, un código mal cargado se arrastraría para siempre.

**El aislamiento por organización sigue intacto**, y es deliberado: es la barrera que impide que dos
instalaciones que compartan base se vean entre sí.

- **`Models/IDeOrganizacion.cs`** marca las entidades sujetas al ámbito: 23 en total, las 20
  operativas más `Usuario`, `PermisoUsuario` y `PeticionCambio`.
- **`BD/DbContext.cs` hace todo el trabajo, en dos sitios:**
  - `AplicarFiltroDeOrganizacion` recorre el modelo y pone un `HasQueryFilter` a cada
    `IDeOrganizacion`. Es **fail-closed**: sin ámbito fijado no se ve nada, en vez de verse todo.
  - `SaveChanges()` estampa `OrganizacionId` en toda fila nueva. Un solo sitio: olvidarlo en una
    fuente de datos crearía filas que después ninguna consulta devolvería.
- **`Services/Ambito.cs`** guarda el núcleo activo (la entidad entera, no solo su Id, porque de
  ahí sale el C.O.D que estampan los documentos). La fija `SesionActual.IniciarSesion` a partir del
  usuario y **no cambia mientras dure la sesión**.
- **`IgnoreQueryFilters()` se usa en dos sitios y solo dos**: el login (al autenticar todavía no hay
  ámbito) y la lectura de los ajustes de permisos del usuario que entra. Ambos en
  `BD/SqlUsuarioDataSource.cs`.
- **`Organizaciones` no lleva filtro** — es la tabla que define el ámbito. Ya no se administra
  desde la interfaz: la fila nace en el primer arranque.
- **Añadir una entidad al ámbito** es implementar `IDeOrganizacion`: el filtro y el estampado la
  recogen solos.

## Roles y permisos (2026-08-20)

Tres roles (`Models/Rol.cs`), cada uno con un conjunto base en `Services/MatrizPermisos.cs` que el
administrador ajusta por usuario con `PermisoUsuario` (concede o revoca; **revocar gana**).

Los ajustes se editan en **Administración · Usuarios**: al seleccionar un usuario, el panel de al
lado (`PermisosDeUsuarioViewModel`) muestra los 87 permisos agrupados por módulo, marcados según lo
que ya da su rol. La tabla `PermisosUsuario` sigue guardando **solo deltas**: al guardar, un permiso
que vuelve a coincidir con el rol **borra** su ajuste en vez de dejar una fila que repita la matriz.
Dos guardas: no se concede un permiso que quien edita no tiene (era una escalada real: un
administrador podía fabricar un usuario con más alcance que el suyo), y nadie ajusta los suyos
propios.

| Rol | Alcance |
|---|---|
| **Remesero** | 24 permisos: Registro de Operación, Seguimiento, Flota, Mantenimiento, Horarios y Combustible. Crea, edita y confirma; **no anula nada**, no entra a Finanzas, Nómina·Liquidaciones, Tarifas, Empleados ni a los catálogos maestros |
| **AdministradorNucleo** | Todo dentro del núcleo (86 permisos). Lo único que no puede es crear usuarios Desarrollador (`Usuarios.CrearDesarrollador`) |
| **Desarrollador** | Los 87 permisos, y es el único que reparte su propio rol |

- **`Services/Permisos.cs`** es el catálogo de cadenas. Los de navegación llevan prefijo `Ver.` y se
  **derivan de la clave del submódulo**, así que no pueden desincronizarse al renombrar.
- **`SesionActual`** calcula el conjunto efectivo **una vez al entrar** y lo cachea. No es
  optimización prematura: `CommandManager.RequerySuggested` dispara los `CanExecute` de toda la
  ventana ante cualquier entrada del usuario. Consecuencia: **un cambio de rol o de permisos se
  aplica al volver a iniciar sesión**, no en caliente.
- **Cuatro caminos llevan a una pantalla** y los cuatro filtran con la misma regla
  (`Services/NavegacionPermitida.cs`): sidebar, lanzador de Inicio, tarjetas del dashboard y la
  guarda de `MainWindow.CrearVistaSubmodulo`.
- **Colisiones de permisos corregidas** al conectar la matriz: `Finanzas.*` la compartían las
  facturas al ingenio, las de proveedor y el padrón de proveedores (ahora `FacturasCliente.*`,
  `FacturasProveedor.*` y `Proveedores.*`); `Empleados.*` cubría los dos padrones (ahora
  `PersonalCampo.*`); e `Inventario.Eliminar` significaba a la vez borrar un artículo del catálogo y
  borrar una salida en borrador (ahora `Inventario.EliminarSalida`).
- **Contraseñas**: PBKDF2-SHA256 con salt por usuario y 210 000 iteraciones
  (`Services/Passwords.cs`), sin dependencias nuevas. No hay usuarios sembrados ni contraseñas por
  defecto: en la primera ejecución contra una base sin usuarios, `Views/PrimerArranqueView` pide el
  nombre del núcleo, su C.O.D y crea el usuario Desarrollador.

## Peticiones de cambio

Cuando al remesero le falta un permiso **de los sensibles** (`MatrizPermisos.Solicitables`), el botón
no queda gris: pide el motivo con el `MotivoEditorViewModel` de siempre y deja una `PeticionCambio`
en la bandeja del administrador (módulo fijado **Peticiones**).

- **Aprobar autoriza, no ejecuta.** La petición no guarda la mutación; el administrador hace el
  cambio en la pantalla que corresponde, con sus validaciones y su máquina de estados intactas.
  Un motor que reprodujera mutaciones guardadas se saltaría justo esas comprobaciones.
- **`PeticionService.Resolver` exige aprobador ≠ solicitante**, que es la segregación de funciones
  del diseño de autorización.
- Hoy son solicitables `Remesas.Anular`, `Remesas.Recepcion`, `Flota.Crear`, `Flota.Editar`,
  `Combustible.Anular` y `Combustible.Recargar`: **exactamente** las acciones sensibles que aparecen
  en las pantallas que el remesero ve. Al ampliar lo que ve un rol, ampliar la lista **y** cablear el
  comando con `SolicitudesDeCambio`, o la regla queda muerta.

## Decisiones PROVISIONALES pendientes del socio

Están marcadas en el código con el comentario `// PROVISIONAL:` (búsqueda: `grep -rn "PROVISIONAL"`).
Lo que hace falta para cerrarlas:

1. **Tarifario real** (lo que paga el ingenio por tonelada y lo que se paga a cada núcleo). Sin
   mocks, la tabla `Tarifas` arranca vacía: hay que cargar el tarifario real antes de facturar o
   liquidar nada.
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

Roles: **Remesero, AdministradorNucleo, Desarrollador** (los seis anteriores —Admin, Operaciones,
Taller, Finanzas, RRHH, Consulta— se sustituyeron el 2026-08-20; ver "Roles y permisos").
Todo se filtra por la **zafra activa**, todavía pendiente: quedan 7 `// TODO: ZafraId`. El mecanismo
donde encaja ya existe — sería un `IDeZafra` con su segundo `HasQueryFilter`, igual que
`IDeOrganizacion`.

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

**Estado (2026-08-20):** hechas las capas 1 (RBAC), 3 (segregación: aprobador ≠ solicitante) y 5
(auditoría de la decisión, en `PeticionCambio`). La capa 2 está en su versión simple —una petición,
un aprobador— sin niveles ni umbrales configurables, y la 4 (override por excepción) sigue sin
formalizarse. **Falta lo importante: la comprobación sigue siendo solo de UI.** Los servicios de
dominio no consultan `ISesionActual`, así que la defensa en profundidad que pide este diseño todavía
no existe; ver `FacturaClienteService.cs` (`// PROVISIONAL:`).

## Próximo paso sugerido

1. **Rotar la contraseña de `sa`.** La cadena de conexión ya salió de `appsettings.json` (ahora es
   LocalDB con un `.mdf` en `App_Data`), pero `ASO123` sigue en el historial de un repositorio
   público: quitarla del HEAD no la revoca.
2. **Llevar la comprobación de permisos a los servicios de dominio.** Hoy toda la autorización vive
   en el `CanExecute` de los comandos: quien llame a un servicio desde otro sitio se la salta.
3. **Flota · Telemetría**, el único submódulo sin construir. Es también lo que permitiría desglosar el
   L/ton por máquina y frente, hoy calculado solo de forma global.
4. **Resolver las decisiones provisionales** con el socio (tarifario, formatos, turnos): ahora que la
   BD está conectada y no hay mocks, cada supuesto sin confirmar se convierte en datos reales mal
   cargados.

El catálogo completo de permisos está en `Services/Permisos.cs` (87 en uso) y el reparto por rol en
`Services/MatrizPermisos.cs`.
