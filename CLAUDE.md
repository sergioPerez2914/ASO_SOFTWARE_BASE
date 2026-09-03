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
| Finanzas | Cuentas por Cobrar · Cuentas por Pagar · Tarifas · Banco | funcionales |

Además hay **cuatro módulos fijados** fuera de esa lista, sin submódulos, que se muestran en el
menú según el permiso: **Inicio**, **Peticiones** (bandeja de solicitudes de cambio),
**Administración** (usuarios con sus permisos, y los datos del núcleo) y **Configuración**.

Los tres primeros van arriba, en `ModuloCatalogo.Fijados`, que es el orden del menú. **Configuración
va aparte** (`ModuloCatalogo.Configuracion`), anclada al pie del sidebar y fuera de su `ScrollViewer`:
no es trabajo del día. La lista que sí las incluye a las cuatro es `TodosLosFijados`, y es la que hay
que usar para permisos y resolución de claves — con `Fijados`, `Ver.Configuracion` no existiría en la
matriz y no habría forma de quitarle la sección a nadie.

**De los 17 submódulos hay 16 construidos del todo; falta solo Flota · Telemetría.**
Banco se construyó el 2026-08-26 (ver su sección más abajo): es un libro interno de entradas y
salidas, sin conexión con ningún banco, y la pregunta de cómo llega el extracto real quedó
resuelta por la vía manual — se cuadra a mano con la marca de conciliado.
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
- `Styles/Tokens.xaml` + `Colors.xaml` + `Theme.xaml` — el sistema visual (ver "El sistema visual"
  más abajo). Iconos = fuente Phosphor empotrada en Assets/Fonts/Phosphor.ttf; los puntos de
  codigo tienen nombre en Controls/Iconos.cs y se usan como {x:Static controls:Iconos.Loque} —
  nunca &#x…; a mano.
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
  Castrol 20W50 y un Mobil 20W50 son productos distintos), sin que la presentación (envase) sea
  parte de esa identidad — ver "Lubricante en litros" más abajo. La Requisición sigue sin
  referenciarlo (solo pide Tipo+Grado); la marca concreta se elige — o se crea al vuelo, botón
  "+ Nuevo" — recién al confirmar la `RecepcionMercancia`, que es cuando se sabe qué trajo el
  proveedor (`RecepcionMercanciaLinea.LubricanteId`, paralelo a `StockCombustibleId` para Diésel).
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
- **Reparto de roles** (revisado el 2026-08-26 con la llegada del Almacenista): el Remesero
  identifica y envía la Requisición; el **Almacenista** la atiende — compara proveedores, arma la
  Orden de Compra en Borrador y registra la Recepción; y **aprobar la Orden de Compra sigue siendo
  exclusivo del AdministradorNucleo**, porque es donde se autoriza el gasto. Quien compra y recibe
  no firma el dinero: por eso el Almacenista tampoco tiene `OrdenCompra.Anular` (deshacer un
  compromiso ya autorizado), aunque sí `OrdenCompra.Eliminar` para sus propios borradores. Permisos
  `Requisicion.*` / `OrdenCompra.*` / `RecepcionMercancia.*` en `Services/Permisos.cs`, repartidos
  en `MatrizPermisos.cs`; ninguno es solicitable, porque cada rol ya tiene su parte del flujo.
- **`Services/ComprasService.cs`** concentra las reglas de los tres documentos, mismo contrato que
  `RemesaService` (`PuedeX` + transición que revalida y lanza `InvalidOperationException`). Pantalla
  `ComprasViewModel` en Inventario · Compras, contenedor de dos padrones (Requisiciones / Órdenes de
  Compra — la Recepción se edita desde la Orden, no es un tercer padrón), mismo arquetipo que
  `CuentasPorPagarViewModel`.
- **Pendiente**: el cotejo a tres vías (Orden de Compra + Recepción + factura del proveedor) antes
  de que la deuda aparezca en Cuentas por Pagar — es lo que de verdad retiraría `RecargaCombustible`.
  `OrdenCompra.Estado.Cerrada` existe pero nada la dispara todavía.

**Lubricante en litros, sin traducir a envases (2026-08-31, ajustado el mismo día):** el lubricante
se llegó a pedir y cotizar por **envase** (Presentación + Unidades, con una tabla fija de litros
por envase — Galón = 3.785 L, Barril = 208 L…), pisando lo pedido en la Requisición y obligando a
traducir mentalmente "cuántos litros necesito" a "cuántos galones son". Al cliente le confundía, y
además "Granel" —que es la ausencia de envase— tenía una entrada en esa tabla como si fuera uno
más. `Lubricante.ExistenciaL` pasó de derivarse (`Unidades × litros`) a capturarse directo, mismo
criterio que `StockCombustible.ExistenciaL`, y su identidad pasó de Marca+Tipo+Grado+Presentación a
solo **Marca+Tipo+Grado** — una sola fila de existencia por producto, sin importar en qué envase
haya llegado cada compra.

