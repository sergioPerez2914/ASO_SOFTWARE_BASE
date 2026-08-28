using System;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Zafra sobre la que trabaja la sesión actual. Hermana de <see cref="Ambito"/> (misma razón de
/// ser: estática y global porque <see cref="BD.AsoDbContext"/> se construye sin argumentos en
/// cada método de las fuentes Sql), pero NO igual: el núcleo es fijo de por vida de la
/// instalación, mientras que la zafra cambia varias veces al año, así que <see cref="Fijar"/> es
/// público y se llama en caliente —al iniciar sesión y cada vez que <see cref="ZafraService"/>
/// abre, cierra o reabre una zafra— y no solo en el login.
///
/// Fail-closed, igual que <see cref="Ambito"/>: sin zafra fijada, <see cref="Exigir"/> lanza en
/// vez de dejar pasar un documento sin zafra.
///
/// Hoy nada lee <see cref="ZafraId"/> todavía: el filtro real (<c>IDeZafra</c> +
/// <c>HasQueryFilter</c>) es una fase posterior, pendiente de las preguntas de negocio del plan
/// de Zafra activa.
/// </summary>
public static class ZafraActiva
{
    /// <summary>Zafra Abierta del núcleo activo; null si no hay ninguna (instalación nueva, o
    /// la última se cerró sin abrir la siguiente).</summary>
    public static Zafra? Actual { get; private set; }

    public static int? ZafraId => Actual?.Id;

    public static bool EstaFijada => Actual is not null;

    public static void Fijar(Zafra? zafra) => Actual = zafra;

    /// <summary>Zafra activa, o excepción si no hay ninguna. Para escrituras.</summary>
    public static int Exigir() => Actual?.Id
        ?? throw new InvalidOperationException(
            "No hay una zafra activa. Abra una zafra en Administración antes de registrar documentos.");
}
