using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using orion.Models;

namespace orion.Controllers
{
    [Authorize]
    public class TipoCambioController : Controller
    {
        private readonly OrionContext _context;
        private const decimal TipoCambioPorDefecto = 6.96m;

        public TipoCambioController(OrionContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var tiposCambio = await _context.TipoCambioFecha
                .OrderByDescending(t => t.FechaInicio)
                .Select(t => new
                {
                    t.Id,
                    FechaInicio = t.FechaInicio.ToString("yyyy-MM-dd"),
                    FechaFin = t.FechaFin.ToString("yyyy-MM-dd"),
                    t.Valor,
                    t.Estado
                })
                .ToListAsync();

            return Json(tiposCambio);
        }

        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var registro = await _context.TipoCambioFecha.FirstOrDefaultAsync(t => t.Id == id);
            if (registro == null)
            {
                return Json(new { tipo = "error", mensaje = "Registro no encontrado" });
            }

            return Json(new
            {
                registro.Id,
                FechaInicio = registro.FechaInicio.ToString("yyyy-MM-dd"),
                FechaFin = registro.FechaFin.ToString("yyyy-MM-dd"),
                registro.Valor,
                registro.Estado
            });
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] TipoCambioFecha model)
        {
            try
            {
                if (model.FechaInicio > model.FechaFin)
                {
                    return Json(new { tipo = "warning", mensaje = "La fecha inicio no puede ser mayor a la fecha fin" });
                }

                var fechaInicio = model.FechaInicio.Date;
                var fechaFin = model.FechaFin.Date;

                var hayCruce = await _context.TipoCambioFecha.AnyAsync(t =>
                    t.Id != model.Id &&
                    (t.Estado == "1" || t.Estado == "ACTIVO") &&
                    fechaInicio <= t.FechaFin &&
                    fechaFin >= t.FechaInicio);

                if (hayCruce)
                {
                    return Json(new { tipo = "warning", mensaje = "Ya existe un tipo de cambio para un rango de fechas que se cruza" });
                }

                if (model.Id == 0)
                {
                    model.FechaInicio = fechaInicio;
                    model.FechaFin = fechaFin;
                    model.Estado ??= "1";
                    _context.TipoCambioFecha.Add(model);
                    await _context.SaveChangesAsync();
                    return Json(new { tipo = "success", mensaje = "Tipo de cambio registrado correctamente" });
                }

                var actual = await _context.TipoCambioFecha.FirstOrDefaultAsync(t => t.Id == model.Id);
                if (actual == null)
                {
                    return Json(new { tipo = "error", mensaje = "Registro no encontrado" });
                }

                actual.FechaInicio = fechaInicio;
                actual.FechaFin = fechaFin;
                actual.Valor = model.Valor;
                actual.Estado = string.IsNullOrWhiteSpace(model.Estado) ? "1" : model.Estado;

                await _context.SaveChangesAsync();
                return Json(new { tipo = "success", mensaje = "Tipo de cambio actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error en servidor: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
        {
            var registro = await _context.TipoCambioFecha.FirstOrDefaultAsync(t => t.Id == id);
            if (registro == null)
            {
                return Json(new { tipo = "warning", mensaje = "Registro no encontrado" });
            }

            _context.TipoCambioFecha.Remove(registro);
            await _context.SaveChangesAsync();
            return Json(new { tipo = "success", mensaje = "Tipo de cambio eliminado" });
        }

        [HttpGet]
        public async Task<IActionResult> GetTipoCambioPorFecha(string? fecha)
        {
            DateTime fechaConsulta;
            if (!DateTime.TryParse(fecha, out fechaConsulta))
            {
                fechaConsulta = DateTime.Now;
            }

            var fechaFiltro = fechaConsulta.Date;

            var registro = await _context.TipoCambioFecha
                .Where(t => (t.Estado == "1" || t.Estado == "ACTIVO") && t.FechaInicio <= fechaFiltro && t.FechaFin >= fechaFiltro)
                .OrderByDescending(t => t.FechaInicio)
                .FirstOrDefaultAsync();

            var valor = registro?.Valor ?? TipoCambioPorDefecto;

            return Json(new { valor = valor.ToString("0.####") });
        }
    }
}
