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
| Inventario | Repuestos · Combustible · Producto · **Compras** | funcionales |
| Nómina | Liquidaciones · Empleados · Gestión de Horarios | funcionales |
| Finanzas | Cuentas por Cobrar · Cuentas por Pagar · Tarifas · **Banco** | funcionales, salvo Banco (pendiente) |

Además hay **cuatro módulos fijados** fuera de esa lista, sin submódulos, que se muestran en el
menú según el permiso: **Inicio**, **Peticiones** (bandeja de solicitudes de cambio),
**Administración** (usuarios con sus permisos, y los datos del núcleo) y **Configuración**.

Los tres primeros van arriba, en `ModuloCatalogo.Fijados`, que es el orden del menú. **Configuración
va aparte** (`ModuloCatalogo.Configuracion`), anclada al pie del sidebar y fuera de su `ScrollViewer`:
no es trabajo del día. La lista que sí las incluye a las cuatro es `TodosLosFijados`, y es la que hay
que usar para permisos y resolución de claves — con `Fijados`, `Ver.Configuracion` no existiría en la
matriz y no habría forma de quitarle la sección a nadie.

**De los 17 submódulos hay 15 construidos del todo; faltan Flota · Telemetría y Finanzas · Banco.**
Banco está dado de alta en el catálogo pero cae en el marcador de posición: mostrará el estado de
la cuenta según lo cobrado y pagado en la aplicación, y queda por decidir cómo llega el dato real
del banco (importar el extracto, teclearlo o una interfaz contratada de banca empresas).
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
- `Views/ConfiguracionView` — apariencia, la propia cuenta y las preferencias de la máquina
  (ver "Configuración y preferencias").
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
- **La línea de tiempo de Seguimiento tiene tres orígenes**: eventos derivados de la propia
  remesa, eventos derivados de los documentos que la citan (su factura, las liquidaciones que la
  computaron, las peticiones de cambio sobre ella) y eventos almacenados. Derivar es siempre la
  primera opción: no necesita tabla ni escritura y no se puede desincronizar. Solo se almacena lo
  que no deja huella en ningún documento — cambios de turno, mantenimientos, notas, ediciones del
  borrador y la liberación al anular una factura, que borra el campo que la delataba.
  - `EventoOperacion.OrigenId` guarda el Id del documento que lo originó, para abrir su ficha
    desde la línea de tiempo. No lleva el tipo: `Tipo` ya lo dice.
  - **Los miembros de `TipoEventoOperacion` se añaden SIEMPRE al final**: se persisten como `int`
    y declarar uno en medio reinterpretaría las filas guardadas. El orden de lectura lo da
    `OrdenCicloVida`, no la declaración. Los `switch` del modelo no llevan arco de descarte a
    propósito, para que olvidarse de mapear un tipo nuevo dé un aviso del compilador en vez de
    disfrazarlo de otro evento; por eso silencian CS8524 con un `#pragma`.
  - Combustible y repuestos **no** pueden llegar a la timeline: `ValeCombustible` y
    `SalidaInventario` se vinculan al activo, no a la remesa.
- **La jornada de campo se ficha contra una remesa** (`JornadaTrabajo.RemesaId`, obligatorio solo
  para `TipoPersonal.Campo`): al abrirla y al cerrarla, `HorarioService` publica un evento
  `CambioTurno` en la línea de tiempo de ese frente. El frente **se elige en la pantalla, no en el
  diálogo** (`HorariosViewModel.FrenteSeleccionado`): se ficha a varias personas seguidas contra la
  misma remesa, así que elegirlo en cada alta sería teclear lo mismo diez veces. Ese selector acota
  además la tabla al frente elegido, y el editor solo lo muestra como texto. Es el segundo módulo que escribe en el
  seguimiento sin tocar Operaciones, con el mismo patrón que `MantenimientoService`. La remesa
  debe estar en `Borrador` o `Confirmada` al abrir; al cerrar **no** se revalida, porque anular la
  remesa mientras alguien trabajaba no borra las horas que hay que pagarle.
