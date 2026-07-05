using Microsoft.EntityFrameworkCore;
using ASO.Desktop.Models;

namespace ASO.Desktop.BD;

public class AsoDbContext : DbContext
{
    public DbSet<Empleado> Empleados { get; set; }
    public DbSet<InventoryItem> Inventarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Reemplaza con tu cadena de conexión real de SQL Server
            optionsBuilder.UseSqlServer("Server=DESKTOP-UHOHO1R\\SQLEXPRESS;Database=Pruebita;Trusted_Connection=True;TrustServerCertificate=True;");
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
            entity.Property(i => i.StockActual).IsRequired();
            entity.Property(i => i.StockMinimo).IsRequired();

            // Especificamos precisión decimal para dinero (18 enteros, 2 decimales)
            entity.Property(i => i.CostoUnitario).HasColumnType("decimal(18,2)").IsRequired();

            // Ignoramos miembros calculados en C# para que no busquen columnas físicas en SQL
            entity.Ignore(i => i.ValorTotal);
            entity.Ignore(i => i.Estado);
            entity.Ignore(i => i.EstadoTexto);
        });

    }
}