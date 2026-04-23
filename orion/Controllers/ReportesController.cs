using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using orion.Models;
using orion.Servicios;
using System.Linq;

namespace orion.Controllers
{
    [Authorize]
    public class ReportesController : Controller
    {
        private readonly OrionContext _context;

        public ReportesController(OrionContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

       

        [HttpGet]
        public async Task<IActionResult> GenerarReporteExcel(string? fechaDesde = null,string? fechaHasta = null)
        {
            var tipoUsuario = await ObtenerTipoUsuarioAsync();
            var idArea = tipoUsuario.Tipo == "PLANTA" ? tipoUsuario.IdArea : (string?)null;
            var soloAlmacen = tipoUsuario.Tipo == "ALMACEN";
            try
            {
                DateTime? desde = DateTime.TryParse(fechaDesde, out var fd) ? fd.Date : null;
                DateTime? hasta = DateTime.TryParse(fechaHasta, out var fh) ? fh.Date.AddDays(1).AddSeconds(-1) : null;
                var ordenes = await ObtenerDetalleReporteAsync(desde, hasta, null, idArea, soloAlmacen);
                var excelService = new ReporteOrdenCompraExcelService();
                var excelBytes = excelService.GenerarExcelReporteGeneral(ordenes);

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"REPORTE_ORDENES_{DateTime.Now:ddMMyyyy}.xlsx");
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error al generar reporte: " + ex.Message });
            }
        }