Dónde se define la Presentación y los litros que trae el envase se decidió dos veces el mismo día:
primero se probó igualando a Diésel (elegirla recién al **recibir** la mercancía, con
`RecepcionMercanciaLinea.LitrosPorEnvase` como calculadora de "cuántos envases hacen falta"), pero
el cliente la quiso de vuelta en **"Comparar proveedores" (armar la Orden de Compra)** — esa
pantalla funciona como una factura del proveedor, y ahí es donde tiene sentido decidir marca,
clase, presentación y litros por envase. `CotizacionProveedorLinea`/`OrdenCompraLinea` llevan
`Presentacion` (obligatoria para lubricante) y `LitrosPorEnvase` (opcional, solo ayuda visual —
nunca recalcula la cantidad ni el precio); Recepción vuelve a mostrar la Presentación de Lubricante
de solo lectura, heredada de la orden. Diésel no cambió en ningún momento: sigue sin marca, y su
Presentación se sigue eligiendo recién al recibir (nunca tuvo tabla de conversión).

**Una necesidad de la requisición admite varias líneas de compra (2026-08-31):** pedir 150 L de un
grado de lubricante y cubrirlos con 100 L de una marca en barril más 50 L de otra en galón, dentro
de la misma cotización/orden, antes no se podía — "Comparar proveedores" armaba exactamente una
línea por línea de requisición. Ahora la captura es "Agregar línea" (mismo arquetipo que
`RequisicionEditorViewModel`): se elige contra qué necesidad va cada línea
(`CotizacionProveedorLinea.RequisicionLineaIndex`, el índice de `Requisicion.Lineas` — no hizo
falta un Id nuevo) y se pueden agregar varias contra la misma. Un panel de solo lectura muestra
pedido vs. cubierto por necesidad, sin bloquear si no coincide exacto (mismo criterio que
`CantidadPedida` vs. `CantidadRecibida` en Recepción); lo único que sí exige
`ComprasService.CotizacionEstaCompleta` es que ninguna necesidad quede sin una sola línea de
compra. Como dos líneas de compra ahora pueden compartir Marca+Clase+Grado con distinta
presentación, `ConfirmarRecepcion` dejó de buscar el precio del lubricante por esa combinación (era
ambiguo) — `RecepcionMercanciaLinea.PrecioUnitario` es un snapshot copiado 1:1 al armar la recepción
desde la línea de la orden de compra de origen.

Al probarlo, "Cantidad" en el formulario no dejaba claro si eran litros o unidades de repuesto, y
faltaba dónde anotar cuántos envases se compran — solo estaba "litros que trae cada envase". Se
agregó `CotizacionProveedorLinea.Unidades`/`OrdenCompraLinea.Unidades` (envases, opcional, mismo
criterio que `LitrosPorEnvase`: no fuerza nada) y la etiqueta de "Cantidad" ahora dice "(litros)" o
"(unidades)" según el tipo de la necesidad elegida
(`CompararProveedoresEditorViewModel.EtiquetaCantidadLinea`). Si se completan Envases y Litros por
envase y se deja Cantidad vacío, "Agregar línea" calcula la cantidad sola (envases × litros por
envase) — Cantidad se puede seguir escribiendo directo si no se sabe la conversión exacta. El panel
de cobertura ("¿Cuándo dejar de agregar líneas?") se resaltó con una tarjeta propia y un chip de
color por necesidad (`ChipCoberturaStyle`: rojo sin cubrir, ámbar a medias, verde completo) — es el
indicativo de cuándo parar, y se perdía entre el resto de la pantalla.

**Recepción vuelve a separar "Editar" de "Confirmar" (2026-08-31):** el commit `be9df11` las había
fusionado porque la Presentación de Lubricante vivía en Recepción, y si no se cargaba en "Editar",
"Confirmar" la rechazaba sin decir dónde corregirla. Con la Presentación de Lubricante fija desde
la Orden de Compra, ese motivo desapareció — el único caso que sigue pendiente de completarse en
Recepción es la Presentación de Diésel. Se separaron de nuevo: "Editar"
(`RecepcionMercanciaEditorViewModel`) es el único lugar para corregir `CantidadRecibida` y la
Presentación de Diésel; "Confirmar" (`ConfirmarRecepcionEditorViewModel`) ya no muestra ninguna
grilla de líneas, solo pide quién recibió. Si algo queda incompleto, `ComprasService.ConfirmarRecepcion`
lo rechaza con un mensaje que señala ir a "Editar" — para no reintroducir el problema original sin
la ventana fusionada.

