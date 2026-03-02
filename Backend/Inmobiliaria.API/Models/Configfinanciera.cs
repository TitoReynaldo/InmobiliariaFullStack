using System;
using System.Collections.Generic;

namespace Inmobiliaria.API.Models;

public partial class Configfinanciera
{
    public int ConfigId { get; set; }

    public string NombreBanco { get; set; } = null!;

    public string? TipoTasa { get; set; }

    public decimal TasaInteres { get; set; }

    public decimal PorcentajeDesgravamen { get; set; }

    public decimal PorcentajeSeguroInmueble { get; set; }

    public int PlazoMinimoMeses { get; set; }

    public int PlazoMaximoMeses { get; set; }

    public virtual ICollection<Simulacione> Simulaciones { get; set; } = new List<Simulacione>();
}
