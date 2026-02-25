using Microsoft.EntityFrameworkCore;
using orion.Models;


namespace orion.Servicios.Contrato
{
    public interface IUsuarioService
    {

        Task<Usuario> GetUsuarios(string Nombre, string contraseña);

        Task<Usuario> SaveUsuario(Usuario modelo);
    }
}