        private async Task<List<ReporteGeneralOrdenDetalleDto>> ObtenerDetalleReporteAsync(DateTime? fechaDesde, DateTime? fechaHasta, List<int>? estados, string? idAreaPlanta = null, bool soloAlmacen = false)
        {
            // ── Obtener órdenes base ─────────────────────────────────────
            var query = _context.OrdenCompra
                .Include(o => o.Estado)
                .AsQueryable();
            // Filtro perfil PLANTA: solo su área
            if (!string.IsNullOrEmpty(idAreaPlanta))
                query = query.Where(o => o.IdAreaCorrespondencia.ToString() == idAreaPlanta);

            // Filtro perfil ALMACEN: solo órdenes que alguna vez llegaron a estado 9
            if (soloAlmacen)
            {
                var idsConAlmacen = await _context.HistorialEstadoOrden
                    .Where(h => h.IdEstadoNuevo == 9)
                    .Select(h => h.IdOrden)
                    .Distinct()
                    .ToListAsync();
                query = query.Where(o => idsConAlmacen.Contains(o.Id));
            }
            if (estados != null && estados.Count > 0)
                query = query.Where(o => estados.Contains((int)o.IdEstadoSolicitud));

            var ordenes = await query
                .OrderByDescending(o => o.Id)
                .Select(o => new ReporteGeneralOrdenDto
                {
                    Id = o.Id,
                    Fecha = o.Fecha,
                    IdSolicitudPrecio = o.IdSolicitudPrecio,
                    Referencia = o.Referencia,
                    Solicitante = o.Solicitante,
                    Estado = o.Estado != null ? o.Estado.Estado : "Sin Estado",
                    EsImportacion = o.EsImportacion ?? false,
                    IdEstado = o.IdEstadoSolicitud,
                    FechaEstado = _context.HistorialEstadoOrden
                        .Where(h => h.IdOrden == o.Id)
                        .OrderByDescending(h => h.FechaCambio)
                        .Select(h => h.FechaCambio)
                        .FirstOrDefault(),
                    Proveedor = _context.SolicitudPrecio
                        .Where(sp => sp.IdSolicitudPrecio == o.IdSolicitudPrecio)
                        .Join(_context.DetalleSolicitudes,
                            sp => sp.IdDetalleSolicitud,
                            ds => ds.Id.ToString(),
                            (sp, ds) => ds.Proveedor)
                        .FirstOrDefault() ?? "",
                    TipoCambio = o.TipoCambio,
                    Observacion = o.Observacion,
                    FormaPago = o.FormaPago,
                    MedioTransporte = o.MedioTransporte,
                    ResponsableRecepcion = o.ResponsableRecepcion,
                    FechaEntrega = o.FechaEntrega,
                    LugarEntrega = o.LugarEntrega,
                    FechaAnticipo = o.FechaAnticipo,
                    MontoAnticipo = o.MontoAnticipo,
                    FechaPagoFinal = o.FechaPagoFinal,
                    MontoPagoFinal = o.MontoPagoFinal,
                    Banco = o.Banco,
                    Cuenta = o.Cuenta,
                    NombreCuentaBancaria = o.NombreCuentaBancaria,
                    CodigoSwift = o.CodigoSwift,
                    Incoterm = o.Incoterm,
                    RazonSocial = o.RazonSocial,
                    Nit = o.Nit,
                    Telefono = o.Telefono,
                    NomContacto = o.NomContacto,
                    Aprobador = o.Aprobador,
                    IdAreaCorrespondencia = o.IdAreaCorrespondencia,
                    CorrespondeAsc = o.CorrespondeAsc,
                    RecepcionTipo = o.RecepcionTipo,
                    ObservacionRecepcion = o.ObservacionRecepcion,
                    RutasArchivos = o.RutasArchivos
                })
                .ToListAsync();

            // Filtrar las que todos los productos son stock
            var idsSolicitudPrecio = ordenes
                .Where(o => o.IdSolicitudPrecio.HasValue)
                .Select(o => o.IdSolicitudPrecio!.Value)
                .Distinct()
                .ToList();

            var todosStockIds = await _context.SolicitudPrecio
                .Where(sp => sp.IdSolicitudPrecio.HasValue
                    && idsSolicitudPrecio.Contains(sp.IdSolicitudPrecio.Value))
                .GroupBy(sp => sp.IdSolicitudPrecio)
                .Where(g => g.All(sp => sp.EsStock == true))
                .Select(g => g.Key)
                .ToListAsync();

            ordenes = ordenes.Where(o => !todosStockIds.Contains(o.IdSolicitudPrecio)).ToList();

            // ── Detalle de ítems con filtro por Frequerimiento ───────────
            var idsValidos = ordenes
                .Where(o => o.IdSolicitudPrecio.HasValue)
                .Select(o => o.IdSolicitudPrecio!.Value)
                .Distinct()
                .ToList();

            var detalleQuery = (
                from sp in _context.SolicitudPrecio
                where sp.IdSolicitudPrecio.HasValue && idsValidos.Contains(sp.IdSolicitudPrecio.Value)
                join ds in _context.DetalleSolicitudes on sp.IdDetalleSolicitud equals ds.Id.ToString()
                join s in _context.Solicitudes on ds.IdSolicitud equals s.Id
                select new
                {
                    IdSolicitudPrecio = sp.IdSolicitudPrecio!.Value,
                    IdSolicitud = s.Id,
                    Proveedor = ds.Proveedor,
                    NombreItem = ds.Descripcion,
                    CodigoItem = ds.Codigo,
                    Cantidad = sp.Cantidad ?? ds.Cantidad,
                    Precio = sp.Precio,
                    Frequerimiento = ds.Frequerimiento,
                    SolicitanteSolicitud = s.Solicitante,
                    ReferenciaSolicitud = s.Referencia
                });

            if (fechaDesde.HasValue)
                detalleQuery = detalleQuery.Where(x => x.Frequerimiento >= fechaDesde.Value);
            if (fechaHasta.HasValue)
                detalleQuery = detalleQuery.Where(x => x.Frequerimiento <= fechaHasta.Value);

            var detalleItems = await detalleQuery.ToListAsync();

            // Si se aplicó filtro de fechas, quedarse solo con órdenes que tengan ítems
            if (fechaDesde.HasValue || fechaHasta.HasValue)
            {
                var idsSolicitudPrecioConItems = detalleItems
                    .Select(x => x.IdSolicitudPrecio)
                    .Distinct()
                    .ToHashSet();
                ordenes = ordenes
                    .Where(o => o.IdSolicitudPrecio.HasValue
                        && idsSolicitudPrecioConItems.Contains(o.IdSolicitudPrecio.Value))
                    .ToList();
            }

            var detallePorSolicitudPrecio = detalleItems
                .GroupBy(x => x.IdSolicitudPrecio)
                .ToDictionary(g => g.Key, g => g.ToList());

            // ── Solicitudes vinculadas ───────────────────────────────────
            var solicitudesVinculadas = detalleItems
                .GroupBy(x => x.IdSolicitudPrecio)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Ids = string.Join(", ", g.Select(x => x.IdSolicitud).Distinct().OrderBy(x => x)),
                        Referencias = string.Join(", ", g
                            .Where(x => !string.IsNullOrWhiteSpace(x.ReferenciaSolicitud))
                            .Select(x => x.ReferenciaSolicitud).Distinct()),
                        Solicitantes = string.Join(", ", g
                            .Where(x => !string.IsNullOrWhiteSpace(x.SolicitanteSolicitud))
                            .Select(x => x.SolicitanteSolicitud).Distinct())
                    });

            foreach (var orden in ordenes)
            {
                if (orden.IdSolicitudPrecio.HasValue
                    && solicitudesVinculadas.TryGetValue(orden.IdSolicitudPrecio.Value, out var sv))
                {
                    orden.SolicitudesVinculadas = sv.Ids;
                    orden.ReferenciasSolicitudesVinculadas = sv.Referencias;
                    orden.SolicitantesSolicitudesVinculadas = sv.Solicitantes;
                }
            }

            // ── Historial de estados ─────────────────────────────────────
            var idsOrdenes = ordenes.Select(o => o.Id).ToList();
            var historialEstados = await _context.HistorialEstadoOrden
                .Where(h => idsOrdenes.Contains(h.IdOrden))
                .GroupBy(h => new { h.IdOrden, h.IdEstadoNuevo })
                .Select(g => new
                {
                    g.Key.IdOrden,
                    g.Key.IdEstadoNuevo,
                    Fecha = g.Min(h => h.FechaCambio)
                })
                .ToListAsync();

