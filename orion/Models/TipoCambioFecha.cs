using System;

namespace orion.Models
{
    public class TipoCambioFecha
    {
        public int Id { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal Valor { get; set; }
        public string Estado { get; set; } = "1";
    }
}
