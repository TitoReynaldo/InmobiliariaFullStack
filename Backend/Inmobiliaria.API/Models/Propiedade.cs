using System;
using System.Collections.Generic;

namespace Inmobiliaria.API.Models;

public partial class Propiedade
{
    public int PropiedadId { get; set; }

    public string Direccion { get; set; } = null!;

    public decimal PrecioVenta { get; set; }

    public string? Estado { get; set; }

    public bool? EsBonoVerde { get; set; }

    public int? IdProyecto { get; set; }

    public string? UbicacionGeografica { get; set; }

    public string? TipoInmueble { get; set; }

    public decimal? AreaTotal { get; set; }

    public decimal? TasacionActivo { get; set; }

    public bool? FlagBonoVerde { get; set; }

    public bool? FlagBfh { get; set; }

    public string? MonedaBase { get; set; }

    public virtual ICollection<Simulacione> Simulaciones { get; set; } = new List<Simulacione>();
}//TRAS