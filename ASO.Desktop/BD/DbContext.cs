using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ASO.Desktop.Models;
using ASO.Desktop.Configuration;

namespace ASO.Desktop.BD;

public class AsoDbContext : DbContext
{
    public DbSet<Empleado> Empleados { get; set; }
    public DbSet<InventoryItem> Inventarios { get; set; }

    // Fase 1 (catálogos simples)
    public DbSet<Nucleo> Nucleos { get; set; }
    public DbSet<Proveedor> Proveedores { get; set; }
    public DbSet<ConceptoNomina> ConceptosNomina { get; set; }
    public DbSet<TanqueCombustible> TanquesCombustible { get; set; }

    // Fase 2 (catálogos con relaciones livianas)
    public DbSet<PersonalCampo> PersonalCampo { get; set; }
    public DbSet<ActivoFlota> ActivosFlota { get; set; }
    public DbSet<ReglaMantenimiento> ReglasMantenimiento { get; set; }
    public DbSet<Tarifa> Tarifas { get; set; }

    // Fase 3 (documentos planos)
    public DbSet<Remesa> Remesas { get; set; }
    public DbSet<JornadaTrabajo> Jornadas { get; set; }
    public DbSet<SalidaInventario> SalidasInventario { get; set; }
    public DbSet<MantenimientoRegistro> MantenimientoRegistros { get; set; }
    public DbSet<ValeCombustible> ValesCombustible { get; set; }
    public DbSet<RecargaCombustible> RecargasCombustible { get; set; }
    public DbSet<FacturaProveedor> FacturasProveedor { get; set; }

    // Fase 4 (documentos con colección anidada)
    public DbSet<Finca> Fincas { get; set; }
    public DbSet<Liquidacion> Liquidaciones { get; set; }
    public DbSet<FacturaCliente> FacturasCliente { get; set; }

    // Fase 5 (evento derivado/adaptador)
    public DbSet<EventoOperacion> EventosOperacion { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // La cadena de conexión vive en appsettings.json / appsettings.local.json,
            // no en el código. Cada máquina configura la suya sin tocar el repo.
            optionsBuilder.UseSqlServer(AppConfig.ConnectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuraciones adicionales y restricciones para la tabla Empleados
        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Cedula).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Cargo).IsRequired().HasMaxLength(100);

            // Ignoramos esta propiedad en SQL porque es calculada solo para la UI de WPF
            entity.Ignore(e => e.EstadoTexto);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.ToTable("Inventarios");

            // Definimos el Código como Clave Primaria (Varchar)
            entity.HasKey(i => i.Codigo);
            entity.Property(i => i.Codigo).HasMaxLength(30);