- **Dos padrones de personal sin unificar**: `Empleado` (nómina/taller, entidad EF) y `PersonalCampo`
  (quien firma la remesa, con núcleo C.O.D). Nómina · Empleados los administra por separado en una
  vista conmutable.
- **`MotivoEditorViewModel`**: editor genérico de "decir por qué"; lo reutilizan todas las anulaciones
  nuevas (salida, vale, liquidación, factura de cliente y de proveedor).
- **Umbral configurable** (`appsettings.json` → `Combustible:UmbralAlertaConsumo`, 0.25 = 25 %):
  cuánto puede superar un vale al promedio histórico del activo antes de marcarse con alerta.

## Inventario · Compras: requisición, orden de compra y recepción (2026-08-25)

Reemplaza el atajo de `RecargaCombustible` (sumar litros a mano, sin aprobación ni proveedor real)
por el proceso real: alguien identifica cuánto hace falta → se comparan precios de proveedor → se
aprueba una orden de compra → el proveedor entrega → se recibe → **ahí** se refleja en existencia.
**Los tres pasos están construidos**, incluida la Recepción: es ella, no la Orden de Compra, la que
mueve `StockCombustible`/`InventoryItem.StockActual`. `RecargaCombustible` **sigue viva y en uso**
en paralelo a propósito — se retira cuando el cotejo a tres vías (ver más abajo) esté listo, no antes.

- **`Requisicion`** (`Estado`: Borrador → Enviada → Atendida | Anulada): quien está en campo/taller
  dice qué hace falta. Documento con líneas (`RequisicionLinea`), sin monto — eso lo decide la
  cotización más adelante. Cada línea es `TipoInsumo.Combustible` (dice `TipoCombustibleSolicitado`:
  Diesel o Lubricante; si es Lubricante, Tipo —Mineral/Sintético/Semi-sintético— y Grado de
  viscosidad —15W40, 20W50, etc.— por dos desplegables cerrados, **sin catálogo de stock que
  elegir**, igual que Diésel nunca referencia un `StockCombustible` en esta etapa) o
  `TipoInsumo.Repuesto` (artículo del catálogo).
- **`Lubricante` (2026-08-25), catálogo propio en Inventario · Combustible** (pestaña "Lubricantes"
  dentro de `CombustibleViewModel`, no un submódulo nuevo): reemplaza el hack anterior que creaba
  un `InventoryItem` con `Categoria = "Lubricantes"` y código `LUB-{tipo}-{grado}` por cada
  combinación — se veía como "Repuesto" y sus características quedaban aplanadas en el `Nombre`.
  Se identifica por **Marca + Tipo + Grado**: cada marca es su propia fila de existencia (un
  Castrol 20W50 y un Mobil 20W50 son productos distintos). La Requisición sigue sin referenciarlo
  (solo pide Tipo+Grado); la marca concreta se elige — o se crea al vuelo, botón "+ Nuevo" — recién
  al confirmar la `RecepcionMercancia`, que es cuando se sabe qué trajo el proveedor
  (`RecepcionMercanciaLinea.LubricanteId`, paralelo a `StockCombustibleId` para Diésel).
  `ComprasService.ConfirmarRecepcion`/`AnularRecepcion` tienen ahora tres ramas de stock
  (Repuesto → `InventoryItem`, Diésel → `StockCombustible`, Lubricante → `Lubricante`) en vez de
  dos. Permiso `Lubricantes.*`: maestro simple, mismo trato que `Proveedores.*` — el remesero ve la
  pestaña (tiene `Ver.Combustible`) pero no administra el catálogo.
- **`CotizacionProveedor`**: por cada requisición Enviada, el administrador captura una fila por
  proveedor consultado (monto total, no por línea). Documento plano, sin estado propio — es apoyo
  para decidir, no un documento con ciclo de vida. Desde el diálogo de comparar proveedores se puede
  dar de alta un proveedor nuevo al vuelo (botón "+ Nuevo", reutiliza `ProveedorEditorViewModel`)
  sin salir a Finanzas · Cuentas por Pagar.
