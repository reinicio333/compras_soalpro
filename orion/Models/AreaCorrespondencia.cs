using System.Collections.Generic;

namespace orion.Models
{
    public class AreaCorrespondencia
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Estado { get; set; }

        public virtual ICollection<OrdenCompra> OrdenesCompra { get; set; } = new List<OrdenCompra>();
    }
}
