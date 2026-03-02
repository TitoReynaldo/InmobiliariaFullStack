using System.Collections.Generic;

namespace Inmobiliaria.API.DTOs.Simulacion
{
    public class SimulacionResultDto
    {
        public string Moneda { get; set; } = "PEN";

        public decimal CuotaMensualReferencial { get; set; }
        public decimal TEA { get; set; }
        public decimal TCEA { get; set; }
        public decimal VAN { get; set; }
        public decimal TIR { get; set; }

        public decimal TotalIntereses { get; set; }
        public decimal TotalAmortizacion { get; set; }
        public decimal TotalSeguros { get; set; }
        public decimal TotalPortes { get; set; }
        public decimal TotalGastosAdmin { get; set; }
        public string AdvertenciaRiesgo { get; set; } = string.Empty;

        public List<DetalleCronogramaDto> Cronograma { get; set; } = new List<DetalleCronogramaDto>();
    }

    public class DetalleCronogramaDto
    {
        public int NroCuota { get; set; }
        public decimal SaldoInicial { get; set; }
        public decimal Amortizacion { get; set; }
        public decimal Interes { get; set; }
        public decimal SegDesgravamen { get; set; }
        public decimal SeguroRiesgo { get; set; }
        public decimal SegInmueble { get; set; }
        public decimal Portes { get; set; }
        public decimal GastosAdministracion { get; set; }
        public decimal CuotaTotal { get; set; }
        public decimal SaldoFinal { get; set; }
        public decimal TasaPeriodo { get; set; }
    }
}