La migración `Fase23_LubricanteEnLitros` convirtió la existencia previa con la fórmula de la tabla
fija, pero **no fusionó** las filas que quedaron duplicadas por Marca+Clase+Grado (antes
distinguidas por Presentación, ahora parte del mismo producto) — revisar y fusionar a mano en
Inventario · Combustible · Lubricantes (sumar `ExistenciaL`, decidir qué `CostoUnitario` prevalece)
antes de repartir esta build.

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

## El sistema visual (refinado el 2026-09-02)

Las cuatro reglas que hay que respetar al escribir XAML nuevo:

1. **Color por `DynamicResource`** a una clave de `Colors.xaml`, nunca un hex a mano — es lo único
   que sobrevive al cambio de tema (ver `Services/Tema.cs`). **Forma y tamaño por `StaticResource`**
   a `Tokens.xaml`. La excepción son los diccionarios HERMANOS (`Controles.xaml`,
   `Componentes.xaml`): ahí TODO va por `DynamicResource`, incluidos los tokens, porque un
   `StaticResource` dentro de una `ControlTemplate` no alcanza al hermano y revienta la primera vez
   que se pinta ese control, no al arrancar.
2. **Una clave nueva de color va en LAS DOS paletas.** `ColorsOscuro.xaml` no hereda de
   `Colors.xaml`: la que falte se resuelve contra la clara y sale un parche claro sobre fondo oscuro.
3. **No se anima un brush: se anima la opacidad** de un velo o de un borde superpuesto. Los brushes
   de los diccionarios se congelan al cargarse y mutarlos lanza. Además la opacidad vale igual en
   los dos temas sin escribir la animación dos veces. Duraciones en `DurRapida` (120 ms) y
   `DurNormal` (180 ms); nada pasa de ahí y nada mueve contenido salvo el sidebar.
4. **Sombra solo en lo que flota** (desplegable, calendario, tooltip, menú), por
   `Effect="{DynamicResource SombraFlotante}"` — el efecto entero por clave, no su color: un
   `DropShadowEffect` es `Freezable` y un `DynamicResource` sobre su `Color` no se resuelve desde
   dentro de una plantilla. Una tarjeta de datos NO lleva sombra: se separa por su borde.

Lo que se hizo en el refinamiento, y por qué:

- **Satoshi con sus tres cortes** (Regular, Medium, Bold), no solo Bold. Con un peso único no hay
  jerarquía y el título, el cuerpo y el pie pesan igual. `FwMedium` es **Medium** y no SemiBold
  porque Satoshi no trae SemiBold y WPF lo sintetizaría engordando el Medium.
- **Escala tipográfica con los escalones de arriba separados** (10/11/13/14/16/22/28, más
  `FsHero` 36). El cuerpo subió de 12 a 13: el texto en Regular pesa menos en pantalla que el mismo
  texto en Bold. El sitio lo devuelve el espaciado, más compacto (`TkControl 8,4`, `TkBoton 12,4`).
- **`AltoControl` (28) y `AltoFila` (32) como `MinHeight`**, no solo relleno: con relleno solo, dos
  botones con textos de distinta altura de línea salían de distinto tamaño y la barra quedaba dentada.
- **La barra de herramientas dejó de ser tarjeta** (`ToolbarStyle`, que existía sin usarse). Una
  pantalla de listado tenía dos cajas blancas idénticas donde solo una tiene datos.
- **La tabla dejó la rejilla completa y la cebra**, que son dos mecanismos para lo mismo sumados:
  queda la línea horizontal. La cabecera es un rótulo (`FsCaption`, gris, sobre `SurfaceBrush`) y
  no otra fila más; separar columnas lo hacen ahora el relleno y la alineación. La fila elegida se
  tiñe con `InfoSoftBrush` — antes usaba `NavSelectedBrush`, que significa "módulo activo del menú"
  y se separaba del hover por un 4 % de luminancia.
- **El ítem del sidebar es una pastilla metida hacia dentro**, y esa es la ÚNICA señal de elegido
  junto con el color del rótulo. Antes eran tres a la vez (relleno de borde a borde, barra de 3 px
  y color): con tres señales para un solo hecho, ninguna manda.
- **`BorderStrongBrush`**, un escalón más de borde, para lo que el borde fino no separa (cabecera de
  tabla, tarjeta señalada). Sin él había que recurrir al verde, y el verde debería significar algo:
  por eso el hover de `CardButtonStyle` y el icono de la tarjeta de dashboard dejaron de ser verdes.
