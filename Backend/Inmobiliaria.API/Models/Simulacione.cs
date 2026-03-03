using System;
using System.Collections.Generic;

namespace Inmobiliaria.API.Models;

public partial class Simulacione
{
    public int SimulacionId { get; set; }

    public int ClienteId { get; set; }

    public int PropiedadId { get; set; }

    public int ConfigId { get; set; }

    public decimal MontoPrestamo { get; set; }

    public decimal TasaEfectivaAnual { get; set; }

    public decimal? Van { get; set; }

    public decimal? Tir { get; set; }

    public decimal? Tcea { get; set; }

    public DateTime? FechaSimulacion { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual Configfinanciera Config { get; set; } = null!;

    public virtual ICollection<Detallecronograma> Detallecronogramas { get; set; } = new List<Detallecronograma>();

    public virtual Propiedade Propiedad { get; set; } = null!;
}
