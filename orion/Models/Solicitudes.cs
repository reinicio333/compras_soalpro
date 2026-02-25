using System;
using System.ComponentModel.DataAnnotations;

namespace orion.Models
{
    public class Solicitudes
    {
        [Key]
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime? Frequerimiento { get; set; }
        public string? Referencia { get; set; }
        public string? Solicitante { get; set; }

    }
}