- **Los tintes suaves de estado** eran los Tailwind de fábrica en una paleta derivada del CC-20; se
  bajaron de saturación. Los pares de texto no se tocan: su contraste ya estaba medido.

## Operaciones · el boleto del central cierra la remesa (2026-09-02)

Antes la remesa se abría con ~18 campos de una sentada y se cerraba con tres (llegada, bruto,
tara). Las dos puntas cambiaron:

- **El alta solo pregunta el inicio de carga** (`NuevaRemesaEditorViewModel`). Es lo único que se
  sabe en el campo cuando arranca la carga; lo demás se completa con "Editar", que sigue abriendo
  el formulario largo de siempre (`RemesaEditorViewModel`). `RegistroOperacionViewModel.CrearEditor`
  ramifica por `item.Id == 0`. **La normativa se sigue cumpliendo donde toca**:
  `RemesaService.EstaCompleta` no deja confirmar un documento a medias y enumera qué falta —
  ya trataba `default` y `0` como faltante, así que no hizo falta tocarla.
- **Cerrar la remesa exige el boleto que emite el central** (`Models/BoletoCentral.cs`, tipo
  *owned* de `Remesa`, con la migración `Fase27_BoletoCentral` que suma columnas `Boleto_*`
  nullable a `Remesas`). Los campos salen del ejemplar de `docs/boleto.pdf`: número de formulario,
  calidad (ATR, fibra, pureza, trash mineral y vegetal) y los montos que el central reconoce —
  caña entregada, seis descuentos (corte, alza y empuje, transporte, administración, rural,
  investigación) y valor líquido. `RemesaService.RegistrarRecepcion` pasó a ser
  `RegistrarBoleto`; el estado terminal sigue siendo `Recibida` (no se agregó ni se renombró
  ningún estado) y el permiso sigue siendo `Remesas.Recepcion`.
- **El pesaje NO se mudó al boleto.** `PesoBrutoT`/`TaraT`/`PesoNetoT` siguen en `Remesa` porque
  son las toneladas operativas que leen la facturación, la liquidación y el seguimiento; moverlas
  habría tocado cuatro consumidores y migrado las filas existentes sin ganar nada. La fecha del
  boleto tampoco se guarda: es `LlegadaCentral`, y guardarla dos veces daría dos versiones del
  mismo dato.
- **Se comparan las dos cifras, y la diferencia avisa pero no bloquea.** El cálculo de cobro por
  servicio salió de `FacturaClienteService.GenerarBorrador` a
  `TarifaService.CalcularCobroPorServicio`, que ahora usan la factura y el boleto: **dos
  implementaciones darían dos cifras para lo mismo y la comparación dejaría de servir para
  reclamar**. La comparación vive en el ViewModel del editor y no en `RemesaService` a propósito —
  es un dato para reclamarle al central, no una regla del documento. Sin tarifario cargado se
  muestra por qué no se puede comparar y el boleto se registra igual.
- **La calidad es informativa**: el cobro sigue siendo tarifa × toneladas netas. Se captura desde
  ya para que el día que el socio defina la fórmula del ATR los datos estén desde el principio.
- **Finanzas · Cuentas por Cobrar enseña dos padrones** (pestañas, como Cuentas por Pagar): las
  facturas y **las remesas cerradas que esperan factura**, con lo que dice el tarifario, lo que
  dice el boleto y la diferencia. Antes esas remesas solo existían dentro del diálogo de generar y
  como un número suelto en el resumen de cartera, cuando son dinero por cobrar. La consulta ya
  existía (`FacturaClienteService.RemesasFacturables`); lo que faltaba era enseñarla.
- **Facturar por lotes ya funcionaba** —`FacturaCliente` nace con tres líneas por remesa y la
  selección es una lista de casillas—; lo que se agregó es el caso real de "las cinco de la
  semana": filtro por rango de fechas y marcar/desmarcar en bloque. El filtro **esconde filas pero
  no las desmarca**, y una marcada que quedó fuera del rango se avisa en el total en vez de
  descontarse sola.

## Finanzas · Banco: el libro de entradas y salidas (2026-08-26)

**El sistema no se conecta con ningún banco.** Banco es un libro interno de caja: dice cuánto
dinero entró y salió por la aplicación, de qué cuenta, y cuánto queda. Cuadrarlo con el banco de
verdad es lo que hace la marca de conciliado, a mano y contra el extracto en papel.