            var historialPorOrden = historialEstados
                .GroupBy(h => h.IdOrden)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(h => h.IdEstadoNuevo, h => h.Fecha));

            // ── Armar resultado final ────────────────────────────────────
            var resultado = new List<ReporteGeneralOrdenDetalleDto>();

            foreach (var orden in ordenes)
            {
                var historial = historialPorOrden.TryGetValue(orden.Id, out var hist)
                    ? hist : new Dictionary<int, DateTime>();

                if (!orden.IdSolicitudPrecio.HasValue
                    || !detallePorSolicitudPrecio.TryGetValue(orden.IdSolicitudPrecio.Value, out var items)
                    || items.Count == 0)
                {
                    resultado.Add(MapearOrdenDetalle(orden, historial));
                    continue;
                }

                foreach (var item in items)
                {
                    var dto = MapearOrdenDetalle(orden, historial);
                    dto.IdSolicitud = item.IdSolicitud;
                    dto.ProveedorItem = item.Proveedor;
                    dto.NombreItem = item.NombreItem;
                    dto.CodigoItem = item.CodigoItem;
                    dto.CantidadItem = item.Cantidad;
                    dto.PrecioItem = item.Precio;
                    dto.Frequerimiento = item.Frequerimiento;
                    resultado.Add(dto);
                }
            }

            return resultado;
        }
        [HttpGet]
        public async Task<IActionResult> GetPreviewReporte(string? fechaDesde = null,string? fechaHasta = null)
        {
            var tipoUsuario = await ObtenerTipoUsuarioAsync();
            var idArea = tipoUsuario.Tipo == "PLANTA" ? tipoUsuario.IdArea : (string?)null;
            var soloAlmacen = tipoUsuario.Tipo == "ALMACEN";
            try
            {
                DateTime? desde = DateTime.TryParse(fechaDesde, out var fd) ? fd.Date : null;
                DateTime? hasta = DateTime.TryParse(fechaHasta, out var fh) ? fh.Date.AddDays(1).AddSeconds(-1) : null;

                var ordenes = await ObtenerDetalleReporteAsync(desde, hasta, null, idArea, soloAlmacen);

                return Json(ordenes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener preview: " + ex.Message });
            }
        }
        private async Task<(string Tipo, string? IdArea)> ObtenerTipoUsuarioAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return ("COMPRAS", null);

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            if (usuario == null)
                return ("COMPRAS", null);

            return (usuario.IdTipo?.ToUpper() ?? "COMPRAS", usuario.Area);
        }
        private static ReporteGeneralOrdenDetalleDto MapearOrdenDetalle(
            ReporteGeneralOrdenDto o,
            Dictionary<int, DateTime> historial)
        {
            return new ReporteGeneralOrdenDetalleDto
            {
                Id = o.Id,
                Fecha = o.Fecha,
                IdSolicitudPrecio = o.IdSolicitudPrecio,
                SolicitudesVinculadas = o.SolicitudesVinculadas,
                ReferenciasSolicitudesVinculadas = o.ReferenciasSolicitudesVinculadas,
                SolicitantesSolicitudesVinculadas = o.SolicitantesSolicitudesVinculadas,
                Referencia = o.Referencia,
                Solicitante = o.Solicitante,
                Proveedor = o.Proveedor,
                EsImportacion = o.EsImportacion,
                Estado = o.Estado,
                IdEstado = o.IdEstado,
                FechaEstado = o.FechaEstado,
                TipoCambio = o.TipoCambio,
                Observacion = o.Observacion,
                FormaPago = o.FormaPago,
                MedioTransporte = o.MedioTransporte,
                ResponsableRecepcion = o.ResponsableRecepcion,
                FechaEntrega = o.FechaEntrega,
                LugarEntrega = o.LugarEntrega,
                FechaAnticipo = o.FechaAnticipo,
                MontoAnticipo = o.MontoAnticipo,
                FechaPagoFinal = o.FechaPagoFinal,
                MontoPagoFinal = o.MontoPagoFinal,
                Banco = o.Banco,
                Cuenta = o.Cuenta,
                NombreCuentaBancaria = o.NombreCuentaBancaria,
                CodigoSwift = o.CodigoSwift,
                Incoterm = o.Incoterm,
                RazonSocial = o.RazonSocial,
                Nit = o.Nit,
                Telefono = o.Telefono,
                NomContacto = o.NomContacto,
                Aprobador = o.Aprobador,
                IdAreaCorrespondencia = o.IdAreaCorrespondencia,
                CorrespondeAsc = o.CorrespondeAsc,
                RecepcionTipo = o.RecepcionTipo,
                ObservacionRecepcion = o.ObservacionRecepcion,
                RutasArchivos = o.RutasArchivos,
                HistorialEstados = historial
            };
        }
    }
}