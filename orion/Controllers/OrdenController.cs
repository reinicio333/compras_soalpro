using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using orion.Models;
using orion.Servicios;
using System.IO;
using System.Text.Json;

namespace orion.Controllers
{
    [Authorize]
    public class OrdenController : Controller
    {
        private readonly OrionContext _context;
        private readonly IEmailService _emailService;
        private const decimal TipoCambioPorDefecto = 6.96m;
        private const long MaxArchivoBytes = 1 * 1024 * 1024;
        private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".pdf", ".xls", ".xlsx", ".doc", ".docx"
        };

        public OrdenController(OrionContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
                var nombreUsuario = User.Identity.Name;
                var usuarioActual = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Nombre == nombreUsuario);

                IQueryable<OrdenCompra> query = _context.OrdenCompra
                    .Include(o => o.Estado)
                    .Include(o => o.SolicitudPrecio);

                if (usuarioActual?.IdTipo == "ADMINISTRADOR" ||
                    usuarioActual?.IdTipo == "ALMACEN" ||
                    usuarioActual?.IdTipo == "COMPRAS")
                {
                    // Ven todas las ordenes, sin filtro
                }
                else if (usuarioActual?.IdTipo == "GERENCIA")
                {
                    // GERENCIA: ve solo ordenes en Pre autorización (estado 2) asignadas al aprobador actual
                    var idGerenteActual = usuarioActual.Id.ToString();
                    query = query.Where(o => o.IdEstadoSolicitud == 2
                        && (o.Aprobador == idGerenteActual || o.Aprobador == nombreUsuario));
                }
                else if (usuarioActual?.IdTipo == "PLANTA")
                {
                    // Solo ven órdenes de su área
                    var usuariosDelArea = await _context.Usuarios
                        .Where(u => u.Area == usuarioActual.Area)
                        .Select(u => u.Nombre)
                        .ToListAsync();

                    var idSolicitudesDelArea = await _context.Solicitudes
                        .Where(s => usuariosDelArea.Contains(s.Solicitante))
                        .Select(s => s.Id)
                        .ToListAsync();

                    var idDetalleSolicitudesDelArea = await _context.DetalleSolicitudes
                        .Where(d => idSolicitudesDelArea.Contains(d.IdSolicitud))
                        .Select(d => d.Id.ToString())
                        .ToListAsync();

                    var idSolicitudPrecioDelAreaSinStock = await _context.SolicitudPrecio
                        .Where(sp => idDetalleSolicitudesDelArea.Contains(sp.IdDetalleSolicitud)
                            && sp.EsStock != true)
                        .Select(sp => sp.IdSolicitudPrecio)
                        .Distinct()
                        .ToListAsync();

                    query = query.Where(o => idSolicitudPrecioDelAreaSinStock.Contains(o.IdSolicitudPrecio));
                }
                else
                {
                    // COMPRAS u otros: solo ven las de su área
                    if (string.IsNullOrEmpty(usuarioActual?.Area))
                    {
                        query = query.Where(o => o.Solicitante == nombreUsuario);
                    }
                    else
                    {
                        var usuariosDelArea = await _context.Usuarios
                            .Where(u => u.Area == usuarioActual.Area)
                            .Select(u => u.Nombre)
                            .ToListAsync();

                        query = query.Where(o => usuariosDelArea.Contains(o.Solicitante));
                    }
                }

