using Microsoft.AspNetCore.Mvc;
using orion.Models;
using orion.Recursos;
using orion.Servicios.Contrato;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace orion.Controllers
{


    public class InicioController : Controller
    {
        private readonly IUsuarioService _usuarioServicio;
        public InicioController(IUsuarioService usuarioServicio)
        {
            _usuarioServicio = usuarioServicio;
        }

        public IActionResult IniciarSesion()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> IniciarSesion(string Nombre, string contraseña)
        {
            Usuario usuario_encontrado = await _usuarioServicio.GetUsuarios(Nombre, Utilidades.EncriptarClave(contraseña));

            if (usuario_encontrado == null)
            {
                ViewData["Mensaje"] = "No se encontraron coincidencias";
                return View();
            }
            if (usuario_encontrado.Estado == "1")
            {
                List<Claim> claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.Name, usuario_encontrado.Nombre),
                    new Claim(ClaimTypes.Role, usuario_encontrado.IdTipo)
                };

                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                AuthenticationProperties properties = new AuthenticationProperties()
                {
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    properties
                );

                // REEMPLAZA el RedirectToAction por esto:
                return usuario_encontrado.IdTipo switch
                {
                    "GERENCIA" => RedirectToAction("Index", "Orden"),
                    "CONTABILIDAD" => RedirectToAction("Index", "TipoCambio"),
                    "COMPRAS" => RedirectToAction("Index", "Orden"),
                    "ALMACEN" => RedirectToAction("Index", "Orden"),
                    "ADMINISTRADOR" => RedirectToAction("Index", "Solicitudes"),
                    _ => RedirectToAction("Index", "Solicitudes")
                };
            }
            else
            {
                ViewData["Mensaje"] = "USUARIO INACTIVO";
                return View();
            }
        }
    }
}
