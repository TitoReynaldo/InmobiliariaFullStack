using System;
using System.Collections.Generic;

namespace Inmobiliaria.API.Models;

public partial class Configfinanciera
{
    public int ConfigId { get; set; }

    public string? Moneda { get; set; }

    public decimal CuotaInicial { get; set; }

    public int PlazoMeses { get; set; }

    public string? TipoTasa { get; set; }

    public decimal TasaInteres { get; set; }

    public int? DiasPorPeriodo { get; set; }

    public int? DiasPorAnio { get; set; }

    public string? TipoGracia { get; set; }

    public int? MesesGracia { get; set; }

    public decimal? CostesNotariales { get; set; }

    public decimal? CostesRegistrales { get; set; }

    public decimal? Tasacion { get; set; }

    public decimal? Portes { get; set; }

    public decimal? GastosAdministracion { get; set; }

    public decimal PorcentajeDesgravamen { get; set; }

    public decimal PorcentajeSeguroInmueble { get; set; }

    public decimal? TasaDescuento { get; set; }

    public virtual ICollection<Simulacione> Simulaciones { get; set; } = new List<Simulacione>();
}