Antes de esto la aplicación no sabía cuánto dinero había. Cobrar y pagar eran transiciones de
estado dentro del propio documento —un enum y una fecha—, sin monto, sin cuenta y sin registro
propio; el "Saldo neto" del dashboard era `porCobrar - porPagar`, la diferencia entre dos deudas,
que no es dinero que se pueda gastar.

- **`CuentaBancaria`**: banco, caja chica o divisas, cada una con su `SaldoInicial` y su
  `FechaApertura`. **No guarda el saldo**: lo calcula `BancoService.SaldoDeLibro`, por el mismo
  motivo que `OrdenCompra.MontoTotal` se deriva de sus líneas. Una cuenta con movimientos no se
  borra, se desmarca `Activa`.
- **`MovimientoBanco`** (`Estado`: Registrado → Conciliado, rama Anulado): el asiento. `Monto`
  siempre positivo, el signo lo da `Tipo` (Entrada/Salida). Lleva `Fecha` **valor** —el día en que
  el dinero se movió, que la elige el usuario— aparte de la fecha del documento, `Categoria`,
  `Referencia` (cheque/transferencia) y `Origen` + `OrigenId`.

**El asiento se ESCRIBE, no se deriva**, y va contra el instinto que deja el resto del código —la
línea de tiempo de Seguimiento deriva y no guarda. Aquí no se puede por dos razones: un asiento
necesita datos que el documento **no tiene** (a qué cuenta entró, con qué fecha valor, con qué
referencia, si ya apareció en el extracto), y el precedente de dinero es `Tarifa` —los documentos
copian el monto, nunca guardan solo el Id—, porque si mañana alguien corrige la factura el libro
no puede moverse solo. Lo que sí se hereda de Seguimiento es que **el usuario nunca lo teclea dos
veces**: cuando el asiento nace de un documento lo escribe el servicio de dominio.

- **Los tres servicios de dinero reciben `BancoService` por constructor, obligatorio** (no
  opcional: un default `null` dejaría un hueco silencioso por el que un pago no generaría
  asiento), y escriben el movimiento en la misma operación que la transición:
  `FacturaClienteService.RegistrarCobro`, `CuentasPorPagarService.RegistrarPago` y
  `LiquidacionService.Pagar`, los tres con la firma `(documento, AsientoBanco, usuarioId)`. **El
  asiento va primero** porque es el que puede rechazar: fallar después de marcar la factura
  dejaría un documento cobrado sin rastro del dinero.
- **`AsientoBanco`** es un `record` con `(CuentaId, Fecha, Referencia)` — exactamente lo que el
  documento no puede responder. Lo pide **`AsientoBancoEditorViewModel`**, un solo editor para los
  tres casos, en el mismo espíritu que `MotivoEditorViewModel` sirve a todas las anulaciones.
  Sustituyó al `Confirmar` de sí/no que había en esos tres comandos.
- **Anti-doble-asiento**: los tres `Registrar…` rechazan si el documento ya tiene un movimiento no
  anulado (`GetByOrigen`), mismo patrón que `Remesa.FacturaClienteId` contra la doble facturación.
  Si el asiento se anula, el documento puede volver a asentarse.
- **Inmutabilidad**: un movimiento con `Origen != Manual` no se edita ni se borra desde Banco —su
  verdad está en el documento—; solo admite conciliarse y anularse con motivo. El manual sí, y
  solo mientras esté `Registrado`.
- **Transferencia entre cuentas**: escribe **dos** asientos enlazados por `ContraparteId`, una
  salida y una entrada, y anular uno anula el otro. Media transferencia haría aparecer o
  desaparecer dinero. Sin conversión de moneda: transferir entre monedas distintas se rechaza.
- **`SaldoCorrido` (movimiento) y `SaldoActual` (cuenta) son propiedades settable, ignoradas por
  EF, que rellena la pantalla.** No son derivadas como el resto de las `…Texto`: dependen de toda
  la historia de la cuenta, que una fila suelta no conoce. Y como los modelos no implementan
  INotifyPropertyChanged, **hay que llamar a `ItemsView.Refresh()` después de recalcularlas** o la
  tabla se queda con el valor viejo.
- Pantalla `BancoViewModel` en Finanzas · Banco, contenedor de dos padrones (Movimientos /
  Cuentas), mismo arquetipo que `CuentasPorPagarViewModel`. **La cuenta se elige en la pantalla, no
  en el diálogo** (`MovimientosBancoCrudViewModel.CuentaSeleccionada`), con el criterio del frente
  de `HorariosViewModel`; además es lo que hace que el saldo de la cabecera signifique algo. La
  cabecera muestra **saldo de libro · conciliado · sin cuadrar**, y esa diferencia es lo que el
  banco todavía no confirmó (un cheque girado que nadie cobró).
