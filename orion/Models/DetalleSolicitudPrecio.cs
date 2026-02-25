namespace orion.Models
{
    public class DetalleSolicitudPrecio
    {
        public int Id { get; set; }
        public int IdSolicitudPrecio { get; set; }
        public int IdDetalleSolicitud { get; set; }
        public decimal Precio { get; set; }

        // Propiedades de navegación
        public virtual SolicitudPrecio? SolicitudPrecio { get; set; }
        public virtual DetalleSolicitudes? DetalleSolicitud { get; set; }
    }
}