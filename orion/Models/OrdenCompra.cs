using System;
using System.Collections.Generic;

namespace orion.Models
{
    public class OrdenCompra
    {
        public int Id { get; set; }
        public DateTime? Fecha { get; set; }
        public int? IdProveedor { get; set; }
        public int? IdSolicitudPrecio { get; set; }
        public int? IdEstadoSolicitud { get; set; }
        public string? TipoCambio { get; set; }
        public string? Solicitante { get; set; }
        public string? Referencia { get; set; }
        public string? Observacion { get; set; }
        public string? FormaPago { get; set; }
        public string? MedioTransporte { get; set; }
        public string? ResponsableRecepcion { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public string? LugarEntrega { get; set; }
        public DateTime? FechaAnticipo { get; set; }
        public decimal? MontoAnticipo { get; set; }
        public DateTime? FechaPagoFinal { get; set; }
        public decimal? MontoPagoFinal { get; set; }
        public string? Banco { get; set; }
        public string? Cuenta { get; set; }
        public string? NombreCuentaBancaria { get; set; }
        public string? CodigoSwift { get; set; }
        public string? Incoterm { get; set; }
        public string? RazonSocial { get; set; }
        public string? Nit { get; set; }
        public bool? EsImportacion { get; set; }
        public string? Telefono { get; set; }
        public string? NomContacto { get; set; }

        // Propiedades de navegación
        public virtual SolicitudPrecio? SolicitudPrecio { get; set; }
        public virtual EstadosOrden? Estado { get; set; }
    }
}