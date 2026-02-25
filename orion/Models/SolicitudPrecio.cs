using System.Collections.Generic;

namespace orion.Models
{
    public class SolicitudPrecio
    {
        public int Id { get; set; }
        public string? IdDetalleSolicitud { get; set; }
        public int? IdSolicitudPrecio { get; set; }
        public decimal Precio { get; set; }

        // Propiedades de navegación
        public virtual ICollection<DetalleSolicitudPrecio>? DetallesPrecios { get; set; }
        public virtual ICollection<OrdenCompra>? OrdenesCompra { get; set; }
    }
}