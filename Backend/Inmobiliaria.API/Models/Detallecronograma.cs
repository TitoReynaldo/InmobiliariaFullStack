using System;
using System.Collections.Generic;

namespace Inmobiliaria.API.Models;

public partial class Detallecronograma
{
    public int DetalleId { get; set; }

    public int SimulacionId { get; set; }

    public int NroCuota { get; set; }

    public decimal TasaEfectivaPeriodo { get; set; }

    public decimal SaldoInicial { get; set; }

    public decimal Interes { get; set; }

    public decimal Amortizacion { get; set; }

    public decimal Cuota { get; set; }

    public decimal SeguroDesgravamen { get; set; }

    public decimal SeguroInmueble { get; set; }

    public decimal CuotaTotal { get; set; }

    public decimal SaldoFinal { get; set; }

    public virtual Simulacione Simulacion { get; set; } = null!;
}
