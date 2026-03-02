using System;

namespace orion.Models
{
    public class ArchivoOrden
    {
        public int Id { get; set; }
        public int IdOrden { get; set; }
        public string NombreOriginal { get; set; }
        public string NombreGuardado { get; set; }
        public string RutaRelativa { get; set; }
        public string Extension { get; set; }
        public long TamanoBytes { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Usuario { get; set; }

        public virtual OrdenCompra Orden { get; set; }
    }
}