- **`OrdenCompra`** (`Estado`: Borrador → Aprobada → Cerrada | Anulada): se arma desde una
  requisición Enviada más la cotización ganadora (`ComprasService.CrearDesdeRequisicion`), copiando
  las líneas y congelando `MontoCotizado` = el monto de esa cotización. Con una sola línea el
  precio unitario se calcula solo (`cotizado ÷ cantidad`); con varias, el sistema no reparte un
  total entre ellas — quedan en 0 para completarlas a mano, con el monto cotizado visible aparte
  como referencia. "Aprobada" es a la vez la aprobación del gasto y la emisión al proveedor.
- **`RecepcionMercancia`** (`Estado`: Borrador → Confirmada, rama Anulada): "la carta de
  recibimiento". Se arma desde una Orden Aprobada sin recepción activa
  (`OrdenCompra.RecepcionMercanciaId`, una a la vez), copiando las líneas con `CantidadPedida` de
  referencia y `CantidadRecibida` prellenada igual a la pedida, a corregir si hubo faltante o
  sobrante real — es lo REALMENTE recibido, no lo pedido, lo que mueve el stock al confirmar. Una
  línea de combustible elige aquí a qué `StockCombustible` concreto del catálogo se suma (la orden
  solo decía diésel/lubricante, no un producto). Confirmar es inmutable; anular revierte el stock
  con motivo (permisos `RecepcionMercancia.*` en `Services/Permisos.cs`).
- **Reparto de roles**: Remesero identifica y envía la Requisición; comparar proveedores, armar y
  aprobar la Orden de Compra, y registrar la Recepción es exclusivo de AdministradorNucleo
  (permisos `Requisicion.*` / `OrdenCompra.*` / `RecepcionMercancia.*` en `Services/Permisos.cs`,
  repartidos en `MatrizPermisos.cs`; ninguno es solicitable, porque cada rol ya tiene su parte del
  flujo).
- **`Services/ComprasService.cs`** concentra las reglas de los tres documentos, mismo contrato que
  `RemesaService` (`PuedeX` + transición que revalida y lanza `InvalidOperationException`). Pantalla
  `ComprasViewModel` en Inventario · Compras, contenedor de dos padrones (Requisiciones / Órdenes de
  Compra — la Recepción se edita desde la Orden, no es un tercer padrón), mismo arquetipo que
  `CuentasPorPagarViewModel`.
- **Pendiente**: el cotejo a tres vías (Orden de Compra + Recepción + factura del proveedor) antes
  de que la deuda aparezca en Cuentas por Pagar — es lo que de verdad retiraría `RecargaCombustible`.
  `OrdenCompra.Estado.Cerrada` existe pero nada la dispara todavía.

**Corrección "cisterna" no existe (2026-08-24):** el modelo original (`TanqueCombustible`) asumía
que el combustible/aceite se vacía en una cisterna física elegible de una lista. La empresa real
guarda el aceite en la presentación en que llega (barril, garrafa), no en un envase común — la
palabra no le hizo sentido al cliente. Se renombró a **`StockCombustible`**: existencia general por
**producto** (p. ej. "Diesel", "Aceite hidráulico"), no por envase, mismo vocabulario que
`InventoryItem.StockActual`. `ValeCombustible` y `RecargaCombustible` (que sí gestionan una cisterna
real, quedan intactos en su función) usan `StockCombustibleId`/`StockCombustibleNombre`; las líneas
de `Requisicion`/`OrdenCompra`, en cambio, **ya no referencian stock en absoluto** — piden tipo y
grado, como se explica arriba. **Sigue pendiente con el socio cómo se van a rastrear las
presentaciones reales** (por barril/garrafa, con su propio conteo) — `StockCombustible` es un
número general mientras tanto, ver `Models/StockCombustible.cs`.

