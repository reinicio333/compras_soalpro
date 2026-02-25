using System.Collections.Generic;

namespace orion.Models
{
    public class EstadosOrden
    {
        public int Id { get; set; }
        public string? Estado { get; set; }
        public string? Detalle { get; set; }

        // Propiedades de navegación
        public virtual ICollection<OrdenCompra>? OrdenesCompra { get; set; }
    }
}