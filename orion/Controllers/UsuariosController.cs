
using Microsoft.AspNetCore.Mvc;
using orion.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using orion.Recursos;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace orion.Controllers

{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly OrionContext _context;
        public UsuariosController(OrionContext context)
        {
            _context = context;
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
        public IActionResult Guardar([FromForm] Usuario model)
        {
            try
            {
                if (model.Id == 0) // Nuevo usuario
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
                        Contraseña = Utilidades.EncriptarClave(model.Contraseña), // siempre encriptada
                        IdTipo = model.IdTipo,
                        Estado = model.Estado,
                        NomCompleto = model.NomCompleto,
                        Idusuario = model.Idusuario
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

                    // Solo actualizar contraseña si viene con valor nuevo
                    if (!string.IsNullOrEmpty(model.Contraseña))
                    {
                        usuarioExistente.Contraseña = Utilidades.EncriptarClave(model.Contraseña);
                    }

                    _context.SaveChanges();
                    return Json(new { tipo = "success", mensaje = "Usuario actualizado con éxito" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { tipo = "error", mensaje = "Error en servidor: " + ex.Message });
            }
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