## Persistencia

Las entidades de dominio persisten en **SQL Server vía EF Core Migrations**. Desde el 2026-08-20 no
hay mocks: `Configuration/DataSourceFactory.cs` devuelve siempre la implementación `Sql…`.

- **Migraciones** en `ASO.Desktop/Migrations/`, por fases:
  `Baseline00_EmpleadoInventario` → `Fase1_CatalogosSimples` → `Fase2_CatalogosConRelaciones` →
  `Fase3_DocumentosPlanos` → `Fase4_ColeccionesAnidadas` → `Fase5_EventoOperacion` →
  `FixStockActualStockMinimoDecimal` → `Fase6_OrganizacionYSeguridad` → `Fase7_NucleoUnico` →
  `Fase8_RequisicionYOrdenCompra` → `Fase9_RenombrarCisternaAStock` →
  `Fase10_RequisicionCombustibleYUnidad` → `Fase11_MontoCotizadoYLineasOrdenCompra` →
  `Fase12_RecepcionMercancia` → `Fase13_JornadaEnFrente` → `Fase14_Lubricantes` →
  `Fase14_OrigenDelEvento`.
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
lado (`PermisosDeUsuarioViewModel`) muestra los 90 permisos agrupados por módulo, marcados según lo
que ya da su rol. La tabla `PermisosUsuario` sigue guardando **solo deltas**: al guardar, un permiso
que vuelve a coincidir con el rol **borra** su ajuste en vez de dejar una fila que repita la matriz.
Dos guardas: no se concede un permiso que quien edita no tiene (era una escalada real: un
administrador podía fabricar un usuario con más alcance que el suyo), y nadie ajusta los suyos
propios.

| Rol | Alcance |
|---|---|
| **Remesero** | 25 permisos: Registro de Operación, Seguimiento, Flota, Mantenimiento, Horarios, Combustible y Configuración. Crea, edita y confirma; **no anula nada**, no entra a Finanzas, Nómina·Liquidaciones, Tarifas, Empleados ni a los catálogos maestros |
| **AdministradorNucleo** | Todo dentro del núcleo (89 permisos). Lo único que no puede es crear usuarios Desarrollador (`Usuarios.CrearDesarrollador`) |
| **Desarrollador** | Los 90 permisos, y es el único que reparte su propio rol |

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

## Configuración y preferencias (2026-08-22)

Sección propia, anclada **al pie del sidebar** y fuera de su scroll, con tres pestañas de tres
alcances distintos — que es la razón de que estén separadas y de que no vivan en Administración
(allí se administra a los demás; aquí cada quien ajusta lo suyo, y el remesero entra en la segunda
pero no en la primera):

| Pestaña | Alcance | Qué |
|---|---|---|
| **Apariencia** | esta máquina | Tema claro/oscuro y escala de la interfaz (100/110/125 %). Sin botón de guardar: se aplican al instante |
| **Mi cuenta** | quien está dentro | Ficha de solo lectura, **cambiar la propia contraseña** y recordar el usuario en el login |
| **Aplicación** | esta máquina, para todos | Abrir en la última sección · **umbral de alerta de consumo**, detrás de `Configuracion.Preferencias` |

- **Las preferencias NO van a la base de datos.** Viven en `%AppData%\ASO\ajustes.json`
  (`Models/AjustesApp.cs` + `Configuration/AjustesStoreJson.cs`, fachada `Services/Ajustes.cs`).
  Son de quien está sentado delante, no del núcleo: meterlas en la base habría significado una
  entidad `IDeOrganizacion` más y una migración, para algo que ni siquiera es dato del negocio.
  `Leer()` **nunca lanza**: archivo ausente, corrupto o sin permisos caen en los valores por
  defecto — corre en el arranque, antes del login, y ahí no hay dónde informar de nada.
