using Microsoft.EntityFrameworkCore;
using orion.Models;
using orion.Servicios.Contrato;

namespace orion.Servicios.Implementacion
{
    public class UsuarioService : IUsuarioService
    {
        private readonly OrionContext _dbContext;
        public UsuarioService(OrionContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<Usuario> GetUsuarios(string Nombre, string contraseña)
        {
            Usuario usuario_encontrado = await _dbContext.Usuarios.Where(u => u.Nombre == Nombre && u.Contraseña == contraseña).
                FirstOrDefaultAsync();

            return usuario_encontrado;
        }

        public async Task<Usuario> SaveUsuario(Usuario modelo)
        {
            _dbContext.Usuarios.Add(modelo);
            await _dbContext.SaveChangesAsync();
            return modelo;
        }
    }
}
