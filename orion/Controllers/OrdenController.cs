using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using orion.Models;
using orion.Servicios;

namespace orion.Controllers
{
    [Authorize]
    public class OrdenController : Controller
    {
        private readonly OrionContext _context;

        public OrdenController(OrionContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Gerencia()
        {
            return View();
        }

        #region LISTAR ÓRDENES DE COMPRA

        [HttpGet]
        public async Task<IActionResult> ListarOrdenes()
        {
            try
            {
                var ordenes = await _context.OrdenCompra
                    .Include(o => o.Estado)
                    .Include(o => o.SolicitudPrecio)
                    .OrderByDescending(o => o.Id)
                    .Select(o => new
                    {
                        o.Id,
                        Fecha = o.Fecha,
                        o.Referencia,
                        o.Solicitante,
                        Estado = o.Estado != null ? o.Estado.Estado : "Sin Estado",
                        o.EsImportacion,
                        IdEstado = o.IdEstadoSolicitud
                    })
                    .ToListAsync();

                return Json(ordenes);
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error al cargar órdenes: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEstados()
        {
            try
            {
                var estados = await _context.EstadosOrden
                    .OrderBy(e => e.Id)
                    .Select(e => new
                    {
                        e.Id,
                        e.Estado,
                        e.Detalle
                    })
                    .ToListAsync();

                return Json(estados);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GuardarOrden([FromBody] OrdenCompraDto datos)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var maxIdSolicitudPrecio = await _context.SolicitudPrecio
                    .MaxAsync(sp => (int?)sp.IdSolicitudPrecio) ?? 0;
                var nuevoIdSolicitudPrecio = maxIdSolicitudPrecio + 1;

                var orden = new OrdenCompra
                {
                    Fecha = DateTime.TryParse(datos.Fecha, out var fecha) ? fecha : DateTime.Now,
                    IdSolicitudPrecio = nuevoIdSolicitudPrecio,
                    IdEstadoSolicitud = 1,
                    TipoCambio = datos.Tc,
                    Solicitante = datos.Solicitante,
                    Referencia = datos.Referencia,
                    Observacion = datos.Cabecera?.Observacion,
                    FormaPago = datos.Cabecera?.FormaPago,
                    MedioTransporte = datos.Entrega?.MedioTransporte,
                    ResponsableRecepcion = datos.Entrega?.ResponsableRecepcion,
                    FechaEntrega = DateTime.TryParse(datos.Entrega?.FechaEntrega, out var fEntrega) ? fEntrega : (DateTime?)null,
                    LugarEntrega = datos.Entrega?.LugarEntrega,
                    FechaAnticipo = DateTime.TryParse(datos.Pago?.AnticipoF, out var fAnticipo) ? fAnticipo : (DateTime?)null,
                    MontoAnticipo = decimal.TryParse(datos.Pago?.AnticipoM, out var mAnticipo) ? mAnticipo : (decimal?)null,
                    FechaPagoFinal = DateTime.TryParse(datos.Pago?.FinalF, out var fFinal) ? fFinal : (DateTime?)null,
                    MontoPagoFinal = decimal.TryParse(datos.Pago?.FinalM, out var mFinal) ? mFinal : (decimal?)null,
                    Banco = datos.Pago?.Banco,
                    Cuenta = datos.Pago?.Cuenta,
                    NombreCuentaBancaria = datos.Pago?.NombreCuenta,
                    CodigoSwift = datos.Pago?.Swift,
                    Incoterm = datos.Pago?.Incoterm,
                    RazonSocial = datos.Facturacion?.Razon,
                    Nit = datos.Facturacion?.Nit,
                    Telefono = datos.Telefono,
                    NomContacto = datos.Contacto,
                    EsImportacion = datos.EsImportacion
                };

                _context.OrdenCompra.Add(orden);
                await _context.SaveChangesAsync();

                foreach (var producto in datos.Productos)
                {
                    var solicitudPrecio = new SolicitudPrecio
                    {
                        IdSolicitudPrecio = nuevoIdSolicitudPrecio,
                        IdDetalleSolicitud = producto.IdDetalleSolicitud.ToString(),
                        Precio = producto.Precio
                    };
                    _context.SolicitudPrecio.Add(solicitudPrecio);

                    var detalleProducto = await _context.DetalleSolicitudes
                        .FindAsync(producto.IdDetalleSolicitud);
                    if (detalleProducto != null)
                    {
                        detalleProducto.Estado = "Pendiente";
                    }
                }

                await _context.SaveChangesAsync();
                var historialInicial = new HistorialEstadoOrden
                {
                    IdOrden = orden.Id,
                    IdEstadoAnterior = 1,
                    IdEstadoNuevo = 1,
                    Usuario = User.Identity.Name ?? "Sistema",
                    FechaCambio = DateTime.Now
                };
                _context.HistorialEstadoOrden.Add(historialInicial);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { tipo = "success", mensaje = "Orden de compra guardada correctamente", id = orden.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var innerMessage = ex.InnerException?.Message ?? "Sin inner exception";
                return Json(new { tipo = "error", mensaje = $"Error: {ex.Message} | Inner: {innerMessage}" });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetOrden(int id)
        {
            try
            {
                var orden = await _context.OrdenCompra
                    .Include(o => o.Estado)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (orden == null)
                {
                    return Json(new { tipo = "error", mensaje = "Orden no encontrada" });
                }

                var idSolicitudPrecio = orden.IdSolicitudPrecio ?? 0;
                var proveedor = "";
                var idSolicitud = 0;

                var productos = await (from sp in _context.SolicitudPrecio
                                       where sp.IdSolicitudPrecio == idSolicitudPrecio
                                       join ds in _context.DetalleSolicitudes
                                       on sp.IdDetalleSolicitud equals ds.Id.ToString()
                                       select new
                                       {
                                           IdDetalleSolicitud = ds.Id,
                                           Codigo = ds.Codigo,
                                           Descripcion = ds.Descripcion,
                                           Caracteristicas = ds.Caracteristicas,
                                           Unidad = ds.Unidad,
                                           Cantidad = ds.Cantidad,
                                           Precio = sp.Precio,
                                           IdSolicitud = ds.IdSolicitud,
                                           Proveedor = ds.Proveedor,
                                           UltimoPrecio = ds.UltimoPrecio,   
                                           FultimoPrecio = ds.FultimoPrecio
                                       }).ToListAsync();

                if (productos.Any())
                {
                    proveedor = productos.First().Proveedor;
                    idSolicitud = productos.First().IdSolicitud;
                }

                return Json(new
                {
                    tipo = "success",
                    orden = new
                    {
                        orden.Id,
                        Fecha = orden.Fecha?.ToString("yyyy-MM-dd"),
                        Proveedor = proveedor,
                        IdSolicitud = idSolicitud,
                        orden.TipoCambio,
                        orden.Solicitante,
                        orden.Referencia,
                        orden.Observacion,
                        orden.FormaPago,
                        orden.MedioTransporte,
                        orden.ResponsableRecepcion,
                        FechaEntrega = orden.FechaEntrega?.ToString("yyyy-MM-dd"),
                        orden.LugarEntrega,
                        FechaAnticipo = orden.FechaAnticipo?.ToString("yyyy-MM-dd"),
                        orden.MontoAnticipo,
                        FechaPagoFinal = orden.FechaPagoFinal?.ToString("yyyy-MM-dd"),
                        orden.MontoPagoFinal,
                        orden.Banco,
                        orden.Cuenta,
                        orden.NombreCuentaBancaria,
                        orden.CodigoSwift,
                        orden.Incoterm,
                        orden.RazonSocial,
                        orden.Nit,
                        orden.EsImportacion,
                        orden.IdEstadoSolicitud,
                        orden.Telefono,
                        orden.NomContacto,
                        Estado = orden.Estado?.Estado
                    },
                    productos
                });
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error al cargar orden: " + ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> ActualizarOrden([FromBody] OrdenCompraDto datos)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var orden = await _context.OrdenCompra
                    .FirstOrDefaultAsync(o => o.Id == datos.IdOrden);

                if (orden == null)
                {
                    return Json(new { tipo = "error", mensaje = "Orden no encontrada" });
                }

                orden.Fecha = DateTime.TryParse(datos.Fecha, out var fecha) ? fecha : orden.Fecha;
                orden.TipoCambio = datos.Tc;
                orden.Solicitante = datos.Solicitante;
                orden.Referencia = datos.Referencia;
                orden.Observacion = datos.Cabecera?.Observacion;
                orden.FormaPago = datos.Cabecera?.FormaPago;
                orden.MedioTransporte = datos.Entrega?.MedioTransporte;
                orden.ResponsableRecepcion = datos.Entrega?.ResponsableRecepcion;
                orden.FechaEntrega = DateTime.TryParse(datos.Entrega?.FechaEntrega, out var fEntrega) ? fEntrega : orden.FechaEntrega;
                orden.LugarEntrega = datos.Entrega?.LugarEntrega;
                orden.FechaAnticipo = DateTime.TryParse(datos.Pago?.AnticipoF, out var fAnticipo) ? fAnticipo : orden.FechaAnticipo;
                orden.MontoAnticipo = decimal.TryParse(datos.Pago?.AnticipoM, out var mAnticipo) ? mAnticipo : orden.MontoAnticipo;
                orden.FechaPagoFinal = DateTime.TryParse(datos.Pago?.FinalF, out var fFinal) ? fFinal : orden.FechaPagoFinal;
                orden.MontoPagoFinal = decimal.TryParse(datos.Pago?.FinalM, out var mFinal) ? mFinal : orden.MontoPagoFinal;
                orden.Banco = datos.Pago?.Banco;
                orden.Cuenta = datos.Pago?.Cuenta;
                orden.NombreCuentaBancaria = datos.Pago?.NombreCuenta;
                orden.CodigoSwift = datos.Pago?.Swift;
                orden.Incoterm = datos.Pago?.Incoterm;
                orden.RazonSocial = datos.Facturacion?.Razon;
                orden.Nit = datos.Facturacion?.Nit;
                orden.EsImportacion = datos.EsImportacion; 
                orden.Telefono = datos.Telefono;
                orden.NomContacto = datos.NomContacto;

                if (orden.IdSolicitudPrecio == null)
                {
                    var maxIdSolicitudPrecio = await _context.SolicitudPrecio
                        .MaxAsync(sp => (int?)sp.IdSolicitudPrecio) ?? 0;
                    orden.IdSolicitudPrecio = maxIdSolicitudPrecio + 1;
                }

                var idSolicitudPrecioActual = orden.IdSolicitudPrecio;

                var preciosAntiguos = await _context.SolicitudPrecio
                    .Where(sp => sp.IdSolicitudPrecio == idSolicitudPrecioActual)
                    .ToListAsync();

                foreach (var precioAntiguo in preciosAntiguos)
                {
                    var idDetalleAntiguo = int.Parse(precioAntiguo.IdDetalleSolicitud);

                    if (!datos.Productos.Any(p => p.IdDetalleSolicitud == idDetalleAntiguo))
                    {
                        var detalleProducto = await _context.DetalleSolicitudes.FindAsync(idDetalleAntiguo);
                        if (detalleProducto != null)
                        {
                            detalleProducto.Estado = "Creado";
                        }
                    }
                }

                _context.SolicitudPrecio.RemoveRange(preciosAntiguos);

                foreach (var producto in datos.Productos)
                {
                    var solicitudPrecio = new SolicitudPrecio
                    {
                        IdSolicitudPrecio = idSolicitudPrecioActual,
                        IdDetalleSolicitud = producto.IdDetalleSolicitud.ToString(),
                        Precio = producto.Precio
                    };
                    _context.SolicitudPrecio.Add(solicitudPrecio);

                    var detalleProducto = await _context.DetalleSolicitudes
                        .FindAsync(producto.IdDetalleSolicitud);
                    if (detalleProducto != null)
                    {
                        detalleProducto.Estado = "Pendiente";
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { tipo = "success", mensaje = "Orden actualizada correctamente" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { tipo = "error", mensaje = "Error al actualizar orden: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EliminarOrden(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var orden = await _context.OrdenCompra
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (orden == null)
                {
                    return Json(new { tipo = "error", mensaje = "Orden no encontrada" });
                }

                var idSolicitudPrecio = orden.IdSolicitudPrecio ?? 0;

                if (idSolicitudPrecio > 0)
                {
                    var precios = await _context.SolicitudPrecio
                        .Where(sp => sp.IdSolicitudPrecio == idSolicitudPrecio)
                        .ToListAsync();

                    foreach (var precio in precios)
                    {
                        var idDetalle = int.Parse(precio.IdDetalleSolicitud);
                        var detalleProducto = await _context.DetalleSolicitudes.FindAsync(idDetalle);
                        if (detalleProducto != null)
                        {
                            detalleProducto.Estado = "Creado";
                        }
                    }

                    _context.SolicitudPrecio.RemoveRange(precios);
                }

                _context.OrdenCompra.Remove(orden);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { tipo = "success", mensaje = "Orden eliminada correctamente" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { tipo = "error", mensaje = "Error al eliminar orden: " + ex.Message });
            }
        }



        [HttpPost]
        public async Task<IActionResult> CambiarEstado([FromBody] CambioEstadoDto datos)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var orden = await _context.OrdenCompra.FindAsync(datos.IdOrden);

                if (orden == null)
                {
                    return Json(new { tipo = "error", mensaje = "Orden no encontrada" });
                }

                var estadoAnterior = orden.IdEstadoSolicitud;

                orden.IdEstadoSolicitud = datos.NuevoEstado;

                var historial = new HistorialEstadoOrden
                {
                    IdOrden = datos.IdOrden,
                    IdEstadoAnterior = estadoAnterior,
                    IdEstadoNuevo = datos.NuevoEstado,
                    Usuario = User.Identity.Name ?? "Sistema",
                    FechaCambio = DateTime.Now
                };

                _context.HistorialEstadoOrden.Add(historial);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { tipo = "success", mensaje = "Estado actualizado correctamente" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { tipo = "error", mensaje = "Error al cambiar estado: " + ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetHistorialEstados(int idOrden)
        {
            try
            {
                var historial = await _context.HistorialEstadoOrden
                    .Where(h => h.IdOrden == idOrden)
                    .Include(h => h.EstadoAnterior)
                    .Include(h => h.EstadoNuevo)
                    .OrderByDescending(h => h.FechaCambio)
                    .Select(h => new
                    {
                        h.Id,
                        EstadoAnterior = h.EstadoAnterior != null ? h.EstadoAnterior.Estado : "Inicial",
                        EstadoNuevo = h.EstadoNuevo.Estado,
                        h.Usuario,
                        Fecha = h.FechaCambio.ToString("dd/MM/yyyy HH:mm")
                    })
                    .ToListAsync();

                return Json(historial);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerarPdf([FromBody] GenerarPdfDto datos)
        {
            try
            {
                var orden = await _context.OrdenCompra
                    .Include(o => o.Estado)
                    .FirstOrDefaultAsync(o => o.Id == datos.IdOrden);

                if (orden == null)
                {
                    return NotFound();
                }

                
                var idSolicitud = await _context.SolicitudPrecio
                    .Where(sp => sp.IdSolicitudPrecio == orden.IdSolicitudPrecio)
                    .Join(_context.DetalleSolicitudes,
                        sp => sp.IdDetalleSolicitud,
                        ds => ds.Id.ToString(),
                        (sp, ds) => ds.IdSolicitud)
                    .FirstOrDefaultAsync();

                var solicitud = await _context.Solicitudes
                    .FirstOrDefaultAsync(s => s.Id == idSolicitud);

                if (solicitud == null)
                {
                    solicitud = new Solicitudes
                    {
                        Id = 0,
                        Fecha = DateTime.Now,
                        Frequerimiento = null,
                        Referencia = orden.Referencia,
                        Solicitante = orden.Solicitante
                    };
                }

                var ordenDto = await ConvertirOrdenADto(orden);

                var pdfService = new ReporteOrdenCompraPdfService();
                var pdfBytes = pdfService.GenerarPdfOrdenCompra(solicitud, ordenDto);

                return File(pdfBytes, "application/pdf", $"OC_{orden.Id}_{DateTime.Now:ddMMyyyy}.pdf");
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error al generar PDF: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerarPdfVistaPrevia([FromBody] GenerarPdfDto datos)
        {
            try
            {
                var orden = await _context.OrdenCompra
                    .Include(o => o.Estado)
                    .FirstOrDefaultAsync(o => o.Id == datos.IdOrden);

                if (orden == null)
                {
                    return NotFound();
                }

                var idSolicitud = await _context.SolicitudPrecio
                    .Where(sp => sp.IdSolicitudPrecio == orden.IdSolicitudPrecio)
                    .Join(_context.DetalleSolicitudes,
                        sp => sp.IdDetalleSolicitud,
                        ds => ds.Id.ToString(),
                        (sp, ds) => ds.IdSolicitud)
                    .FirstOrDefaultAsync();

                var solicitud = await _context.Solicitudes
                    .FirstOrDefaultAsync(s => s.Id == idSolicitud);

                if (solicitud == null)
                {
                    solicitud = new Solicitudes
                    {
                        Id = 0,
                        Fecha = DateTime.Now,
                        Frequerimiento = null,
                        Referencia = orden.Referencia,
                        Solicitante = orden.Solicitante
                    };
                }


                var ordenDto = await ConvertirOrdenADto(orden);

                var pdfService = new ReporteOrdenCompraPdfService();
                var pdfBytes = pdfService.GenerarPdfOrdenCompra(solicitud, ordenDto);

                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error al generar vista previa: " + ex.Message });
            }
        }

        private async Task<OrdenCompraDto> ConvertirOrdenADto(OrdenCompra orden)
        {
            var idSolicitudPrecio = orden.IdSolicitudPrecio ?? 0;

            var proveedor = "";
            var productosLista = new List<ProductoOrdenDto>();

            if (idSolicitudPrecio > 0)
            {
                var productos = await (from sp in _context.SolicitudPrecio
                                       where sp.IdSolicitudPrecio == idSolicitudPrecio
                                       join ds in _context.DetalleSolicitudes
                                       on sp.IdDetalleSolicitud equals ds.Id.ToString()
                                       select new
                                       {
                                           Detalle = ds,
                                           Precio = sp.Precio
                                       }).ToListAsync();

                if (productos.Any())
                {
                    proveedor = productos.First().Detalle.Proveedor ?? "";
                }

                var index = 1;
                foreach (var producto in productos)
                {
                    var solicitudProducto = await _context.Solicitudes
                        .FirstOrDefaultAsync(s => s.Id == producto.Detalle.IdSolicitud);
                    productosLista.Add(new ProductoOrdenDto
                    {
                        IdDetalleSolicitud = producto.Detalle.Id,
                        Codigo = producto.Detalle.Codigo,
                        Nro = index.ToString(),
                        Descripcion = producto.Detalle.Descripcion,
                        FechaEntrega = solicitudProducto?.Frequerimiento?.ToString("dd/MM/yyyy") ?? "",
                        Caracteristicas = producto.Detalle.Caracteristicas,
                        Unidad = producto.Detalle.Unidad,
                        Cantidad = producto.Detalle.Cantidad,
                        Precio = producto.Precio,
                        UltimoPrecio = producto.Detalle.UltimoPrecio,
                        FultimoPrecio = producto.Detalle.FultimoPrecio
                    });
                    index++;
                }
            }

            return new OrdenCompraDto
            {
                IdSolicitud = 0, 
                Fecha = orden.Fecha?.ToString("dd/MM/yyyy") ?? DateTime.Now.ToString("dd/MM/yyyy"),
                Proveedor = proveedor,
                Telefono = orden.Telefono ?? "",
                Contacto = orden.NomContacto ?? "",
                NomContacto = orden.NomContacto ?? "",
                Solicitante = orden.Solicitante,
                Rol = "",
                Referencia = orden.Referencia,
                Tc = orden.TipoCambio ?? "6.96",
                Cabecera = new CabeceraDto
                {
                    Observacion = orden.Observacion,
                    FormaPago = orden.FormaPago
                },
                Entrega = new EntregaDto
                {
                    MedioTransporte = orden.MedioTransporte,
                    ResponsableRecepcion = orden.ResponsableRecepcion,
                    FechaEntrega = orden.FechaEntrega?.ToString("dd/MM/yyyy"),
                    LugarEntrega = orden.LugarEntrega
                },
                Pago = new PagoDto
                {
                    AnticipoF = orden.FechaAnticipo?.ToString("dd/MM/yyyy"),
                    AnticipoM = orden.MontoAnticipo?.ToString("N2"),
                    FinalF = orden.FechaPagoFinal?.ToString("dd/MM/yyyy"),
                    FinalM = orden.MontoPagoFinal?.ToString("N2"),
                    Banco = orden.Banco,
                    Cuenta = orden.Cuenta,
                    NombreCuenta = orden.NombreCuentaBancaria,
                    Swift = orden.CodigoSwift,
                    Incoterm = orden.Incoterm
                },
                Facturacion = new FacturacionDto
                {
                    Razon = orden.RazonSocial,
                    Nit = orden.Nit
                },
                Productos = productosLista
            };
        }
        [HttpGet]
        public async Task<IActionResult> GetProveedores()
        {
            try
            {
                var proveedores = await _context.DetalleSolicitudes
                    .Where(d => !string.IsNullOrEmpty(d.Proveedor) && d.Estado == "Creado")
                    .Select(d => d.Proveedor)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToListAsync();

                return Json(proveedores);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReferenciaPorProveedor(string proveedor)
        {
            try
            {
                if (string.IsNullOrEmpty(proveedor))
                {
                    return Json(new
                    {
                        referencia = "No seleccionado",
                        id = 0,
                        telefono = "",
                        contacto = "",
                        banco = "",
                        cuenta = "",
                        nombreCuenta = ""
                    });
                }

                var detalleSolicitud = await _context.DetalleSolicitudes
                    .Where(d => d.Proveedor == proveedor)
                    .OrderByDescending(d => d.IdSolicitud)
                    .FirstOrDefaultAsync();

                if (detalleSolicitud == null)
                {
                    return Json(new
                    {
                        referencia = "Sin referencia",
                        id = 0,
                        telefono = "",
                        contacto = "",
                        banco = "",
                        cuenta = "",
                        nombreCuenta = ""
                    });
                }

                var solicitud = await _context.Solicitudes
                    .FirstOrDefaultAsync(s => s.Id == detalleSolicitud.IdSolicitud);

                string telefono = "";
                string contacto = "";
                string banco = "";
                string cuenta = "";
                string nombreCuenta = "";

                if (!string.IsNullOrEmpty(detalleSolicitud.CodProveedor))
                {
                    var proveProduc = await _context.ProveProduc
                        .Where(pp => pp.CodProveedor == detalleSolicitud.CodProveedor)
                        .FirstOrDefaultAsync();

                    if (proveProduc != null)
                    {
                        var telefonos = new[] { proveProduc.Telefono, proveProduc.Telefono2 }
                        .Where(t => !string.IsNullOrEmpty(t) && t != "0")
                        .ToList();

                        telefono = string.Join(" - ", telefonos);
                        contacto = proveProduc.Contacto ?? "";
                        banco = proveProduc.Banco ?? "";
                        cuenta = proveProduc.Cuenta ?? "";
                        nombreCuenta = proveProduc.NomCuenta ?? "";
                    }
                }

                return Json(new
                {
                    referencia = solicitud?.Referencia ?? "Sin referencia",
                    id = solicitud?.Id ?? 0,
                    telefono,
                    contacto,
                    banco,
                    cuenta,
                    nombreCuenta
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDetalleSolicitud(int id)
        {
            try
            {
                var solicitud = await _context.Solicitudes
                    .FirstOrDefaultAsync(s => s.Id == id);

                var fechaEntrega = solicitud?.Frequerimiento?.ToString("yyyy-MM-dd") ?? "";
                var detalles = await _context.DetalleSolicitudes
                    .Where(d => d.IdSolicitud == id && d.Estado == "Creado")
                    .Select(d => new
                    {
                        d.Id,
                        d.Codigo,
                        d.Descripcion,
                        d.Proveedor,
                        d.Caracteristicas,
                        d.Unidad,
                        d.Cantidad,
                        FechaEntrega = fechaEntrega
                    })
                    .ToListAsync();

                return Json(detalles);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = "Error: " + ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetDetallesPorProveedor(string proveedor)
        {
            try
            {
                if (string.IsNullOrEmpty(proveedor))
                {
                    return Json(new List<object>());
                }

                var detalles = await _context.DetalleSolicitudes
                    .Where(d => d.Proveedor == proveedor && d.Estado == "Creado")
                    .Join(
                        _context.Solicitudes,
                        detalle => detalle.IdSolicitud,
                        solicitud => solicitud.Id,
                        (detalle, solicitud) => new
                        {
                            detalle.Id,
                            detalle.Codigo,
                            detalle.Descripcion,
                            detalle.Proveedor,
                            detalle.Caracteristicas,
                            detalle.Unidad,
                            detalle.UltimoPrecio,      
                            detalle.FultimoPrecio,
                            detalle.Cantidad,
                            FechaEntrega = solicitud.Frequerimiento.HasValue
                                ? solicitud.Frequerimiento.Value.ToString("yyyy-MM-dd")
                                : ""
                        }
                    )
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

                return Json(detalles);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = "Error: " + ex.Message });
            }
        }
        #endregion
    }

    #region DTOs

    public class OrdenCompraDto
    {
        public int IdOrden { get; set; }
        public int IdSolicitud { get; set; }
        public string Fecha { get; set; }
        public string Proveedor { get; set; }
        public string Telefono { get; set; }
        public string Contacto { get; set; }
        public string Solicitante { get; set; }
        public string Rol { get; set; }
        public string Referencia { get; set; }
        public string Tc { get; set; } = "6.96";
        public bool EsImportacion { get; set; }
        public string NomContacto { get; set; }
        public CabeceraDto Cabecera { get; set; }
        public EntregaDto Entrega { get; set; }
        public PagoDto Pago { get; set; }
        public FacturacionDto Facturacion { get; set; }
        public List<ProductoOrdenDto> Productos { get; set; }
    }

    public class CabeceraDto
    {
        public string Observacion { get; set; }
        public string FormaPago { get; set; }
    }

    public class EntregaDto
    {
        public string MedioTransporte { get; set; }
        public string ResponsableRecepcion { get; set; }
        public string FechaEntrega { get; set; }
        public string LugarEntrega { get; set; }
    }

    public class PagoDto
    {
        public string AnticipoF { get; set; }
        public string AnticipoM { get; set; }
        public string FinalF { get; set; }
        public string FinalM { get; set; }
        public string Banco { get; set; }
        public string Cuenta { get; set; }
        public string NombreCuenta { get; set; }
        public string Swift { get; set; }
        public string Incoterm { get; set; }
    }

    public class FacturacionDto
    {
        public string Razon { get; set; }
        public string Nit { get; set; }
    }

    public class ProductoOrdenDto
    {
        public int IdDetalleSolicitud { get; set; }
        public string Codigo { get; set; }
        public string Nro { get; set; }
        public string Descripcion { get; set; }
        public string FechaEntrega { get; set; }
        public string Caracteristicas { get; set; }
        public string Unidad { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal? UltimoPrecio { get; set; } 
        public DateTime? FultimoPrecio { get; set; }
    }

    public class CambioEstadoDto
    {
        public int IdOrden { get; set; }
        public int NuevoEstado { get; set; }
    }

    public class GenerarPdfDto
    {
        public int IdOrden { get; set; }
    }

    #endregion
}