- **El tema cambia en caliente superponiendo un diccionario.** `Styles/ColorsOscuro.xaml` define
  las 26 claves de brush de la paleta oscura, y `Services/Tema.cs` lo agrega o lo quita del final de
  `Application.Resources.MergedDictionaries`: los diccionarios fusionados se recorren en orden
  inverso, así que el último puesto tapa a `Colors.xaml`. `ColorsOscuro.xaml` **no** mergea
  `Colors.xaml` y define las 26 completas — una clave que faltara se resolvería contra la paleta
  clara y saldría un recuadro blanco sobre fondo oscuro.

  **Todas las referencias a color del XAML son `DynamicResource`, y las nuevas también tienen que
  serlo.** No es preferencia de estilo, es lo único que funciona, y costó un intento fallido
  averiguarlo: la primera versión reasignaba el `.Color` de cada brush de `Colors.xaml` y **no
  cambiaba nada**, porque **WPF congela los `Freezable` de un `ResourceDictionary` compilado al
  cargarlo** — `IsFrozen` ya es `true` en el arranque y mutarlos lanza. Con `StaticResource` el
  problema es el mismo por otra vía: resuelve una vez, se queda con el objeto y no se entera de que
  se superpuso otra paleta.

  De ahí dos reglas al escribir XAML: **un color va siempre por `DynamicResource` a una clave de la
  paleta**, y **no se escriben hex a mano** (los seis que había en `Theme.xaml` se promovieron a
  `CardHoverBrush`, `DangerHoverBrush`, `DangerPressedBrush`, `ToggleHoverBrush` y
  `FilaAlternaBrush`). Los `Style` y los converters siguen con `StaticResource`: no cambian con el
  tema.
- **`Styles/Controles.xaml` reestiliza los controles de WPF**, y es lo que evita las cajas blancas.
  La plantilla de fábrica (Aero2) pinta sus fondos con brushes escritos dentro del tema del
  framework: no son `SystemColors` ni nada sustituible por clave, así que sobre el tema oscuro
  quedaban cajas blancas con texto claro encima. Poner `Background` en el `Style` no basta para
  `ComboBox`, `CheckBox`, `DatePicker` ni `ScrollBar` — hay que reemplazar la plantilla, y eso es
  lo que hay ahí, junto con `DataGridColumnHeader`/`Cell`/`Row`, `ListBoxItem`, `Menu`, `ToolTip`,
  `Calendar` y el `TextBox`/`PasswordBox` sin estilo.
  - Los estilos **implícitos no alcanzan a un control que ya trae `Style=` con clave**: por eso
    `FormTextBoxStyle`, `FormPasswordBoxStyle`, `FormComboBoxStyle` y `FormDatePickerStyle` llevan
    `BasedOn="{StaticResource {x:Type …}}"`. Sin eso se quedan con la plantilla de fábrica.
  - **Reestilar un control es hacerse cargo de lo que su plantilla resolvía sola.** El `ComboBox`
    lo dejó claro: al sustituir su plantilla, la caja cerrada pasó a mostrar el `ToString()` del
    objeto — en un `record` de C#, el volcado de todas sus propiedades
    (`OpcionTema { Valor = Oscuro, Texto = Oscuro }`), en los ~32 combos de la app. La causa es que
    `SelectionBoxItemTemplate` **se queda en null cuando la lista usa `DisplayMemberPath`** (medido,
    y también en el ComboBox de fábrica), así que ni `TemplateBinding`, ni `Binding` con
    `RelativeSource`, ni `ContentSource="SelectionBoxItem"` traen nada que pintar. La plantilla
    resuelve ahora el `DisplayMemberPath` por su cuenta con `Controls/SeleccionATexto.cs`, y deja el
    `ContentPresenter` solo para los combos que traen `ItemTemplate` propio (Salida de inventario).
  - **El calendario no se estiliza con un estilo implícito.** `CalendarItem` crea los botones de día
    enlazando su `Style` a `Calendar.CalendarDayButtonStyle`; ese enlace deja un **valor local** en
    la propiedad `Style` aunque el origen sea nulo, y un `Style` local a nulo **impide** que WPF
    busque el implícito — se queda con la plantilla de fábrica y su gris `#333333` a fuego. Hay que
    dárselo por `Calendar.CalendarDayButtonStyle` / `CalendarButtonStyle`, que es el enganche que
    WPF sí respeta.