                var ordenes = await query
                    .OrderByDescending(o => o.Id)
                    .Select(o => new
                    {
                        o.Id,
                        Fecha = o.Fecha,
                        o.Referencia,
                        o.Solicitante,
                        Estado = o.Estado != null ? o.Estado.Estado : "Sin Estado",
                        o.EsImportacion,
                        IdEstado = o.IdEstadoSolicitud,
                        FechaEstado = _context.HistorialEstadoOrden
                            .Where(h => h.IdOrden == o.Id)
                            .OrderByDescending(h => h.FechaCambio)
                            .Select(h => h.FechaCambio)
                            .FirstOrDefault(),
                        TodosEnStock = _context.SolicitudPrecio
                            .Any(sp => sp.IdSolicitudPrecio == o.IdSolicitudPrecio)
                            && _context.SolicitudPrecio
                                .Where(sp => sp.IdSolicitudPrecio == o.IdSolicitudPrecio)
                                .All(sp => sp.EsStock == true),
                        Proveedor = _context.SolicitudPrecio
                            .Where(sp => sp.IdSolicitudPrecio == o.IdSolicitudPrecio)
                            .Join(_context.DetalleSolicitudes,
                                sp => sp.IdDetalleSolicitud,
                                ds => ds.Id.ToString(),
                                (sp, ds) => ds.Proveedor)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                var ordenesFiltradas = ordenes
                    .Where(o => !o.TodosEnStock)
                    .Select(o => new
                    {
                        o.Id,
                        o.Fecha,
                        o.Referencia,
                        o.Solicitante,
                        o.Estado,
                        o.EsImportacion,
                        o.IdEstado,
                        o.FechaEstado,
                        o.Proveedor
                    });

                return Json(ordenesFiltradas);
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error al cargar órdenes: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTipoUsuario()
        {
            var nombreUsuario = User.Identity.Name;
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Nombre == nombreUsuario);

            return Json(new
            {
                tipo = usuario?.IdTipo ?? "",
                area = usuario?.Area ?? ""
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAreasCorrespondencia()
        {
            var areas = await _context.AreasCorrespondencia
                .Where(a => a.Estado == null || a.Estado == "A")
                .OrderBy(a => a.Nombre)
                .Select(a => new
                {
                    id = a.Id,
                    nombre = a.Nombre
                })
                .ToListAsync();

            return Json(areas);
        }

        [HttpGet]
        public async Task<IActionResult> GetAprobadores()
        {
            var aprobadores = await _context.Usuarios
                .Where(u => u.IdTipo == "GERENCIA")
                .OrderBy(u => u.NomCompleto)
                .Select(u => new
                {
                    id = u.Id.ToString(),
                    usuario = u.Nombre,
                    nombre = string.IsNullOrWhiteSpace(u.NomCompleto) ? u.Nombre : u.Nombre + " - " + u.NomCompleto
                })
                .ToListAsync();

            return Json(aprobadores);
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

                var aprobadorId = await ObtenerAprobadorId(datos.Aprobador);
                if (string.IsNullOrEmpty(aprobadorId))
                {
                    return Json(new { tipo = "warning", mensaje = "Debe seleccionar un APROVADOR válido" });
                }

                var orden = new OrdenCompra
                {
                    Fecha = DateTime.TryParse(datos.Fecha, out var fecha) ? fecha : DateTime.Now,
                    IdSolicitudPrecio = nuevoIdSolicitudPrecio,
                    IdEstadoSolicitud = 1,
                    TipoCambio = await ObtenerTipoCambioTextoPorFechaAsync(DateTime.TryParse(datos.Fecha, out var fechaTcGuardar) ? fechaTcGuardar : DateTime.Now),
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
                    Aprobador = aprobadorId,
                    EsImportacion = datos.EsImportacion,
                    IdAreaCorrespondencia = datos.IdAreaCorrespondencia,
                    CorrespondeAsc = datos.CorrespondeAsc
                };

                _context.OrdenCompra.Add(orden);
                await _context.SaveChangesAsync();

                foreach (var producto in datos.Productos)
                {
                    var solicitudPrecio = new SolicitudPrecio
                    {
                        IdSolicitudPrecio = nuevoIdSolicitudPrecio,
                        IdDetalleSolicitud = producto.IdDetalleSolicitud.ToString(),
                        Precio = producto.Precio,
                        Cantidad = producto.Cantidad,
                        EsStock = producto.EsStock
                    };
                    _context.SolicitudPrecio.Add(solicitudPrecio);

                    var detalleProducto = await _context.DetalleSolicitudes
                        .FindAsync(producto.IdDetalleSolicitud);
                    if (detalleProducto != null)
                    {
                        detalleProducto.Estado = producto.EsStock ? "En Stock" : "Pendiente";
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

                try
                {
                    var proveedor = await _context.SolicitudPrecio
                        .Where(sp => sp.IdSolicitudPrecio == orden.IdSolicitudPrecio)
                        .Join(_context.DetalleSolicitudes,
                            sp => sp.IdDetalleSolicitud,
                            ds => ds.Id.ToString(),
                            (sp, ds) => ds.Proveedor)
                        .FirstOrDefaultAsync();

                    // Obtener solicitudes vinculadas
                    var solicitudesVinculadas = await (
                        from sp in _context.SolicitudPrecio
                        where sp.IdSolicitudPrecio == orden.IdSolicitudPrecio
                        join ds in _context.DetalleSolicitudes on sp.IdDetalleSolicitud equals ds.Id.ToString()
                        join sol in _context.Solicitudes on ds.IdSolicitud equals sol.Id
                        select new { sol.Id, sol.Solicitante }
                    ).Distinct().ToListAsync();

                    var solicitudesEmail = new List<(string, string)>();
                    foreach (var sol in solicitudesVinculadas)
                    {
                        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Nombre == sol.Solicitante);
                        var nombreCompleto = !string.IsNullOrWhiteSpace(usuario?.NomCompleto)
                            ? usuario.NomCompleto
                            : sol.Solicitante;
                        solicitudesEmail.Add((sol.Id.ToString(), nombreCompleto));
                    }

                    var destinatarios = await ObtenerSolicitantesOrdenAsync(orden);
                    if (destinatarios.Any())
                    {
                        await _emailService.EnviarAsync(
                            destinatarios,
                            $"Orden de Compra #{orden.Id} ha sido creada",
                            EmailService.NotificacionCreada(
                                orden.Id.ToString(),
                                proveedor ?? "-",
                                orden.Fecha?.ToString("dd/MM/yyyy") ?? "",
                                solicitudesEmail
                            ),
                            TipoNotificacion.Normal
                        );
                    }
                }
                catch { }

                return Json(new { tipo = "success", mensaje = "Orden de compra guardada correctamente", id = orden.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var innerMessage = ex.InnerException?.Message ?? "Sin inner exception";
                return Json(new { tipo = "error", mensaje = $"Error: {ex.Message} | Inner: {innerMessage}" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SubirArchivosOrden(int idOrden, List<IFormFile> archivos)
        {
            try
            {
                if (idOrden <= 0)
                {
                    return Json(new { tipo = "warning", mensaje = "La orden no es válida" });
                }

                var orden = await _context.OrdenCompra.FirstOrDefaultAsync(o => o.Id == idOrden);
                if (orden == null)
                {
                    return Json(new { tipo = "warning", mensaje = "La orden no existe" });
                }

                if (archivos == null || archivos.Count == 0)
                {
                    return Json(new { tipo = "warning", mensaje = "Debe seleccionar al menos un archivo" });
                }

                var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var carpetaOrden = Path.Combine(webRoot, "uploads", "ordenes", idOrden.ToString());
                Directory.CreateDirectory(carpetaOrden);

                var archivosActuales = string.IsNullOrWhiteSpace(orden.RutasArchivos)
                    ? new List<ArchivoOrdenItemDto>()
                    : JsonSerializer.Deserialize<List<ArchivoOrdenItemDto>>(orden.RutasArchivos) ?? new List<ArchivoOrdenItemDto>();

                foreach (var archivo in archivos)
                {
                    if (archivo.Length <= 0)
                    {
                        continue;
                    }

                    if (archivo.Length > MaxArchivoBytes)
                    {
                        return Json(new { tipo = "warning", mensaje = $"El archivo '{archivo.FileName}' supera 1MB" });
                    }

                    var extension = Path.GetExtension(archivo.FileName);
                    if (string.IsNullOrWhiteSpace(extension) || !ExtensionesPermitidas.Contains(extension))
                    {
                        return Json(new { tipo = "warning", mensaje = $"El archivo '{archivo.FileName}' no tiene un formato permitido" });
                    }

                    var nombreLimpio = Path.GetFileNameWithoutExtension(archivo.FileName);
                    nombreLimpio = string.Concat(nombreLimpio.Split(Path.GetInvalidFileNameChars())).Trim();
                    if (string.IsNullOrWhiteSpace(nombreLimpio))
                    {
                        nombreLimpio = "archivo";
                    }

                    var nombreFinal = $"{nombreLimpio}_{DateTime.Now:yyyyMMddHHmmssfff}{extension}";
                    var rutaDestino = Path.Combine(carpetaOrden, nombreFinal);

                    await using (var stream = new FileStream(rutaDestino, FileMode.Create))
                    {
                        await archivo.CopyToAsync(stream);
                    }

                    archivosActuales.Add(new ArchivoOrdenItemDto
                    {
                        Nombre = Path.GetFileName(archivo.FileName),
                        Archivo = nombreFinal,
                        TamanoKb = Math.Round(archivo.Length / 1024m, 2),
                        Fecha = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                    });
                }

                orden.RutasArchivos = JsonSerializer.Serialize(archivosActuales);
                await _context.SaveChangesAsync();
                return Json(new { tipo = "success", mensaje = "Archivos subidos correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error al subir archivos: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerArchivosOrden(int idOrden)
        {
            try
            {
                var orden = await _context.OrdenCompra.FirstOrDefaultAsync(o => o.Id == idOrden);
                if (orden == null || string.IsNullOrWhiteSpace(orden.RutasArchivos))
                {
                    return Json(new List<object>());
                }

                var archivos = JsonSerializer.Deserialize<List<ArchivoOrdenItemDto>>(orden.RutasArchivos) ?? new List<ArchivoOrdenItemDto>();

                var respuesta = archivos
                    .Select(a =>
                    {
                        var archivoGuardado = !string.IsNullOrWhiteSpace(a.Archivo)
                            ? a.Archivo
                            : Path.GetFileName(Uri.UnescapeDataString(a.Url ?? string.Empty));

                        return new
                        {
                            nombre = a.Nombre,
                            archivo = archivoGuardado,
                            url = $"/uploads/ordenes/{idOrden}/{Uri.EscapeDataString(archivoGuardado)}",
                            tamanoKb = a.TamanoKb,
                            fecha = a.Fecha
                        };
                    })
                    .Where(a => !string.IsNullOrWhiteSpace(a.archivo))
                    .OrderByDescending(a => ParseFechaOrden(a.fecha))
                    .ToList();

                return Json(respuesta);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = "Error al obtener archivos: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EliminarArchivoOrden([FromBody] EliminarArchivoOrdenDto datos)
        {
            try
            {
                if (datos == null || datos.IdOrden <= 0 || string.IsNullOrWhiteSpace(datos.Archivo))
                {
                    return Json(new { tipo = "warning", mensaje = "Datos inválidos para eliminar archivo" });
                }

                var orden = await _context.OrdenCompra.FirstOrDefaultAsync(o => o.Id == datos.IdOrden);
                if (orden == null || string.IsNullOrWhiteSpace(orden.RutasArchivos))
                {
                    return Json(new { tipo = "warning", mensaje = "La orden no tiene adjuntos" });
                }

                var archivos = JsonSerializer.Deserialize<List<ArchivoOrdenItemDto>>(orden.RutasArchivos) ?? new List<ArchivoOrdenItemDto>();
                                var archivoAEliminar = archivos.FirstOrDefault(a =>
                {
                    var archivoGuardado = !string.IsNullOrWhiteSpace(a.Archivo)
                        ? a.Archivo
                        : Path.GetFileName(Uri.UnescapeDataString(a.Url ?? string.Empty));

                    return string.Equals(archivoGuardado, datos.Archivo, StringComparison.OrdinalIgnoreCase);
                });

                if (archivoAEliminar == null)
                {
                    return Json(new { tipo = "warning", mensaje = "No se encontró el archivo en la orden" });
                }

                archivos.Remove(archivoAEliminar);
                orden.RutasArchivos = archivos.Count == 0 ? null : JsonSerializer.Serialize(archivos);
                await _context.SaveChangesAsync();

                var rutaLocal = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "ordenes", datos.IdOrden.ToString(), datos.Archivo);
                if (System.IO.File.Exists(rutaLocal))
                {
                    System.IO.File.Delete(rutaLocal);
                }

                return Json(new { tipo = "success", mensaje = "Archivo eliminado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error al eliminar archivo: " + ex.Message });
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
                                           CantidadSolicitada = ds.Cantidad,
                                           Cantidad = sp.Cantidad ?? ds.Cantidad, 
                                           Precio = sp.Precio,
                                           IdSolicitud = ds.IdSolicitud,
                                           Proveedor = ds.Proveedor,
                                           UltimoPrecio = ds.UltimoPrecio,
                                           FultimoPrecio = ds.FultimoPrecio,
                                           EsStock = sp.EsStock ?? false
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
                        orden.Aprobador,
                        orden.IdAreaCorrespondencia,
                        orden.CorrespondeAsc,
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
                var productosValidos = (datos.Productos ?? new List<ProductoOrdenDto>())
                    .Where(p => p.IdDetalleSolicitud > 0)
                    .ToList();

                if (!productosValidos.Any())
                {
                    return Json(new { tipo = "warning", mensaje = "Debe mantener al menos un producto válido para actualizar la orden" });
                }

                var orden = await _context.OrdenCompra
                    .FirstOrDefaultAsync(o => o.Id == datos.IdOrden);

                if (orden == null)
                {
                    return Json(new { tipo = "error", mensaje = "Orden no encontrada" });
                }

                var aprobadorId = await ObtenerAprobadorId(datos.Aprobador);
                if (string.IsNullOrEmpty(aprobadorId))
                {
                    return Json(new { tipo = "warning", mensaje = "Debe seleccionar un APROVADOR válido" });
                }

                orden.Fecha = DateTime.TryParse(datos.Fecha, out var fecha) ? fecha : orden.Fecha;
                orden.TipoCambio = await ObtenerTipoCambioTextoPorFechaAsync(DateTime.TryParse(datos.Fecha, out var fechaTcActualizar) ? fechaTcActualizar : (orden.Fecha ?? DateTime.Now));
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
                orden.IdAreaCorrespondencia = datos.IdAreaCorrespondencia;
                orden.CorrespondeAsc = datos.CorrespondeAsc;
                orden.Telefono = datos.Telefono;
                orden.NomContacto = !string.IsNullOrWhiteSpace(datos.Contacto)
                    ? datos.Contacto
                    : datos.NomContacto;
                orden.Aprobador = aprobadorId;

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
                    if (!int.TryParse(precioAntiguo.IdDetalleSolicitud, out var idDetalleAntiguo))
                    {
                        continue;
                    }

                    if (!productosValidos.Any(p => p.IdDetalleSolicitud == idDetalleAntiguo))
                    {
                        var detalleProducto = await _context.DetalleSolicitudes.FindAsync(idDetalleAntiguo);
                        if (detalleProducto != null)
                        {
                            detalleProducto.Estado = "Creado";
                        }
                    }
                }

                _context.SolicitudPrecio.RemoveRange(preciosAntiguos);

                foreach (var producto in productosValidos)
                {
                    var solicitudPrecio = new SolicitudPrecio
                    {
                        IdSolicitudPrecio = idSolicitudPrecioActual,
                        IdDetalleSolicitud = producto.IdDetalleSolicitud.ToString(),
                        Precio = producto.Precio,
                        Cantidad = producto.Cantidad,
                        EsStock = producto.EsStock
                    };
                    _context.SolicitudPrecio.Add(solicitudPrecio);

                    var detalleProducto = await _context.DetalleSolicitudes
                        .FindAsync(producto.IdDetalleSolicitud);
                    if (detalleProducto != null)
                    {
                        detalleProducto.Estado = producto.EsStock ? "En Stock" : "Pendiente";
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



       
        private async Task<List<(string Email, string Nombre)>> ObtenerSolicitantesOrdenAsync(OrdenCompra orden)
        {
            var idsSolicitudes = await (
                from sp in _context.SolicitudPrecio
                where sp.IdSolicitudPrecio == orden.IdSolicitudPrecio
                join ds in _context.DetalleSolicitudes
                    on sp.IdDetalleSolicitud equals ds.Id.ToString()
                select ds.IdSolicitud
            ).Distinct().ToListAsync();

            var nombresSolicitantes = await _context.Solicitudes
                .Where(s => idsSolicitudes.Contains(s.Id))
                .Select(s => s.Solicitante)
                .Distinct().ToListAsync();

            return await _context.Usuarios
                .Where(u => nombresSolicitantes.Contains(u.Nombre)
                         && !string.IsNullOrEmpty(u.Email))
                .Select(u => new ValueTuple<string, string>(u.Email!, u.NomCompleto ?? u.Nombre))
                .ToListAsync();
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado([FromBody] CambioEstadoDto datos)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var orden = await _context.OrdenCompra.FindAsync(datos.IdOrden);

                if (orden == null)
                    return Json(new { tipo = "error", mensaje = "Orden no encontrada" });

                var estadoAnterior = orden.IdEstadoSolicitud;
                orden.IdEstadoSolicitud = datos.NuevoEstado;

                // Si se anula, guardar observación
                if (datos.NuevoEstado == 11 && !string.IsNullOrEmpty(datos.Observacion))
                {
                    orden.Observacion = datos.Observacion;
                }

                if ((datos.NuevoEstado >= 3 && estadoAnterior < 3 && orden.IdSolicitudPrecio.HasValue)
                    || datos.NuevoEstado == 11)
                {
                    var solicitudesPrecio = await _context.SolicitudPrecio
                        .Where(sp => sp.IdSolicitudPrecio == orden.IdSolicitudPrecio)
                        .ToListAsync();

                    foreach (var sp in solicitudesPrecio)
                    {
                        if (int.TryParse(sp.IdDetalleSolicitud, out var idDetalle))
                        {
                            var detalle = await _context.DetalleSolicitudes.FindAsync(idDetalle);
                            if (detalle != null)
                            {
                                if (datos.NuevoEstado == 11)
                                {
                                    detalle.Estado = "Rechazado";
                                }
                                else if (sp.EsStock == true)
                                {
                                    detalle.Estado = "En Stock";
                                }
                                else
                                {
                                    detalle.Estado = "Aprobado";
                                    detalle.Faprobado = DateTime.Now;
                                }
                            }
                        }
                    }
                }

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

                try
                {
                    var proveedor = await _context.SolicitudPrecio
                        .Where(sp => sp.IdSolicitudPrecio == orden.IdSolicitudPrecio)
                        .Join(_context.DetalleSolicitudes,
                            sp => sp.IdDetalleSolicitud,
                            ds => ds.Id.ToString(),
                            (sp, ds) => ds.Proveedor)
                        .FirstOrDefaultAsync();

                    var destinatarios = new List<(string Email, string Nombre)>();
                    var solicitantes = await ObtenerSolicitantesOrdenAsync(orden);

                    if (datos.NuevoEstado == 2)
                    {
                        Usuario? aprobador = null;
                        if (int.TryParse(orden.Aprobador, out var idAprobador))
                        {
                            aprobador = await _context.Usuarios.FindAsync(idAprobador);
                        }

                        if (!string.IsNullOrEmpty(aprobador?.Email))
                        {
                            destinatarios.Add((aprobador.Email, aprobador.NomCompleto ?? aprobador.Nombre));
                        }

                        if (destinatarios.Any())
                        {

                            var solicitantesTexto = string.Join(", ", solicitantes.Select(s => s.Nombre));
                            await _emailService.EnviarAsync(
                                destinatarios,
                                $"Orden #{datos.IdOrden} requiere su Pre-autorización",
                                EmailService.NotificacionPreAutorizacion(
                                    datos.IdOrden.ToString(),
                                    proveedor ?? "-",
                                    DateTime.Now.ToString("dd/MM/yyyy"),
                                    solicitantesTexto
                                ),
                                TipoNotificacion.PreAutorizacion
                            );
                        }
                    }
                    else if (datos.NuevoEstado == 3 || datos.NuevoEstado == 9 || datos.NuevoEstado == 11)
                    {
                        destinatarios.AddRange(solicitantes);

                        var creador = await _context.Usuarios
                            .Where(u => u.Nombre == orden.Solicitante && !string.IsNullOrEmpty(u.Email))
                            .Select(u => new { u.Email, u.NomCompleto, u.Nombre })
                            .FirstOrDefaultAsync();
                        if (creador != null)
                        {
                            destinatarios.Add((creador.Email!, creador.NomCompleto ?? creador.Nombre));
                        }

                        destinatarios = destinatarios
                            .GroupBy(d => d.Email, StringComparer.OrdinalIgnoreCase)
                            .Select(g => g.First())
                            .ToList();

                        if (destinatarios.Any())
                        {
                            if (datos.NuevoEstado == 3)
                            {
                                await _emailService.EnviarAsync(
                                    destinatarios,
                                    $"Orden #{datos.IdOrden} ha sido APROBADA",
                                    EmailService.NotificacionAprobacion(datos.IdOrden.ToString(), proveedor ?? "-", DateTime.Now.ToString("dd/MM/yyyy")),
                                    TipoNotificacion.Aprobacion
                                );
                            }
                            else if (datos.NuevoEstado == 9)
                            {
                                await _emailService.EnviarAsync(
                                    destinatarios,
                                    $"Orden #{datos.IdOrden} ha sido recibida en almacén",
                                    EmailService.NotificacionRecepcion(
                                        datos.IdOrden.ToString(),
                                        proveedor ?? "-",
                                        DateTime.Now.ToString("dd/MM/yyyy")
                                    ),
                                    TipoNotificacion.Recepcion
                                );
                            }
                            else if (datos.NuevoEstado == 11)
                            {

                                await _emailService.EnviarAsync(
                                    destinatarios,
                                    $"Orden #{datos.IdOrden} ha sido RECHAZADA",
                                    EmailService.NotificacionRechazo(
                                        datos.IdOrden.ToString(),
                                        proveedor ?? "-",
                                        datos.Observacion ?? "Sin motivo"
                                    ),
                                    TipoNotificacion.Rechazo
                                );
                            }
                        }
                    }
                }
                catch
                {
                    // No interrumpir el flujo principal.
                }

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
            var solicitantesTexto = "";
            var solicitantesTextoAutor = "";

            if (idSolicitudPrecio > 0)
            {
                var productos = await (from sp in _context.SolicitudPrecio
                                       where sp.IdSolicitudPrecio == idSolicitudPrecio
                                       join ds in _context.DetalleSolicitudes
                                       on sp.IdDetalleSolicitud equals ds.Id.ToString()
                                       select new
                                       {
                                           Detalle = ds,
                                           Precio = sp.Precio,
                                           Cantidad = sp.Cantidad ?? ds.Cantidad,
                                           EsStock = sp.EsStock ?? false
                                       }).ToListAsync();

                if (productos.Any())
                {
                    proveedor = productos.First().Detalle.Proveedor ?? "";
                }

                // Obtener todos los solicitantes únicos
                // Solo productos que NO son stock
                var productosNoStock = productos.Where(p => !p.EsStock).ToList();

                // Obtener solicitantes únicos solo de productos no stock
                var idSolicitudes = productosNoStock.Select(p => p.Detalle.IdSolicitud).Distinct().ToList();
                var solicitantesNombres = await _context.Solicitudes
                    .Where(s => idSolicitudes.Contains(s.Id))
                    .Select(s => s.Solicitante)
                    .Distinct()
                    .ToListAsync();

                var solicitantesConNombre = await _context.Usuarios
                    .Where(u => solicitantesNombres.Contains(u.Nombre))
                    .Select(u => (u.NomCompleto != null && u.NomCompleto != "")
                        ? u.Nombre + " - " + u.NomCompleto
                        : u.Nombre)
                    .ToListAsync();

                solicitantesTexto = string.Join(", ", solicitantesConNombre.Where(s => !string.IsNullOrEmpty(s)));
                solicitantesTextoAutor = string.Join("\n", solicitantesConNombre.Where(s => !string.IsNullOrEmpty(s)));

                var index = 1;
                foreach (var producto in productos.Where(p => !p.EsStock))
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
                        Cantidad = producto.Cantidad,
                        Precio = producto.Precio,
                        UltimoPrecio = producto.Detalle.UltimoPrecio,
                        FultimoPrecio = producto.Detalle.FultimoPrecio
                    });
                    index++;
                }
            }
            // Obtener usuario que creó la orden
            var usuarioOrden = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Nombre == orden.Solicitante);
            var elaboradoPor = usuarioOrden?.NomCompleto != null && usuarioOrden.NomCompleto != ""
                ? usuarioOrden.Nombre + " - " + usuarioOrden.NomCompleto
                : orden.Solicitante ?? "";
            // Obtener usuario que cambió al estado 3 (Aprobación OC)
            var historialAprobacion = await _context.HistorialEstadoOrden
                .FirstOrDefaultAsync(h => h.IdOrden == orden.Id && h.IdEstadoNuevo == 3);

            var autorizadoPor = "";
            if (historialAprobacion != null)
            {
                var usuarioAutorizo = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Nombre == historialAprobacion.Usuario);
                autorizadoPor = usuarioAutorizo?.NomCompleto != null && usuarioAutorizo.NomCompleto != ""
                    ? usuarioAutorizo.Nombre + " - " + usuarioAutorizo.NomCompleto
                    : historialAprobacion.Usuario ?? "";
            }
            // Revisado Por (estado 2 - Pre-autorizado)
            var historialRevision = await _context.HistorialEstadoOrden
                .FirstOrDefaultAsync(h => h.IdOrden == orden.Id && h.IdEstadoNuevo == 2);

            var revisadoPor = "";
            if (historialRevision != null)
            {
                var usuarioReviso = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Nombre == historialRevision.Usuario);
                revisadoPor = usuarioReviso?.NomCompleto != null && usuarioReviso.NomCompleto != ""
                    ? usuarioReviso.Nombre + " - " + usuarioReviso.NomCompleto
                    : historialRevision.Usuario ?? "";
            }
            var aprobadorTexto = "";
            if (!string.IsNullOrWhiteSpace(orden.Aprobador))
            {
                Usuario? usuarioAprobador = null;
                if (int.TryParse(orden.Aprobador, out var idAprobador))
                {
                    usuarioAprobador = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == idAprobador);
                }
                if (usuarioAprobador == null)
                {
                    usuarioAprobador = await _context.Usuarios.FirstOrDefaultAsync(u => u.Nombre == orden.Aprobador);
                }

                if (usuarioAprobador != null)
                {
                    aprobadorTexto = !string.IsNullOrWhiteSpace(usuarioAprobador.NomCompleto)
                        ? usuarioAprobador.Nombre + " - " + usuarioAprobador.NomCompleto
                        : usuarioAprobador.Nombre;
                }
                else
                {
                    aprobadorTexto = orden.Aprobador;
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
                Solicitante = solicitantesTexto,
                ElaboradoPor = !string.IsNullOrEmpty(solicitantesTextoAutor) ? solicitantesTextoAutor : elaboradoPor,
                AutorizadoPor = autorizadoPor,
                RevisadoPor = revisadoPor,
                Aprobador = aprobadorTexto,
                Rol = "",
                IdAreaCorrespondencia = orden.IdAreaCorrespondencia ?? 0,
                CorrespondeAsc = orden.CorrespondeAsc ?? "",
                Referencia = orden.Referencia,
                Tc = await ObtenerTipoCambioTextoPorFechaAsync(orden.Fecha ?? DateTime.Now),
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
                Productos = productosLista,
                Id = orden.Id
            };
        }
        private async Task<string> ObtenerTipoCambioTextoPorFechaAsync(DateTime fecha)
        {
            var fechaConsulta = fecha.Date;

            var registro = await _context.TipoCambioFecha
                .Where(t => (t.Estado == "1" || t.Estado == "ACTIVO") && t.FechaInicio <= fechaConsulta && t.FechaFin >= fechaConsulta)
                .OrderByDescending(t => t.FechaInicio)
                .FirstOrDefaultAsync();

            var valor = registro?.Valor ?? TipoCambioPorDefecto;
            return valor.ToString("0.####");
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

        private async Task<string?> ObtenerAprobadorId(string? valorAprobador)
        {
            if (string.IsNullOrWhiteSpace(valorAprobador))
            {
                return null;
            }

            Usuario? usuario = null;
            if (int.TryParse(valorAprobador, out var idAprobador))
            {
                usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Id == idAprobador && u.IdTipo == "GERENCIA");
            }
            else
            {
                usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Nombre == valorAprobador && u.IdTipo == "GERENCIA");
            }

            return usuario?.Id.ToString();
        }

        private static DateTime ParseFechaOrden(string? fecha)
        {
            if (DateTime.TryParseExact(fecha, "dd/MM/yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out var fechaParseada))
            {
                return fechaParseada;
            }
            return DateTime.MinValue;
        }

        #endregion
    }

    #region DTOs

    public class ArchivoOrdenItemDto
    {
        public string Nombre { get; set; }
        public string? Url { get; set; }
        public string? Archivo { get; set; }
        public decimal TamanoKb { get; set; }
        public string Fecha { get; set; }
    }

    public class EliminarArchivoOrdenDto
    {
        public int IdOrden { get; set; }
        public string Archivo { get; set; }
    }

    public class OrdenCompraDto
    {
        public int Id { get; set; }
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
        public string ElaboradoPor { get; set; }
        public string AutorizadoPor { get; set; }
        public string RevisadoPor { get; set; }
        public string Aprobador { get; set; }
        public int IdAreaCorrespondencia { get; set; }
        public string CorrespondeAsc { get; set; }
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

        public decimal CantidadSolicitada { get; set; }
        public decimal? UltimoPrecio { get; set; } 
        public DateTime? FultimoPrecio { get; set; }
        public bool EsStock { get; set; }
    }

    public class CambioEstadoDto
    {
        public int IdOrden { get; set; }
        public int NuevoEstado { get; set; }
        public string? Observacion { get; set; }
    }

    public class GenerarPdfDto
    {
        public int IdOrden { get; set; }
    }

    #endregion
}
