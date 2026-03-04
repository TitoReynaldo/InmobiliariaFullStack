using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Inmobiliaria.API.DTOs.Simulacion
{
    public class SimulacionInputDto
    {
        [Required]
        [Range(typeof(decimal), "1", "79228162514264337593543950335", ErrorMessage = "El precio de la vivienda debe ser mayor a 0.")]
        public decimal PrecioVivienda { get; set; }

        [Required]
        [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "La cuota inicial no puede ser negativa.")]
        public decimal CuotaInicial { get; set; }

        [Required]
        [Range(typeof(decimal), "0.0001", "100.0", ErrorMessage = "La tasa de interés debe estar entre 0.0001% y 100%.")]
        public decimal TasaInteres { get; set; }

        [Required]
        [RegularExpression("^(Nominal|Efectiva)$", ErrorMessage = "El tipo de tasa debe ser 'Nominal' o 'Efectiva'.")]
        public string TipoTasa { get; set; } = "Efectiva";

        public string DireccionPropiedad { get; set; } = "Sin Nombre";

        public string Moneda { get; set; } = "PEN";

        public int PlazoAnios { get; set; } = 0;

        [RegularExpression("^(Sin Gracia|Parcial|Total)$", ErrorMessage = "Tipo de gracia inválido.")]
        public string TipoGracia { get; set; } = "Sin Gracia";

        [Range(0, 360, ErrorMessage = "El periodo de gracia no puede ser negativo ni exceder 360 periodos.")]
        public int PeriodosGracia { get; set; } = 0;

        public bool AplicaBonoBuenPagador { get; set; } = false;
        public bool AplicaBonoVerde { get; set; } = false;
        public bool AplicaTechoPropio { get; set; } = false;

        public decimal ValorTasacion { get; set; }
        public decimal CuotaInicialPorcentaje { get; set; }
        public int DiasPorPeriodo { get; set; } = 30;
        public int DiasPorAnio { get; set; } = 360;
        public int PlazoMeses { get; set; }
        public int MesesPorCuota { get; set; } = 1;
        public decimal IncrementoTasaFutura { get; set; } = 0;
        public int CuotaInicioAjuste { get; set; } = 0;

        public decimal CostesNotariales { get; set; } = 0;
        public decimal CostesRegistrales { get; set; } = 0;
        public decimal Tasacion { get; set; } = 0;

        public Dictionary<int, decimal>? PagosAnticipados { get; set; }
        public string TipoPrepago { get; set; } = "ReducirCuota";

        public decimal Portes { get; set; } = 0;
        public decimal GastosAdministracion { get; set; } = 0;
        public decimal SeguroDesgravamenMensual { get; set; } = 0;
        public decimal SeguroRiesgoMensual { get; set; } = 0;
        public decimal TasaDescuento { get; set; } = 0;

        public decimal? IngresoNeto { get; set; }
        public string? SituacionLaboral { get; set; }
        public int? ScoreEndeudamiento { get; set; }
        public string? UbicacionGeografica { get; set; }
        public string? TipoInmueble { get; set; }
        public decimal? AreaTotal { get; set; }
    }
}//TRAS