- **Permisos**: `Banco.*` (Crear/Editar/Eliminar del movimiento manual, Conciliar, Anular,
  Transferir) y `CuentasBancarias.*` para el catálogo — se llama así, y no `Cuentas.*`, para no
  chocar con el vocabulario de Cuentas por Cobrar y por Pagar. Ninguno es solicitable y **el
  remesero no tiene ninguno**: no entra a Finanzas. No hizo falta tocar `MatrizPermisos`: el
  universo se arma por reflexión sobre `Permisos`, así que un permiso nuevo entra solo en
  AdministradorNucleo y Desarrollador.
- El dashboard de Finanzas gana el indicador **"Disponible"**, y va el primero: la cifra que se
  puede gastar hoy, para que no se confunda con el "Saldo neto" del final.

### Qué NO entra en el libro, y por qué

Solo entra lo que movió caja de verdad. Son las cuatro preguntas que van a volver:

1. **Vale de combustible, salida de repuestos, costo de taller** — costo devengado, no caja. Ese
   dinero salió al pagar la factura de la compra; contarlo otra vez descuadraría el saldo para
   siempre. `SalidaInventario.CostoTotal` e `InventoryItem.ValorTotal` son valoración de
   existencias, no tesorería.
2. **`RecargaCombustible.CostoTotal`** — sí es dinero que salió, pero es opcional, el proveedor es
   texto libre y no genera `FacturaProveedor`. Como Compras corre en paralelo, derivarlo
   arriesgaría contar el mismo gasto dos veces: queda **fuera de la derivación automática** y, si
   se pagó de contado, se registra como movimiento manual. Se resuelve cuando la recarga genere su
   factura de compra.
3. **Orden de compra aprobada** — compromiso autorizado, no salida. Sigue sin crear deuda en
   Cuentas por Pagar (el cotejo a tres vías está pendiente).
4. **Anticipos de nómina** — se deducen del neto vía `ConceptoNomina{Tipo=Deduccion}`, pero el
   desembolso original nunca se registró en ningún lado. Se captura como salida manual con
   categoría Nómina.

**El `SaldoInicial` de cada cuenta absorbe todo lo anterior a su `FechaApertura`**: las facturas ya
cobradas o pagadas antes de existir el módulo no generan asiento retroactivo. Por eso un
movimiento con fecha anterior a la apertura se rechaza — se contaría dos veces.

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
  `Fase14_OrigenDelEvento` → `Fase15_MarcaYPresentacionLubricante` → `Fase15_Banco` →
  `Fase16_UnidadesLubricante` → `Fase17_CostoUnitarioLubricante` →
  `Fase18_ClaseLubricanteEnRequisicion` → `Fase19_LineasCotizacionProveedor` →
  `Fase20_UnidadesEnCotizacionYOrden` → `Fase21_FacturaProveedorBorradorYLineas` →
  `Fase22_Zafra` → `Fase23_LubricanteEnLitros` → `Fase24_VariasLineasPorNecesidad` →
  `Fase25_EnvasesEnCotizacionYOrden` → `Fase26_DuenoYHectareasFinca` → `Fase27_BoletoCentral`.
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

Cuatro roles (`Models/Rol.cs`), cada uno con un conjunto base en `Services/MatrizPermisos.cs` que el
administrador ajusta por usuario con `PermisoUsuario` (concede o revoca; **revocar gana**).

Los ajustes se editan en **Administración · Usuarios**: al seleccionar un usuario, el panel de al
lado (`PermisosDeUsuarioViewModel`) muestra los 119 permisos agrupados por módulo, marcados según lo
que ya da su rol. La tabla `PermisosUsuario` sigue guardando **solo deltas**: al guardar, un permiso
que vuelve a coincidir con el rol **borra** su ajuste en vez de dejar una fila que repita la matriz.
Dos guardas: no se concede un permiso que quien edita no tiene (era una escalada real: un
administrador podía fabricar un usuario con más alcance que el suyo), y nadie ajusta los suyos
propios.

| Rol | Alcance |
|---|---|
| **Remesero** | 31 permisos: Registro de Operación, Seguimiento, Flota, Mantenimiento, Horarios, Combustible y Configuración. Crea, edita y confirma; salvo la requisición **no anula nada**, no entra a Finanzas (Banco incluido), Nómina·Liquidaciones, Tarifas, Empleados ni a los catálogos maestros |
| **Almacenista** (2026-08-26) | 43 permisos: dueño de Inventario (Repuestos, Combustible, Lubricantes) y del otro extremo de Compras — atiende las requisiciones, cotiza, arma la orden y recibe la mercancía. Anula lo suyo. Ve Flota (Gestión y Mantenimiento) y Finanzas · Cuentas por Pagar de solo lectura, porque una salida se imputa a una máquina y porque el padrón de Proveedores vive ahí. **No aprueba el gasto** (`OrdenCompra.Aprobar`), no fuerza stock (`Inventario.OverrideStock`), y no entra a Operaciones, Nómina ni Administración |
| **AdministradorNucleo** | Todo dentro del núcleo (118 permisos). Lo único que no puede es crear usuarios Desarrollador (`Usuarios.CrearDesarrollador`) |
| **Desarrollador** | Los 119 permisos, y es el único que reparte su propio rol |

