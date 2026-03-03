using orion.Models;
using orion.Recursos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace orion.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly OrionContext _context;
       
        public HomeController(ILogger<HomeController> logger, OrionContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            ClaimsPrincipal claimsuser = HttpContext.User;
            string nombreUsuario = "";
            if (claimsuser.Identity.IsAuthenticated)
            {
                nombreUsuario = claimsuser.Claims.Where(c=>c.Type==ClaimTypes.Name).
                    Select(c=>c.Value).SingleOrDefault();
            }
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> CerrarSesion()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("IniciarSesion", "Inicio");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContrasena(string contraseñaActual, string nuevaContraseña, string confirmarContraseña)
        {
            if (string.IsNullOrWhiteSpace(contraseñaActual) || string.IsNullOrWhiteSpace(nuevaContraseña) || string.IsNullOrWhiteSpace(confirmarContraseña))
            {
                TempData["PasswordMessageType"] = "error";
                TempData["PasswordMessage"] = "Todos los campos de contraseña son obligatorios.";
                return RedirectToRefererOrHome();
            }

            if (nuevaContraseña != confirmarContraseña)
            {
                TempData["PasswordMessageType"] = "error";
                TempData["PasswordMessage"] = "La nueva contraseña y la confirmación no coinciden.";
                return RedirectToRefererOrHome();
            }

            var nombreUsuario = User.Claims.Where(c => c.Type == ClaimTypes.Name).Select(c => c.Value).SingleOrDefault();
            if (string.IsNullOrWhiteSpace(nombreUsuario))
            {
                TempData["PasswordMessageType"] = "error";
                TempData["PasswordMessage"] = "No se pudo identificar el usuario autenticado.";
                return RedirectToRefererOrHome();
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Nombre == nombreUsuario);
            if (usuario == null)
            {
                TempData["PasswordMessageType"] = "error";
                TempData["PasswordMessage"] = "Usuario no encontrado.";
                return RedirectToRefererOrHome();
            }

            if (usuario.Contraseña != Utilidades.EncriptarClave(contraseñaActual))
            {
                TempData["PasswordMessageType"] = "error";
                TempData["PasswordMessage"] = "La contraseña actual es incorrecta.";
                return RedirectToRefererOrHome();
            }

            usuario.Contraseña = Utilidades.EncriptarClave(nuevaContraseña);
            await _context.SaveChangesAsync();

            TempData["PasswordMessageType"] = "success";
            TempData["PasswordMessage"] = "Contraseña actualizada con éxito.";
            return RedirectToRefererOrHome();
        }

        private IActionResult RedirectToRefererOrHome()
        {
            var referer = Request.Headers.Referer.ToString();
            if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
            {
                var localUrl = refererUri.PathAndQuery + refererUri.Fragment;
                if (Url.IsLocalUrl(localUrl))
                {
                    return LocalRedirect(localUrl);
                }
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
