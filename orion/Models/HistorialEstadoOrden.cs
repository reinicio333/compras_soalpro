namespace orion.Models
{
    public class HistorialEstadoOrden
    {
        public int Id { get; set; }
        public int IdOrden { get; set; }
        public int? IdEstadoAnterior { get; set; }
        public int IdEstadoNuevo { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaCambio { get; set; }

        // Navegación
        public OrdenCompra Orden { get; set; }
        public EstadosOrden EstadoAnterior { get; set; }
        public EstadosOrden EstadoNuevo { get; set; }
    }
}