using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using orion.Models;
using orion.Servicios;

namespace orion.Controllers
{
    [Authorize]
    public class SolicitudesController : Controller
    {
        private readonly OrionContext _context;

        public SolicitudesController(OrionContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ListarSolicitudes()
        {
            try
            {
                var solicitante = User.Identity.Name;
                var solicitudes = await _context.Solicitudes
                    .Where(s => s.Solicitante == solicitante)
                    .ToListAsync();
                return Json(solicitudes);
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error al cargar solicitudes: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Guardar()
        {
            try
            {
                var solicitud = new Solicitudes
                {
                    Fecha = DateTime.Parse(Request.Form["fecha"]),
                    Frequerimiento = string.IsNullOrEmpty(Request.Form["frequerimiento"])
                        ? null
                        : DateTime.Parse(Request.Form["frequerimiento"]),
                    Referencia = Request.Form["referencia"],
                    Solicitante = Request.Form["solicitante"]
                };

                _context.Solicitudes.Add(solicitud);
                await _context.SaveChangesAsync();

                var productos = new List<DetalleSolicitudes>();
                int index = 1;

                while (Request.Form.ContainsKey($"productos[{index}][codigo]"))
                {
                    var producto = new DetalleSolicitudes
                    {
                        IdSolicitud = solicitud.Id,
                        Codigo = Request.Form[$"productos[{index}][codigo]"],
                        Descripcion = Request.Form[$"productos[{index}][descripcion]"],
                        Proveedor = Request.Form[$"productos[{index}][proveedor]"],
                        Caracteristicas = Request.Form[$"productos[{index}][caracteristicas]"],
                        Unidad = Request.Form[$"productos[{index}][unidad]"],
                        Cantidad = decimal.Parse(Request.Form[$"productos[{index}][cantidad]"]),
                        CodProveedor = Request.Form[$"productos[{index}][codProveedor]"],
                        FrequerimientoDias = string.IsNullOrEmpty(Request.Form[$"productos[{index}][frequerimiento_dias]"])
                        ? null
                        : int.Parse(Request.Form[$"productos[{index}][frequerimiento_dias]"]),
                        UltimoPrecio = decimal.TryParse(Request.Form[$"productos[{index}][ultimoPrecio]"], out var up) ? up : 0,
                        FultimoPrecio = DateTime.TryParse(Request.Form[$"productos[{index}][fultimaCompra]"], out var fup) ? fup : null,
                        Estado = "Creado",
                        Faprobado = null
                    };

                    productos.Add(producto);
                    index++;
                }

                if (!productos.Any())
                {
                    _context.Solicitudes.Remove(solicitud);
                    await _context.SaveChangesAsync();
                    return Json(new { tipo = "warning", mensaje = "DEBE AGREGAR AL MENOS UN PRODUCTO" });
                }

                _context.DetalleSolicitudes.AddRange(productos);
                await _context.SaveChangesAsync();

                return Json(new { tipo = "success", mensaje = "SOLICITUD REGISTRADA CON ÉXITO" });
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSolicitud(int id)
        {
            try
            {
                var solicitud = await _context.Solicitudes.FindAsync(id);
                if (solicitud == null)
                {
                    return Json(new { tipo = "error", mensaje = "Solicitud no encontrada" });
                }

                // Obtener los detalles con su estado
                var detalles = await _context.DetalleSolicitudes
                    .Where(d => d.IdSolicitud == id)
                    .Select(d => new {
                        d.Id,
                        d.Codigo,
                        d.Descripcion,
                        d.Proveedor,
                        d.Caracteristicas,
                        d.Unidad,
                        d.Cantidad,
                        d.CodProveedor,
                        d.Estado,
                        d.IdSolicitud,
                        d.FrequerimientoDias,
                        NumeroOrden = _context.SolicitudPrecio
                        .Where(sp => sp.IdDetalleSolicitud == d.Id.ToString())
                        .Join(_context.OrdenCompra,
                            sp => sp.IdSolicitudPrecio,
                            oc => oc.IdSolicitudPrecio,
                            (sp, oc) => oc.Id)
                        .FirstOrDefault()
                    })
                    .ToListAsync();

                return Json(new { solicitud, detalles });
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error: " + ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> Actualizar()
        {
            try
            {
                int idSolicitud = int.Parse(Request.Form["id"]);

                var solicitud = await _context.Solicitudes.FindAsync(idSolicitud);
                if (solicitud == null)
                {
                    return Json(new { tipo = "error", mensaje = "SOLICITUD NO ENCONTRADA" });
                }

                solicitud.Fecha = DateTime.Parse(Request.Form["fecha"]);

                // Solo permitir cambiar frequerimiento si NO hay productos en Pendiente
                var tieneProductosEnUso = await _context.DetalleSolicitudes
                    .AnyAsync(d => d.IdSolicitud == idSolicitud && d.Estado == "Pendiente");

                if (tieneProductosEnUso)
                {
                    // Mantener la fecha de requerimiento original
                }
                else
                {
                    solicitud.Frequerimiento = string.IsNullOrEmpty(Request.Form["frequerimiento"])
                        ? null
                        : DateTime.Parse(Request.Form["frequerimiento"]);
                }

                solicitud.Referencia = Request.Form["referencia"];
                solicitud.Solicitante = Request.Form["solicitante"];

                // Obtener productos que están en "Pendiente" (NO se pueden modificar NI eliminar)
                var productosEnPendiente = await _context.DetalleSolicitudes
                    .Where(d => d.IdSolicitud == idSolicitud && d.Estado == "Pendiente")
                    .ToListAsync();

                // Obtener productos que están en "Creado" (se pueden eliminar)
                var productosCreados = await _context.DetalleSolicitudes
                    .Where(d => d.IdSolicitud == idSolicitud && d.Estado == "Creado")
                    .ToListAsync();

                // Solo eliminar productos en estado "Creado"
                _context.DetalleSolicitudes.RemoveRange(productosCreados);

                var productos = new List<DetalleSolicitudes>();
                int index = 1;

                while (Request.Form.ContainsKey($"productos[{index}][codigo]"))
                {
                    var codigoProducto = Request.Form[$"productos[{index}][codigo]"];

                    // Verificar si este producto ya existe en Pendiente
                    var productoPendiente = productosEnPendiente
                        .FirstOrDefault(p => p.Codigo == codigoProducto);

                    if (productoPendiente != null)
                    {
                        // Si está en Pendiente, NO modificar NADA
                        // Los productos en Pendiente se mantienen tal cual
                    }
                    else
                    {
                        var producto = new DetalleSolicitudes
                        {
                            IdSolicitud = solicitud.Id,
                            Codigo = codigoProducto,
                            Descripcion = Request.Form[$"productos[{index}][descripcion]"],
                            Proveedor = Request.Form[$"productos[{index}][proveedor]"],
                            Caracteristicas = Request.Form[$"productos[{index}][caracteristicas]"],
                            Unidad = Request.Form[$"productos[{index}][unidad]"],
                            Cantidad = decimal.Parse(Request.Form[$"productos[{index}][cantidad]"]),
                            CodProveedor = Request.Form[$"productos[{index}][codProveedor]"],
                            FrequerimientoDias = string.IsNullOrEmpty(Request.Form[$"productos[{index}][frequerimiento_dias]"])
                            ? null
                            : int.Parse(Request.Form[$"productos[{index}][frequerimiento_dias]"]),
                            UltimoPrecio = decimal.TryParse(Request.Form[$"productos[{index}][ultimoPrecio]"], out var up) ? up : 0,
                            FultimoPrecio = DateTime.TryParse(Request.Form[$"productos[{index}][fultimaCompra]"], out var fup) ? fup : null,
                            Estado = "Creado",
                            Faprobado = null
                        };
                        productos.Add(producto);
                    }

                    index++;
                }

                if (!productos.Any() && !productosEnPendiente.Any())
                {
                    return Json(new { tipo = "warning", mensaje = "DEBE AGREGAR AL MENOS UN PRODUCTO" });
                }

                _context.DetalleSolicitudes.AddRange(productos);
                await _context.SaveChangesAsync();

                return Json(new { tipo = "success", mensaje = "SOLICITUD ACTUALIZADA CON ÉXITO" });
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EliminarSolicitud(int id)
        {
            try
            {
                var productosEnUso = await _context.DetalleSolicitudes
                    .Where(d => d.IdSolicitud == id && d.Estado == "Pendiente")
                    .CountAsync();

                if (productosEnUso > 0)
                {
                    return Json(new
                    {
                        tipo = "error",
                        mensaje = "NO SE PUEDE ELIMINAR LA SOLICITUD PORQUE TIENE PRODUCTOS EN USO EN ÓRDENES DE COMPRA"
                    });
                }

                var solicitud = await _context.Solicitudes.FindAsync(id);
                if (solicitud == null)
                {
                    return Json(new { tipo = "warning", mensaje = "SOLICITUD NO ENCONTRADA" });
                }

                // Eliminar primero los detalles
                var detalles = await _context.DetalleSolicitudes
                    .Where(d => d.IdSolicitud == id)
                    .ToListAsync();

                _context.DetalleSolicitudes.RemoveRange(detalles);
                _context.Solicitudes.Remove(solicitud);

                await _context.SaveChangesAsync();

                return Json(new { tipo = "success", mensaje = "SOLICITUD ELIMINADA CORRECTAMENTE" });
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error al eliminar: " + ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> VerificarEstadoSolicitud(int id)
        {
            try
            {
                var productosEnUso = await _context.DetalleSolicitudes
                    .Where(d => d.IdSolicitud == id && d.Estado == "Pendiente")
                    .CountAsync();

                return Json(new
                {
                    tieneProductosEnUso = productosEnUso > 0,
                    cantidadProductosEnUso = productosEnUso
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = "Error: " + ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> DescargarPdfSolicitud(int id)
        {
            try
            {
                // Obtener la solicitud
                var solicitud = await _context.Solicitudes.FindAsync(id);
                if (solicitud == null)
                {
                    return NotFound();
                }

                // Obtener los detalles
                var detalles = await _context.DetalleSolicitudes
                    .Where(d => d.IdSolicitud == id)
                    .ToListAsync();

                // Generar el PDF
                var pdfService = new ReporteSolicitudPdfService();
                var pdfBytes = pdfService.GenerarPdfSolicitud(solicitud, detalles);

                // Retornar el archivo para abrir en nueva pestaña
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error al generar PDF: " + ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> VistaPreviaPdfSolicitud(int id)
        {
            try
            {
                // Obtener la solicitud
                var solicitud = await _context.Solicitudes.FindAsync(id);
                if (solicitud == null)
                {
                    return NotFound();
                }

                // Obtener los detalles
                var detalles = await _context.DetalleSolicitudes
                    .Where(d => d.IdSolicitud == id)
                    .ToListAsync();

                // Generar el PDF
                var pdfService = new ReporteSolicitudPdfService();
                var pdfBytes = pdfService.GenerarPdfSolicitud(solicitud, detalles);

                // Retornar para vista previa (sin nombre de archivo)
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error al generar vista previa: " + ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> BuscarProductoProveedor(string q)
        {
            try
            {
                var query = _context.ProveProduc.AsQueryable();

                if (!string.IsNullOrEmpty(q))
                {
                    query = query.Where(p => p.CodItem.Contains(q) || p.NomItem.Contains(q));
                }

                var productos = await query
                    .GroupBy(p => new { p.CodItem, p.NomItem, p.Unidad })
                    .Select(g => new
                    {
                        codItem = g.Key.CodItem,
                        nomItem = g.Key.NomItem,
                        unidad = g.Key.Unidad,
                        text = $"{g.Key.CodItem} - {g.Key.NomItem}"
                    })
                    .Take(50)
                    .ToListAsync();

                return Json(productos);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = "Error al buscar producto: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerProveedoresPorProducto(string codItem)
        {
            try
            {
                var proveedores = await _context.ProveProduc
                    .Where(p => p.CodItem == codItem)
                    .GroupBy(p => new { p.CodProveedor })
                    .Select(g => new
                    {
                        codProveedor = g.Key.CodProveedor,
                        nomProveedor = g.Max(x => x.NomProveedor)
                    })
                    .ToListAsync();

                return Json(proveedores);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = "Error: " + ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> ObtenerPrecioPorProveedorProducto(string codItem, string codProveedor)
        {
            try
            {
                var dato = await _context.ProveProduc
                    .Where(p => p.CodItem == codItem && p.CodProveedor == codProveedor)
                    .OrderByDescending(p => p.FultimaCompra)
                    .Select(p => new
                    {
                        ultimoPrecio = p.Precio,
                        fultimaCompra = p.FultimaCompra,
                        leadTime = p.LeadTime
                    })
                    .FirstOrDefaultAsync();

                return Json(dato ?? new { ultimoPrecio = 0m, fultimaCompra = (DateTime?)null, leadTime = (int?)null });
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = "Error: " + ex.Message });
            }
        }
    }
}