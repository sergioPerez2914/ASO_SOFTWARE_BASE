using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Datos de ejemplo para el módulo de inventario mientras no existe base de datos.
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockInventoryDataSource : IInventoryDataSource
{
    public IEnumerable<InventoryItem> GetItems() => new List<InventoryItem>
    {
        new() { Codigo = "REP-001", Nombre = "Cuchilla de corte base",          Categoria = "Cuchillas",             Unidad = "und", Ubicacion = "Almacén A-1", StockActual = 42,  StockMinimo = 20,  CostoUnitario = 85.50m },
        new() { Codigo = "REP-002", Nombre = "Cuchilla trituradora",            Categoria = "Cuchillas",             Unidad = "und", Ubicacion = "Almacén A-1", StockActual = 12,  StockMinimo = 15,  CostoUnitario = 120.00m },
        new() { Codigo = "FIL-010", Nombre = "Filtro de aceite de motor",       Categoria = "Filtros",               Unidad = "und", Ubicacion = "Almacén B-2", StockActual = 60,  StockMinimo = 25,  CostoUnitario = 18.75m },
        new() { Codigo = "FIL-011", Nombre = "Filtro de combustible",           Categoria = "Filtros",               Unidad = "und", Ubicacion = "Almacén B-2", StockActual = 8,   StockMinimo = 10,  CostoUnitario = 22.40m },
        new() { Codigo = "FIL-012", Nombre = "Filtro de aire primario",         Categoria = "Filtros",               Unidad = "und", Ubicacion = "Almacén B-2", StockActual = 0,   StockMinimo = 8,   CostoUnitario = 34.90m },
        new() { Codigo = "MAN-020", Nombre = "Manguera hidráulica 1/2\"",       Categoria = "Mangueras hidráulicas", Unidad = "m",   Ubicacion = "Almacén C-1", StockActual = 150, StockMinimo = 50,  CostoUnitario = 12.30m },
        new() { Codigo = "MAN-021", Nombre = "Manguera hidráulica 3/4\"",       Categoria = "Mangueras hidráulicas", Unidad = "m",   Ubicacion = "Almacén C-1", StockActual = 35,  StockMinimo = 40,  CostoUnitario = 15.80m },
        new() { Codigo = "LUB-030", Nombre = "Aceite hidráulico ISO 68",        Categoria = "Lubricantes",           Unidad = "L",   Ubicacion = "Almacén D-3", StockActual = 220, StockMinimo = 100, CostoUnitario = 6.90m },
        new() { Codigo = "LUB-031", Nombre = "Grasa multipropósito",            Categoria = "Lubricantes",           Unidad = "kg",  Ubicacion = "Almacén D-3", StockActual = 45,  StockMinimo = 20,  CostoUnitario = 9.50m },
        new() { Codigo = "LUB-032", Nombre = "Aceite de motor 15W-40",          Categoria = "Lubricantes",           Unidad = "L",   Ubicacion = "Almacén D-3", StockActual = 18,  StockMinimo = 60,  CostoUnitario = 7.25m },
        new() { Codigo = "NEU-040", Nombre = "Neumático 23.1-26 cosechadora",   Categoria = "Neumáticos",            Unidad = "und", Ubicacion = "Patio E",     StockActual = 4,   StockMinimo = 4,   CostoUnitario = 1850.00m },
        new() { Codigo = "NEU-041", Nombre = "Neumático 11R22.5 camión",        Categoria = "Neumáticos",            Unidad = "und", Ubicacion = "Patio E",     StockActual = 10,  StockMinimo = 6,   CostoUnitario = 620.00m },
        new() { Codigo = "ROD-050", Nombre = "Rodamiento de rodillos cónicos",  Categoria = "Rodamientos",           Unidad = "und", Ubicacion = "Almacén B-1", StockActual = 0,   StockMinimo = 12,  CostoUnitario = 45.00m },
        new() { Codigo = "ROD-051", Nombre = "Rodamiento rígido de bolas",      Categoria = "Rodamientos",           Unidad = "und", Ubicacion = "Almacén B-1", StockActual = 30,  StockMinimo = 15,  CostoUnitario = 28.00m },
        new() { Codigo = "COR-060", Nombre = "Correa de transmisión trapezoidal", Categoria = "Correas",             Unidad = "und", Ubicacion = "Almacén C-2", StockActual = 22,  StockMinimo = 10,  CostoUnitario = 33.60m },
        new() { Codigo = "BAT-070", Nombre = "Batería 12V 150Ah",               Categoria = "Baterías",              Unidad = "und", Ubicacion = "Almacén A-2", StockActual = 3,   StockMinimo = 5,   CostoUnitario = 210.00m }
    };
}