            entity.Property(i => i.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(i => i.Categoria).IsRequired().HasMaxLength(100);
            entity.Property(i => i.Unidad).IsRequired().HasMaxLength(20);
            entity.Property(i => i.Ubicacion).IsRequired().HasMaxLength(50);
            // TODO socio BD: StockActual/StockMinimo pasaron de int a decimal para admitir
            // artículos por metro, kilo o litro. La tabla existente necesita un
            // ALTER COLUMN ... decimal(18,2) antes de usar esta configuración contra SQL.
            entity.Property(i => i.StockActual).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(i => i.StockMinimo).HasColumnType("decimal(18,2)").IsRequired();

            // Especificamos precisión decimal para dinero (18 enteros, 2 decimales)
            entity.Property(i => i.CostoUnitario).HasColumnType("decimal(18,2)").IsRequired();

            // Ignoramos miembros calculados en C# para que no busquen columnas físicas en SQL
            entity.Ignore(i => i.Id);          // alias de Codigo para el CRUD genérico (IEntidad<string>)
            entity.Ignore(i => i.ValorTotal);
            entity.Ignore(i => i.Estado);
            entity.Ignore(i => i.EstadoTexto);
            entity.Ignore(i => i.StockTexto);
        });

        // --- Fase 1 (catálogos simples) ---

        modelBuilder.Entity<Nucleo>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Codigo).IsRequired().HasMaxLength(30);
            entity.Property(n => n.Nombre).IsRequired().HasMaxLength(150);

            entity.Ignore(n => n.Etiqueta);
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(p => p.Rif).HasMaxLength(30);
            entity.Property(p => p.Telefono).HasMaxLength(30);
            entity.Property(p => p.Notas).HasMaxLength(500);

            entity.Ignore(p => p.EstadoTexto);
            entity.Ignore(p => p.Etiqueta);
        });

        modelBuilder.Entity<ConceptoNomina>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nombre).IsRequired().HasMaxLength(150);

            entity.Ignore(c => c.TipoTexto);
            entity.Ignore(c => c.EstadoTexto);
        });

        modelBuilder.Entity<TanqueCombustible>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(t => t.CapacidadL).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(t => t.ExistenciaL).HasColumnType("decimal(18,2)").IsRequired();

            entity.Ignore(t => t.PorcentajeLleno);
            entity.Ignore(t => t.ExistenciaTexto);
            entity.Ignore(t => t.PorcentajeTexto);
        });

        // --- Fase 2 (catálogos con relaciones livianas) ---

        modelBuilder.Entity<PersonalCampo>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(p => p.Cedula).IsRequired().HasMaxLength(20);
            entity.Property(p => p.NucleoCodigo).HasMaxLength(30);

            entity.Ignore(p => p.RolTexto);
            entity.Ignore(p => p.EstadoTexto);
        });

        modelBuilder.Entity<ActivoFlota>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Codigo).IsRequired().HasMaxLength(30);
            entity.Property(a => a.Marca).HasMaxLength(100);
            entity.Property(a => a.Modelo).HasMaxLength(100);
            entity.Property(a => a.Placa).HasMaxLength(20);
            entity.Property(a => a.Descripcion).HasMaxLength(200);
            entity.Property(a => a.Notas).HasMaxLength(500);
            entity.Property(a => a.HorometroHoras).HasColumnType("decimal(18,2)");
            entity.Property(a => a.OdometroKm).HasColumnType("decimal(18,2)");

            entity.Ignore(a => a.EsTransporte);
            entity.Ignore(a => a.TipoTexto);
            entity.Ignore(a => a.EstadoTexto);
            entity.Ignore(a => a.Etiqueta);
            entity.Ignore(a => a.UsoTexto);
            entity.Ignore(a => a.Glifo);
        });

        modelBuilder.Entity<ReglaMantenimiento>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Revision).IsRequired().HasMaxLength(150);
            entity.Property(r => r.IntervaloHoras).HasColumnType("decimal(18,2)");

            entity.Ignore(r => r.IntervaloTexto);
        });

        modelBuilder.Entity<Tarifa>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Concepto).IsRequired().HasMaxLength(150);
            entity.Property(t => t.MontoPorUnidad).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(t => t.Notas).HasMaxLength(500);

            entity.Ignore(t => t.ServicioTexto);
            entity.Ignore(t => t.AmbitoTexto);
            entity.Ignore(t => t.UnidadTexto);
            entity.Ignore(t => t.UnidadCorta);
            entity.Ignore(t => t.MontoTexto);
            entity.Ignore(t => t.VigenciaTexto);
            entity.Ignore(t => t.EstadoTexto);
        });

        // --- Fase 3 (documentos planos) ---

        modelBuilder.Entity<Remesa>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.FincaCodigoCam).HasMaxLength(30);
            entity.Property(r => r.FincaNombre).HasMaxLength(150);
            entity.Property(r => r.LoteNombre).HasMaxLength(100);
            entity.Property(r => r.TablonNombre).HasMaxLength(100);
            entity.Property(r => r.OperadorNombre).HasMaxLength(150);
            entity.Property(r => r.OperadorNucleoCodigo).HasMaxLength(30);
            entity.Property(r => r.TractoristaNombre).HasMaxLength(150);
            entity.Property(r => r.TractoristaNucleoCodigo).HasMaxLength(30);
            entity.Property(r => r.ChoferNombre).HasMaxLength(150);
            entity.Property(r => r.VehiculoPlaca).HasMaxLength(20);
            entity.Property(r => r.RemeseroNombre).HasMaxLength(150);
            entity.Property(r => r.NucleoCorteCodigo).HasMaxLength(30);
            entity.Property(r => r.NucleoAlzaEmpujeCodigo).HasMaxLength(30);
            entity.Property(r => r.NucleoTransporteCodigo).HasMaxLength(30);
            entity.Property(r => r.MotivoAnulacion).HasMaxLength(500);
            entity.Property(r => r.PesoBrutoT).HasColumnType("decimal(18,2)");
            entity.Property(r => r.TaraT).HasColumnType("decimal(18,2)");

            entity.Ignore(r => r.PesoNetoT);
            entity.Ignore(r => r.Facturada);
            entity.Ignore(r => r.EstadoTexto);
            entity.Ignore(r => r.TipoCosechaTexto);
            entity.Ignore(r => r.UbicacionTexto);
            entity.Ignore(r => r.FacturadaTexto);
        });

        modelBuilder.Entity<JornadaTrabajo>(entity =>
        {
            entity.HasKey(j => j.Id);
            entity.Property(j => j.PersonaNombre).HasMaxLength(150);
            entity.Property(j => j.CargoORol).HasMaxLength(100);
            entity.Property(j => j.NucleoCodigo).HasMaxLength(30);
            entity.Property(j => j.Observacion).HasMaxLength(500);

            entity.Ignore(j => j.EstaAbierta);
            entity.Ignore(j => j.HorasTrabajadas);
            entity.Ignore(j => j.HorasTexto);
            entity.Ignore(j => j.TurnoTexto);
            entity.Ignore(j => j.TipoPersonalTexto);
            entity.Ignore(j => j.EstadoTexto);
            entity.Ignore(j => j.EntradaTexto);
            entity.Ignore(j => j.SalidaTexto);
        });

        modelBuilder.Entity<SalidaInventario>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.ArticuloCodigo).HasMaxLength(30);
            entity.Property(s => s.ArticuloNombre).HasMaxLength(150);
            entity.Property(s => s.Unidad).HasMaxLength(20);
            entity.Property(s => s.ActivoEtiqueta).HasMaxLength(150);
            entity.Property(s => s.Motivo).HasMaxLength(500);
            entity.Property(s => s.MotivoAnulacion).HasMaxLength(500);
            entity.Property(s => s.Cantidad).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(s => s.CostoUnitario).HasColumnType("decimal(18,2)").IsRequired();

            entity.Ignore(s => s.CostoTotal);
            entity.Ignore(s => s.EstadoTexto);
            entity.Ignore(s => s.DestinoTexto);
            entity.Ignore(s => s.CantidadTexto);
            entity.Ignore(s => s.CostoTotalTexto);
            entity.Ignore(s => s.MantenimientoTexto);
        });

        modelBuilder.Entity<MantenimientoRegistro>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.ActivoCodigo).HasMaxLength(30);
            entity.Property(m => m.ActivoEtiqueta).HasMaxLength(150);
            entity.Property(m => m.Descripcion).HasMaxLength(500);
            entity.Property(m => m.RepuestosUsados).HasMaxLength(1000);
            entity.Property(m => m.RealizadoPor).HasMaxLength(150);
            entity.Property(m => m.LecturaUso).HasColumnType("decimal(18,2)");
            entity.Property(m => m.CostoRepuestos).HasColumnType("decimal(18,2)");
            entity.Property(m => m.CostoManoObra).HasColumnType("decimal(18,2)");

            entity.Ignore(m => m.CostoTotal);
            entity.Ignore(m => m.TipoTexto);
            entity.Ignore(m => m.LecturaTexto);
            entity.Ignore(m => m.CostoTotalTexto);
            entity.Ignore(m => m.RemesaTexto);
        });

        modelBuilder.Entity<ValeCombustible>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.TanqueNombre).HasMaxLength(150);
            entity.Property(v => v.ActivoCodigo).HasMaxLength(30);
            entity.Property(v => v.ActivoEtiqueta).HasMaxLength(150);
            entity.Property(v => v.ResponsableNombre).HasMaxLength(150);
            entity.Property(v => v.MotivoAnulacion).HasMaxLength(500);
            entity.Property(v => v.Notas).HasMaxLength(500);
            entity.Property(v => v.Litros).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(v => v.Lectura).HasColumnType("decimal(18,2)");
            entity.Property(v => v.ConsumoPorUnidad).HasColumnType("decimal(18,2)");
            entity.Property(v => v.PromedioHistorico).HasColumnType("decimal(18,2)");

            entity.Ignore(v => v.EstadoTexto);
            entity.Ignore(v => v.UnidadLectura);
            entity.Ignore(v => v.LitrosTexto);
            entity.Ignore(v => v.LecturaTexto);
            entity.Ignore(v => v.ConsumoTexto);
            entity.Ignore(v => v.PromedioTexto);
        });

        modelBuilder.Entity<RecargaCombustible>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.TanqueNombre).HasMaxLength(150);
            entity.Property(r => r.ProveedorNombre).HasMaxLength(150);
            entity.Property(r => r.Notas).HasMaxLength(500);
            entity.Property(r => r.Litros).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(r => r.CostoTotal).HasColumnType("decimal(18,2)");

            entity.Ignore(r => r.LitrosTexto);
            entity.Ignore(r => r.CostoTexto);
            entity.Ignore(r => r.CostoPorLitro);
        });

        modelBuilder.Entity<FacturaProveedor>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.NumeroDocumento).HasMaxLength(50);
            entity.Property(f => f.ProveedorNombre).HasMaxLength(150);
            entity.Property(f => f.Descripcion).HasMaxLength(500);
            entity.Property(f => f.MotivoAnulacion).HasMaxLength(500);
            entity.Property(f => f.Monto).HasColumnType("decimal(18,2)").IsRequired();

            entity.Ignore(f => f.EstaVencida);
            entity.Ignore(f => f.DiasParaVencer);
            entity.Ignore(f => f.EstadoTexto);
            entity.Ignore(f => f.MontoTexto);
            entity.Ignore(f => f.VencimientoTexto);
            entity.Ignore(f => f.PlazoTexto);
        });

        // --- Fase 4 (documentos con colección anidada) ---
        //
        // Cada método de Sql*DataSource abre un AsoDbContext nuevo (estilo desconectado del
        // resto del proyecto), así que un Update ingenuo de la cabecera NO borra los hijos
        // (Lote/Tablon, LiquidacionLinea, FacturaClienteLinea) que se quitaron del lado
        // cliente: EF no tiene con qué comparar sin un grafo rastreado. Por eso los
        // Sql*DataSource de esta fase cargan el grafo con Include/ThenInclude, hacen
        // RemoveRange de la colección vieja y recién ahí asignan la nueva. No "simplificar"
        // esos Update a la única línea que usan el resto de las entidades.

        modelBuilder.Entity<Finca>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.CodigoCam).IsRequired().HasMaxLength(30);
            entity.Property(f => f.Nombre).IsRequired().HasMaxLength(150);
            entity.Ignore(f => f.Etiqueta);

            entity.OwnsMany(f => f.Lotes, lote =>
            {
                lote.WithOwner().HasForeignKey("FincaId");
                lote.HasKey(l => l.Id);
                lote.Property(l => l.Nombre).IsRequired().HasMaxLength(100);

                lote.OwnsMany(l => l.Tablones, tablon =>
                {
                    tablon.WithOwner().HasForeignKey("LoteId");
                    tablon.HasKey(t => t.Id);
                    tablon.Property(t => t.Nombre).IsRequired().HasMaxLength(100);
                });
            });
        });

        modelBuilder.Entity<Liquidacion>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.SujetoCodigo).HasMaxLength(30);
            entity.Property(l => l.SujetoNombre).HasMaxLength(150);
            entity.Property(l => l.MotivoAnulacion).HasMaxLength(500);

            entity.Property(l => l.RemesaIdsIncluidas)
                .HasConversion(
                    v => string.Join(",", v),
                    v => v.Length == 0
                        ? new List<int>()
                        : v.Split(',', System.StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList())
                .Metadata.SetValueComparer(new ValueComparer<List<int>>(
                    (a, b) => a!.SequenceEqual(b!),
                    v => v.Aggregate(0, (h, x) => System.HashCode.Combine(h, x)),
                    v => v.ToList()));

            entity.Ignore(l => l.TotalDevengos);
            entity.Ignore(l => l.TotalDeducciones);
            entity.Ignore(l => l.Neto);
            entity.Ignore(l => l.NetoTexto);
            entity.Ignore(l => l.PeriodoTexto);
            entity.Ignore(l => l.SujetoTipoTexto);
            entity.Ignore(l => l.SujetoTexto);
            entity.Ignore(l => l.EstadoTexto);

            entity.OwnsMany(l => l.Lineas, linea =>
            {
                linea.WithOwner().HasForeignKey("LiquidacionId");
                linea.Property<int>("Id");
                linea.HasKey("Id");
                linea.Property(x => x.Concepto).HasMaxLength(150);
                linea.Property(x => x.UnidadTexto).HasMaxLength(20);
                linea.Property(x => x.Cantidad).HasColumnType("decimal(18,2)");
                linea.Property(x => x.TarifaMonto).HasColumnType("decimal(18,2)");
                linea.Property(x => x.Monto).HasColumnType("decimal(18,2)");

                linea.Ignore(x => x.OrigenTexto);
                linea.Ignore(x => x.CantidadTexto);
                linea.Ignore(x => x.TarifaTexto);
                linea.Ignore(x => x.MontoTexto);
            });
        });

        modelBuilder.Entity<FacturaCliente>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.ClienteNombre).HasMaxLength(150);
            entity.Property(f => f.MotivoAnulacion).HasMaxLength(500);

            entity.Ignore(f => f.NumeroTexto);
            entity.Ignore(f => f.Total);
            entity.Ignore(f => f.Toneladas);
            entity.Ignore(f => f.TotalTexto);
            entity.Ignore(f => f.EstaVencida);
            entity.Ignore(f => f.EstadoTexto);
            entity.Ignore(f => f.EmisionTexto);
            entity.Ignore(f => f.VencimientoTexto);
            entity.Ignore(f => f.RemesasTexto);

            entity.OwnsMany(f => f.Lineas, linea =>
            {
                linea.WithOwner().HasForeignKey("FacturaClienteId");
                linea.Property<int>("Id");
                linea.HasKey("Id");
                linea.Property(x => x.FincaNombre).HasMaxLength(150);
                linea.Property(x => x.NucleoCodigo).HasMaxLength(30);
                linea.Property(x => x.Toneladas).HasColumnType("decimal(18,2)");
                linea.Property(x => x.TarifaMonto).HasColumnType("decimal(18,2)");

                linea.Ignore(x => x.Monto);
                linea.Ignore(x => x.ServicioTexto);
                linea.Ignore(x => x.RemesaTexto);
                linea.Ignore(x => x.ToneladasTexto);
                linea.Ignore(x => x.MontoTexto);
            });
        });

        // --- Fase 5 (evento derivado/adaptador) ---
        //
        // La tabla solo recibe Add() para CambioTurno/Mantenimiento/Nota: los demás valores del
        // enum los sintetiza SeguimientoService en vivo a partir de la Remesa (con Id=0) y nunca
        // llegan a esta fuente de datos. No sembrar datos para esos tipos derivados.

        modelBuilder.Entity<EventoOperacion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Autor).HasMaxLength(150);

            entity.Ignore(e => e.EtiquetaTipo);
            entity.Ignore(e => e.Glifo);
            entity.Ignore(e => e.FechaHoraTexto);
        });
    }
}