using System.Collections.Generic;
using System.Linq;

namespace ASO.Desktop.Navigation;

public sealed record Submodulo(string Clave, string Nombre, string Descripcion, string Icono)
{
    /// <summary>
    /// Permiso de navegación, DERIVADO de la clave en vez de declarado aparte: así no puede
    /// desincronizarse al renombrar un submódulo. Ej.: "Ver.Operaciones.Registro".
    /// </summary>
    public string Permiso => Services.Permisos.Ver(Clave);
}

public sealed record Modulo(
    string Clave,
    string Nombre,
    string Descripcion,
    string Icono,
    IReadOnlyList<string> Indicadores,
    IReadOnlyList<Submodulo> Submodulos)
{
    /// <summary>Permiso propio. Solo decide por sí mismo en los módulos SIN submódulos
    /// (Inicio, Peticiones); en el resto la visibilidad la deciden sus submódulos.</summary>
    public string Permiso => Services.Permisos.Ver(Clave);
}

/// <summary>
/// Fuente única de la estructura de navegación: cinco módulos, cada uno con sus submódulos.
/// El sidebar, el lanzador de inicio, los dashboards y el enrutado de MainWindow leen de aquí,
/// así que agregar o renombrar un submódulo se hace en un solo lugar.
/// Los iconos son glifos de la fuente Segoe MDL2 Assets.
/// </summary>
public static class ModuloCatalogo
{
    public static Modulo Inicio { get; } = new(
        "Inicio",
        "Inicio",
        "Punto de entrada a los cinco módulos del sistema.",
        "",
        [],
        []);

    /// <summary>
    /// Bandeja de peticiones de cambio. Es un pseudo-módulo fijado en el menú, igual que
    /// <see cref="Inicio"/>: no cuelga de ninguno de los cinco porque las peticiones
    /// atraviesan todos (una anulación de remesa y una de factura caen en la misma bandeja).
    /// </summary>
    public static Modulo Peticiones { get; } = new(
        "Peticiones",
        "Peticiones",
        "Solicitudes de cambio pendientes de aprobación.",
        "",
        [],
        []);

    public static IReadOnlyList<Modulo> Modulos { get; } =
    [
        new Modulo(
            "Operaciones",
            "Operaciones",
            "Registro y seguimiento de la operación diaria de cosecha y transporte.",
            "",
            ["Toneladas del día", "Operaciones abiertas", "Frentes activos", "Tiempo muerto"],
            [
                new Submodulo("Operaciones.Registro", "Registro de Operación",
                    "Remesas de caña: finca, núcleos, carga y pesaje en el central.", ""),
                new Submodulo("Operaciones.Seguimiento", "Seguimiento",
                    "Estado y avance de las operaciones en curso.", ""),
                new Submodulo("Operaciones.FincasNucleos", "Fincas y Núcleos",
                    "Catálogo de fincas, lotes/tablones y núcleos de productores.", "")
            ]),

        new Modulo(
            "Flota",
            "Flota",
            "Máquinas y vehículos: disponibilidad, mantenimiento y datos de campo.",
            "",
            ["Unidades activas", "En taller", "Disponibilidad", "Mantenimientos vencidos"],
            [
                new Submodulo("Flota.Gestion", "Gestión de Flota",
                    "Ficha e historial de cada máquina y vehículo.", ""),
                new Submodulo("Flota.Mantenimiento", "Mantenimiento",
                    "Registro de mantenimientos y revisiones recomendadas por uso.", ""),
                new Submodulo("Flota.Telemetria", "Telemetría",
                    "Horómetros, odómetros y lecturas de campo.", "")
            ]),

        new Modulo(
            "Inventario",
            "Inventario",
            "Existencias de repuestos, combustible y producto.",
            "",
            ["Artículos", "Bajo mínimo", "Agotados", "Valor de inventario"],
            [
                new Submodulo("Inventario.Repuestos", "Repuestos",
                    "Stock de repuestos y consumibles de taller.", ""),
                new Submodulo("Inventario.Combustible", "Combustible",
                    "Existencia en cisterna, despachos y rendimiento.", ""),
                new Submodulo("Inventario.Producto", "Producto",
                    "Caña cosechada y entregada al ingenio.", "")
            ]),

        new Modulo(
            "Nomina",
            "Nómina",
            "Personal, jornadas y liquidación por destajo.",
            "",
            ["Empleados activos", "Liquidaciones pendientes", "Horas del período", "Monto del período"],
            [
                new Submodulo("Nomina.Liquidaciones", "Liquidaciones",
                    "Cálculo y cierre de nómina por período.", ""),
                new Submodulo("Nomina.Empleados", "Empleados",
                    "Padrón de personal, cargos y datos de contratación.", ""),
                new Submodulo("Nomina.Horarios", "Gestión de Horarios",
                    "Turnos, jornadas y asistencia.", "")
            ]),

        new Modulo(
            "Finanzas",
            "Finanzas",
            "Cobranza, pagos a proveedores y tarifas del servicio.",
            "",
            ["Por cobrar", "Por pagar", "Vencido", "Saldo neto"],
            [
                new Submodulo("Finanzas.CuentasPorCobrar", "Cuentas por Cobrar",
                    "Facturación al ingenio y seguimiento de cobros.", ""),
                new Submodulo("Finanzas.CuentasPorPagar", "Cuentas por Pagar",
                    "Obligaciones con proveedores y su vencimiento.", ""),
                new Submodulo("Finanzas.Tarifas", "Tarifas",
                    "Precios por tonelada, kilómetro y servicio.", "")
            ])
    ];

    /// <summary>
    /// Administración del sistema: núcleos, usuarios y permisos. Fijado como
    /// <see cref="Peticiones"/>, y por el mismo motivo: no pertenece a ninguno de los cinco
    /// módulos del negocio.
    /// </summary>
    public static Modulo Administracion { get; } = new(
        "Administracion",
        "Administración",
        "Núcleos, usuarios, roles y permisos.",
        "",
        [],
        []);

    /// <summary>Todos los módulos fijados fuera de la lista de cinco, en orden de menú.</summary>
    public static IReadOnlyList<Modulo> Fijados { get; } = [Inicio, Peticiones, Administracion];

    public static Modulo? BuscarModulo(string clave)
        => Fijados.FirstOrDefault(m => m.Clave == clave)
           ?? Modulos.FirstOrDefault(m => m.Clave == clave);

    /// <summary>Resuelve un submódulo por su clave completa ("Modulo.Submodulo").</summary>
    public static Submodulo? BuscarSubmodulo(string clave)
        => Modulos.SelectMany(m => m.Submodulos).FirstOrDefault(s => s.Clave == clave);
}