- **`Services/Permisos.cs`** es el catálogo de cadenas. Los de navegación llevan prefijo `Ver.` y se
  **derivan de la clave del submódulo**, así que no pueden desincronizarse al renombrar.
- **`Rol` se persiste como ORDINAL** (`Usuarios.Rol int`, sin `HasConversion`): los miembros se
  añaden **siempre al final**, igual que `TipoEventoOperacion`. Añadir un rol es enum + conjunto en
  `MatrizPermisos` + el array `asignables` de `UsuarioEditorViewModel` (escrito a mano, no
  `Enum.GetValues`). **No hace falta migración**: la columna ya es `int`.
- **Los tres `switch` sobre `Rol` no llevan arco de descarte** (`Base`, `Usuario.RolTexto`,
  `UsuarioEditorViewModel.Texto`), y callan CS8524 con `#pragma`. Antes sí lo llevaban, y era una
  trampa doble: `Base` caía en `_ => Todos`, así que un rol nuevo que se olvidara ahí se convertía
  en **superusuario en silencio**, y los otros dos lo mostraban en pantalla llamándose
  "Desarrollador". Ahora olvidarse rompe la compilación.
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
- **El tema cambia en caliente superponiendo un diccionario.** `Styles/ColorsOscuro.xaml` define la
  paleta oscura (43 brushes más el efecto `SombraFlotante`), y `Services/Tema.cs` lo agrega o lo
  quita del final de `Application.Resources.MergedDictionaries`: los diccionarios fusionados se
  recorren en orden inverso, así que el último puesto tapa a `Colors.xaml`. `ColorsOscuro.xaml`
  **no** mergea `Colors.xaml` y las define TODAS — una clave que faltara se resolvería contra la
  paleta clara y saldría un recuadro claro sobre fondo oscuro.

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
- **Y exige que el aprobador tenga el permiso que se está pidiendo** (`EsDeSuDominio`, 2026-08-26).
  Sale de que aprobar autoriza y no ejecuta: el cambio lo hace después a mano quien aprobó, así que
  un aprobador sin ese permiso dejaría la petición aprobada y a nadie capaz de cumplirla. Es lo que
  permite dar `Peticiones.Resolver` al Almacenista sin que acabe votando sobre la anulación de una
  remesa: resuelve las de combustible y no las de Operaciones. Para AdministradorNucleo y
  Desarrollador no cambia nada, que los tienen todos. `PeticionService` recibe `ISesionActual`
  **por constructor obligatorio** — un default `null` dejaría el hueco silencioso justo en la regla
  que acota quién resuelve qué, mismo criterio que `BancoService`.
- **El contador del sidebar cuenta lo que ESE usuario puede atender**, no todo lo pendiente del
  núcleo (`SidebarViewModel.ContarPendientes`): a un almacenista, un contador global le marcaría
  trabajo que no puede quitar de en medio.
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
6. ~~Qué pasa con las remesas confirmadas **sin pesaje**~~ — resuelto el 2026-09-02: no hay remesa
   cerrada sin pesaje, porque cerrarla exige el boleto del central (ver "Operaciones · el boleto"
   más abajo). Una remesa confirmada y sin boleto se queda en Confirmada, a la vista.
7. Cliente único vs. maestro de clientes, plazo de crédito real, y el tratamiento de notas de crédito
   y reverso de cobros.
8. Medición de la cisterna (contómetro vs. aforo) y de dónde saldría el kilometraje para la unidad
   `Kilometro` de las tarifas.
9. **Cómo se rastrean las presentaciones reales de aceite/combustible** (barril, garrafa): hoy
   `StockCombustible` es una existencia general por producto, sin envase — ver "Inventario · Compras"
   más arriba.
10. **Tasa de cambio entre monedas.** `CuentaBancaria.Moneda` es texto libre y Banco **no
    convierte**: no se transfiere entre cuentas de distinta moneda, y `DisponibleTotal` suma sin
    convertir. Mientras todas las cuentas sean en bolívares la cifra es correcta; con una cuenta en
    divisas hace falta decidir la tasa y si se consolida.

