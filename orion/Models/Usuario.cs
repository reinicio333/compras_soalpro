using System;
using System.Collections.Generic;

namespace orion.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string IdTipo { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public string? Idusuario { get; set; }

    public string? Contraseña { get; set; }

    public string? NomCompleto { get; set; }
    public string? Area { get; set; }
}
