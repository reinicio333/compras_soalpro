using System.ComponentModel.DataAnnotations;

namespace orion.Models
{
    public class ProveProduc
    {
        [Key]
        public int Id { get; set; }
        public string? CodProveedor { get; set; }
        public string? NomProveedor { get; set; }
        public string? CodItem { get; set; }
        public string? NomItem { get; set; }
        public string? Unidad { get; set; }
        public decimal Precio { get; set; }
        public DateTime? FultimaCompra { get; set; }
        public string? Contacto { get; set; }
        public string? Telefono { get; set; }
        public string? Telefono2 { get; set; }
        public string? Correo { get; set; }
        public string? Cuenta { get; set; }
        public string? Banco { get; set; }
        public string? NomCuenta { get; set; }
        public string? LeadTime { get; set; }

    }
}
