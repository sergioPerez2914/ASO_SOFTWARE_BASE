using System.Collections.Generic;

namespace ASO.Desktop.Models;

/// <summary>
/// Boleto que emite el Central Azucarero al recibir una carga: el papel que cierra la remesa.
/// Los campos siguen el ejemplar de <c>docs/boleto.pdf</c>.
///
/// Es un tipo <i>owned</i> de <see cref="Remesa"/>, no un documento aparte: hay exactamente uno
/// por remesa, nace y muere con ella y no tiene ciclo de vida propio que gobernar.
///
/// El pesaje NO vive aquí. <see cref="Remesa.PesoBrutoT"/>, <see cref="Remesa.TaraT"/> y
/// <see cref="Remesa.PesoNetoT"/> se quedan en la remesa porque son las toneladas operativas que
/// leen la facturación, la liquidación y el seguimiento; el boleto guarda lo demás que trae el papel.
/// </summary>
public class BoletoCentral
{
    // --- Identificación del papel ---

    /// <summary>Número de formulario impreso en el boleto ("Form." en el ejemplar del central).</summary>
    public string Numero { get; set; } = string.Empty;

    // La fecha y hora del boleto no se guardan aquí: son la llegada al central
    // (Remesa.LlegadaCentral), que es cuando el CAM lo emite en la Pre-Romana. Guardarlas dos
    // veces solo daría dos versiones del mismo dato que podrían dejar de coincidir.

    // --- Calidad de la caña ---
    // Informativos por decisión del cliente: el cobro sigue siendo tarifa × toneladas netas.
    // Se guardan porque son el argumento del central cuando el monto no coincide, y porque el día
    // que el socio defina la fórmula de ATR ya estarán capturados desde el principio.

    /// <summary>Azúcares Totales Recuperables.</summary>
    public decimal? Atr { get; set; }
    public decimal? Fibra { get; set; }
    public decimal? Pureza { get; set; }
    public decimal? TrashMineral { get; set; }
    public decimal? TrashVegetal { get; set; }

    // --- Montos que declara el central ---
    // Se registran tal como los imprime el boleto, sin recalcularlos: son lo que el central dice
    // que va a pagar. Lo que la app calcula con su propio tarifario se compara contra esto.

    public decimal MontoCanaEntregada { get; set; }

    public decimal DescuentoCorte { get; set; }
    public decimal DescuentoAlzaEmpuje { get; set; }
    public decimal DescuentoTransporte { get; set; }
    public decimal DescuentoAdministracion { get; set; }
    public decimal DescuentoRural { get; set; }
    public decimal DescuentoInvestigacion { get; set; }

    public decimal ValorLiquido { get; set; }

    public decimal TotalDescuentos =>
        DescuentoCorte + DescuentoAlzaEmpuje + DescuentoTransporte
        + DescuentoAdministracion + DescuentoRural + DescuentoInvestigacion;

    /// <summary>Los tres descuentos que tienen contraparte en el tarifario de la app.</summary>
    public decimal DescuentosDeServicio => DescuentoCorte + DescuentoAlzaEmpuje + DescuentoTransporte;

    public string NumeroTexto => Numero.Length == 0 ? "(sin número)" : Numero;

    public string CalidadTexto
    {
        get
        {
            var partes = new List<string>();
            if (Atr is { } atr) partes.Add($"ATR {atr:N4}");
            if (Fibra is { } fibra) partes.Add($"fibra {fibra:N2} %");
            if (Pureza is { } pureza) partes.Add($"pureza {pureza:N2} %");
            return partes.Count == 0 ? "Sin datos de calidad" : string.Join(" · ", partes);
        }
    }

    public BoletoCentral Clonar() => (BoletoCentral)MemberwiseClone();
}