- **Los botones llevan estilo implícito propio** (`Button` en `Controles.xaml`). Sin él usaban la
  plantilla de fábrica, cuyo hover es un azul claro escrito a fuego en el tema de WPF: en modo
  oscuro el botón se volvía una pastilla clara. Solo Flota se libraba, porque define su propio
  estilo. El realce va en un **velo translúcido** encima (`HoverOverlayBrush` /
  `PressedOverlayBrush`) y no cambiando el `Background`, porque cada botón trae el suyo (azul
  primario, transparente, rojo de emergencia) y sustituirlo lo perdería; el velo oscurece en el
  tema claro y aclara en el oscuro.
- **La escala es un `LayoutTransform` sobre la raíz de `MainWindow`**, no un `FontSize` global: los
  estilos traen tamaños propios y subir solo la fuente descuadraría iconos, chips y columnas.
- **Cambiar la propia contraseña exige la actual** aunque la sesión ya esté abierta: un equipo
  desatendido es justo el caso en el que alguien cambiaría la clave ajena. Reutiliza `Passwords` y
  el mismo largo mínimo que el alta de usuarios.
- **El umbral de combustible tiene permiso propio** (`Configuracion.Preferencias`, que el remesero
  no tiene) porque decide cuándo un vale se marca con alerta: quien pueda subirlo puede apagarse sus
  propias alertas. La precedencia se resuelve en un solo sitio,
  `Ajustes.UmbralAlertaConsumoEfectivo` = ajuste de la máquina, y si no, `appsettings.json`.
- **`Sincronizar` del sidebar recorre `Items` *más* el ítem del pie.** Configuración está fuera de
  `Items` por vivir anclada abajo; si el bucle mirara solo esa lista, entrar en ella no apagaría el
  módulo anterior y salir no la apagaría a ella.

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
9. **Cómo se rastrean las presentaciones reales de aceite/combustible** (barril, garrafa): hoy
   `StockCombustible` es una existencia general por producto, sin envase — ver "Inventario · Compras"
   más arriba.

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
3. **Inventario · Compras ya tiene sus tres pasos** (Requisición, Orden de Compra, Recepción de
   mercancía). Queda el cotejo a tres vías (OC + Recepción + factura del proveedor) antes de que la
   deuda aparezca en Cuentas por Pagar — recién ahí se retira `RecargaCombustible`. Ver "Inventario ·
   Compras" más arriba.
4. **Flota · Telemetría**, el único submódulo sin construir. Es también lo que permitiría desglosar el
   L/ton por máquina y frente, hoy calculado solo de forma global.
5. **Resolver las decisiones provisionales** con el socio (tarifario, formatos, turnos, presentaciones
   de aceite): ahora que la BD está conectada y no hay mocks, cada supuesto sin confirmar se
   convierte en datos reales mal cargados.
6. **Reparar las filas huérfanas que dejó el bug de `OrganizacionId` en `UPDATE`** (corregido en
   código el 2026-08-23, `AsoDbContext.EstamparOrganizacion`): un editor que no conservaba el
   `OrganizacionId` original ponía 0 al guardar una edición, y el filtro fail-closed dejaba esa fila
   fuera de toda consulta — visible en BD, invisible en pantalla. La corrección solo evita casos
   nuevos; no repara los que ya quedaron en 0 antes de esa fecha. Un vistazo rápido a la base local
   de desarrollo (2026-08-25) encontró 3 `Fincas` y 3 `Empleados` en ese estado — revisar si hay más
   antes de repartir esta build.

El catálogo completo de permisos está en `Services/Permisos.cs` (90 en uso) y el reparto por rol en
`Services/MatrizPermisos.cs`.
