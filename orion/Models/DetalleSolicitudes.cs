using System.ComponentModel.DataAnnotations;

namespace orion.Models
{
   
    public class DetalleSolicitudes
    {
        [Key]
        public int Id { get; set; }

        public int IdSolicitud { get; set; }

        public string? Codigo { get; set; }

        public string? Descripcion { get; set; }

        public string? Proveedor { get; set; }

        public string? Caracteristicas { get; set; }

        public string? Unidad { get; set; }

        public decimal Cantidad { get; set; }

        public string? Estado { get; set; }

        public DateTime? Faprobado { get; set; }
        public string? CodProveedor { get; set; }
        public int? FrequerimientoDias { get; set; }
        public decimal UltimoPrecio { get; set; }
        public DateTime? FultimoPrecio { get; set; }

    }
}