## El plan (SIGZ / ASO) — fases

Fase 0 fundación (auth, roles, maestros, shell) · **Fase 1** núcleo operativo (operaciones + flota +
combustible) · Fase 2 taller e inventario · Fase 3 finanzas (CxC/CxP/bancos) · Fase 4 nómina por destajo ·
Fase 5 dashboard gerencial + reportes · Fase 6 (post-MVP) offline, API REST, app móvil.

Roles: **Remesero, Almacenista, AdministradorNucleo, Desarrollador** (los seis anteriores —Admin, Operaciones,
Taller, Finanzas, RRHH, Consulta— se sustituyeron el 2026-08-20; ver "Roles y permisos").
Todo se filtra por la **zafra activa**, todavía pendiente: quedan 11 `// TODO: ZafraId`
(`FacturaCliente`, `FacturaProveedor`, `JornadaTrabajo`, `Liquidacion`, `MovimientoBanco`,
`OrdenCompra`, `RecepcionMercancia`, `Remesa`, `Requisicion`, `SalidaInventario`,
`ValeCombustible`). El mecanismo donde encaja ya existe como interfaz —`Models/IDeZafra.cs`,
con su `int ZafraId`— pero ningún modelo la implementa todavía: falta declarar `: IDeZafra` en
esos 11 y sumarle a `DbContext` el segundo `HasQueryFilter`, igual que `IDeOrganizacion`.

## Diseño del sistema de tickets ("documento de movimiento")

Los tres tickets comparten un mismo patrón: cabecera + líneas, máquina de estados, inmutabilidad tras
confirmar, efectos en una sola transacción, auditoría, `ZafraId`.

1. **Ticket de pesaje** (caña): bruto − tara = neto → toneladas. Efectos: acumula destajo, marca
   "no facturado" (→ FacturaCxC, sin doble facturación), KPIs de producción. **Se construyó como
   el boleto del central** (2026-09-02), y no como documento con su propia máquina de estados:
   hay exactamente uno por remesa y su ciclo de vida es el de ella, así que es un tipo *owned*
   dentro de `Remesa` en vez de una entidad aparte. Ver "Operaciones · el boleto del central".
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

**Estado (2026-08-20, actualizado 2026-09-02):** hechas las capas 1 (RBAC), 3 (segregación:
aprobador ≠ solicitante) y 5 (auditoría de la decisión, en `PeticionCambio`). La capa 2 está en su
versión simple —una petición, un aprobador— sin niveles ni umbrales configurables, y la 4
(override por excepción) sigue sin formalizarse.

El commit `3fb8132` (2026-08-28) ya le exige `ISesionActual` por constructor a los servicios de
dominio y les agregó `_sesion.Puede(...)` a sus transiciones propias (Confirmar, Anular, Aprobar,
Recepción…) — `FacturaClienteService` incluido, que ya no es un ejemplo vigente del hueco. **Lo que
sigue faltando es más puntual:** el CRUD genérico (`CrudViewModelBase.Agregar/Editar/Eliminar`)
escribe directo contra el `IDataSource`, sin pasar por el servicio de dominio ni por
`_sesion.Puede`, así que el alta/edición/borrado básico de casi todos los documentos (Requisición,
Orden de Compra, Recepción, Factura de Proveedor/Cliente, Usuarios, Zafra…) sigue defendido solo
por el `CanExecute` de la UI — únicamente `RemesaService` lo cierra, y solo a medias (no tiene un
método `Crear`, así que el alta de una remesa nueva también pasa por el CRUD genérico). Dos huecos
puntuales más: `SeguimientoService.AgregarNota` no recibe `ISesionActual` y `ZafraService` no
tiene un método `Editar` que valide `Zafra.Editar`.

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
   Con Banco construido, el otro frente natural de Finanzas es el **cotejo a tres vías**: hoy la
   deuda con el proveedor solo existe si alguien teclea la `FacturaProveedor` a mano, así que una
   orden de compra aprobada por 50.000 sigue siendo invisible para el libro.
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
7. **Fusionar a mano los `Lubricante` duplicados** que dejó la migración `Fase23_LubricanteEnLitros`
   (ver "Lubricante en litros" más arriba): filas con el mismo Marca+Clase+Grado que antes se
   distinguían por Presentación. Un vistazo rápido a la base local de desarrollo (2026-08-31)
   encontró al menos dos pares en ese estado.

El catálogo completo de permisos está en `Services/Permisos.cs` (98 acciones, más 21 de navegación
derivadas del catálogo de módulos = 119) y el reparto por rol en
`Services/MatrizPermisos.cs`.
