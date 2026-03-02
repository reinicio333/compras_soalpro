using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using orion.Models;

namespace orion.Controllers
{
    [Authorize(Roles = "ADMINISTRADOR,CONTABILIDAD,CONTADURIA")]
    public class AreasCorrespondenciaController : Controller
    {
        private readonly OrionContext _context;

        public AreasCorrespondenciaController(OrionContext context)
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
            var areas = await _context.AreasCorrespondencia
                .OrderBy(a => a.Nombre)
                .Select(a => new
                {
                    a.Id,
                    a.Nombre,
                    Estado = a.Estado == "A" ? "ACTIVO" : "INACTIVO"
                })
                .ToListAsync();

            return Json(areas);
        }

        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var area = await _context.AreasCorrespondencia.FirstOrDefaultAsync(a => a.Id == id);
            if (area == null)
            {
                return Json(new { tipo = "error", mensaje = "Área no encontrada" });
            }

            return Json(new
            {
                area.Id,
                area.Nombre,
                Estado = area.Estado ?? "A"
            });
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] AreaCorrespondencia model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Nombre))
                {
                    return Json(new { tipo = "warning", mensaje = "El nombre del área es obligatorio" });
                }

                var nombreNormalizado = model.Nombre.Trim().ToUpper();

                var existe = await _context.AreasCorrespondencia.AnyAsync(a =>
                    a.Id != model.Id && a.Nombre != null && a.Nombre.ToUpper() == nombreNormalizado);

                if (existe)
                {
                    return Json(new { tipo = "warning", mensaje = "Ya existe un área con ese nombre" });
                }

                if (model.Id == 0)
                {
                    model.Nombre = nombreNormalizado;
                    model.Estado = string.IsNullOrWhiteSpace(model.Estado) ? "A" : model.Estado;
                    _context.AreasCorrespondencia.Add(model);
                    await _context.SaveChangesAsync();
                    return Json(new { tipo = "success", mensaje = "Área registrada correctamente" });
                }

                var actual = await _context.AreasCorrespondencia.FirstOrDefaultAsync(a => a.Id == model.Id);
                if (actual == null)
                {
                    return Json(new { tipo = "error", mensaje = "Área no encontrada" });
                }

                actual.Nombre = nombreNormalizado;
                actual.Estado = string.IsNullOrWhiteSpace(model.Estado) ? "A" : model.Estado;

                await _context.SaveChangesAsync();
                return Json(new { tipo = "success", mensaje = "Área actualizada correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error en servidor: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
        {
            var area = await _context.AreasCorrespondencia.FirstOrDefaultAsync(a => a.Id == id);
            if (area == null)
            {
                return Json(new { tipo = "warning", mensaje = "Área no encontrada" });
            }

            var enUso = await _context.OrdenCompra.AnyAsync(o => o.IdAreaCorrespondencia == id);
            if (enUso)
            {
                return Json(new { tipo = "warning", mensaje = "No se puede eliminar: el área ya está asociada a órdenes" });
            }

            _context.AreasCorrespondencia.Remove(area);
            await _context.SaveChangesAsync();
            return Json(new { tipo = "success", mensaje = "Área eliminada" });
        }
    }
}
