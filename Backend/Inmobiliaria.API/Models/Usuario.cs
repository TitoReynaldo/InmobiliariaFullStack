using System;
using System.Collections.Generic;

namespace Inmobiliaria.API.Models;

public partial class Usuario
{
    public int UsuarioId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Rol { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public virtual ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();
}
