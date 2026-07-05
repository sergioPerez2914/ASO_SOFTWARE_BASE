using System;
using Microsoft.Extensions.Configuration;

namespace ASO.Desktop.Configuration;

/// <summary>
/// Punto unico de acceso a la configuracion de la aplicacion.
/// Carga appsettings.json (valores por defecto, en el repo) y superpone
/// appsettings.local.json (por maquina, en .gitignore) si existe.
/// </summary>
public static class AppConfig
{
    private static readonly IConfigurationRoot _config = Build();

    private static IConfigurationRoot Build()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
            .Build();
    }

    /// <summary>Cadena de conexion a SQL Server (clave ConnectionStrings:AsoDb).</summary>
    public static string ConnectionString =>
        _config.GetConnectionString("AsoDb")
        ?? throw new InvalidOperationException(
            "No se encontro la cadena de conexion 'AsoDb'. " +
            "Revisa appsettings.json o crea appsettings.local.json a partir de appsettings.local.example.json.");

    /// <summary>
    /// Si es true la app usa datos mock (en memoria) y no necesita SQL Server.
    /// Clave "UseMock" en appsettings(.local).json. Por defecto false (SQL real).
    /// </summary>
    public static bool UseMock =>
        bool.TryParse(_config["UseMock"], out var valor) && valor;
}
