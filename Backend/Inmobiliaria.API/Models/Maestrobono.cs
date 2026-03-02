using System;
using System.Collections.Generic;

namespace Inmobiliaria.API.Models;

public partial class Maestrobono
{
    public int BonoId { get; set; }

    public string TipoBono { get; set; } = null!;

    public decimal ValorViviendaMin { get; set; }

    public decimal ValorViviendaMax { get; set; }

    public decimal ValorBono { get; set; }
}
