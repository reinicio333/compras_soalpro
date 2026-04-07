
using Microsoft.AspNetCore.Mvc;
using orion.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using orion.Recursos;
using Microsoft.AspNetCore.Hosting;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace orion.Controllers

{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly OrionContext _context;
        private readonly IWebHostEnvironment _env;

        public UsuariosController(OrionContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ListarUsuarios()
        {
            var usuario = await _context.Usuarios.ToListAsync();
            return Json(usuario);
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] Usuario model, IFormFile? firmaFile)
        {
            try
            {
                string? nuevaRuta = null;
                if (firmaFile != null && firmaFile.Length > 0)
                {
                    var carpeta = Path.Combine(_env.WebRootPath, "uploads", "firmas");
                    Directory.CreateDirectory(carpeta);
                    var nombreArchivo = $"firma_{Guid.NewGuid()}{Path.GetExtension(firmaFile.FileName)}";
                    var rutaCompleta = Path.Combine(carpeta, nombreArchivo);
                    using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                        await firmaFile.CopyToAsync(stream);
                    nuevaRuta = $"/uploads/firmas/{nombreArchivo}";
                }
                if (model.Id == 0)
                {
                    if (_context.Usuarios.Any(d => d.Nombre == model.Nombre))
                    {
                        return Json(new { tipo = "warning", mensaje = "El nombre del usuario ya existe" });
                    }

                    if (string.IsNullOrEmpty(model.Contraseña))
                    {
                        return Json(new { tipo = "warning", mensaje = "La contraseña es obligatoria para un nuevo usuario" });
                    }

                    var nuevoUsuario = new Usuario
                    {
                        Nombre = model.Nombre,
                        Contraseña = Utilidades.EncriptarClave(model.Contraseña),
                        IdTipo = model.IdTipo,
                        Estado = model.Estado,
                        NomCompleto = model.NomCompleto,
                        Idusuario = model.Idusuario,
                        Area = model.Area,
                        Email = model.Email,
                        EmailResponsable = model.EmailResponsable,
                        FirmaPath = nuevaRuta
                    };

                    _context.Usuarios.Add(nuevoUsuario);
                    _context.SaveChanges();

                    return Json(new { tipo = "success", mensaje = "Usuario registrado con éxito" });
                }
                else // Editar usuario existente
                {
                    if (_context.Usuarios.Any(d => d.Nombre == model.Nombre && d.Id != model.Id))
                    {
                        return Json(new { tipo = "warning", mensaje = "El nombre del usuario ya existe en otro registro" });
                    }

                    var usuarioExistente = _context.Usuarios.Find(model.Id);
                    if (usuarioExistente == null)
                    {
                        return Json(new { tipo = "error", mensaje = "Usuario no encontrado" });
                    }

                    usuarioExistente.Nombre = model.Nombre;
                    usuarioExistente.NomCompleto = model.NomCompleto;
                    usuarioExistente.IdTipo = model.IdTipo;
                    usuarioExistente.Estado = model.Estado;
                    usuarioExistente.Idusuario = model.Idusuario;
                    usuarioExistente.Area = model.Area;
                    usuarioExistente.Email = model.Email;
                    usuarioExistente.EmailResponsable = model.EmailResponsable;
                    // Solo actualizar contraseña si viene con valor nuevo
                    if (!string.IsNullOrEmpty(model.Contraseña))
                    {
                        usuarioExistente.Contraseña = Utilidades.EncriptarClave(model.Contraseña);
                    }
                    if (nuevaRuta != null)
                        usuarioExistente.FirmaPath = nuevaRuta;
                    _context.SaveChanges();
                    return Json(new { tipo = "success", mensaje = "Usuario actualizado con éxito" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error en servidor: " + ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> EliminarFirma(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return Json(new { tipo = "error", mensaje = "Usuario no encontrado" });

            if (!string.IsNullOrEmpty(usuario.FirmaPath))
            {
                var rutaFisica = Path.Combine(_env.WebRootPath, usuario.FirmaPath.TrimStart('/'));
                if (System.IO.File.Exists(rutaFisica))
                    System.IO.File.Delete(rutaFisica);
                usuario.FirmaPath = null;
                await _context.SaveChangesAsync();
            }
            return Json(new { tipo = "success", mensaje = "Firma eliminada" });
        }

        public IActionResult GetUsuario(int id)
        {
            var emp = _context.Usuarios.Find(id);
            return Json(emp);
        }

        [HttpGet]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return Json(new { tipo = "warning", mensaje = "ERROR AL USUARIO" });
            }

            _context.Usuarios.Remove(usuario);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return Json(new { tipo = "success", mensaje = "USUARIO ELIMINADO" });
            }
            else
            {
                return Json(new { tipo = "warning", mensaje = "ERROR AL ELIMINAR" });
            }
        }


    }
}