using System.Security.Cryptography;
using System.Text;

namespace orion.Recursos
{
    public class Utilidades
    {
        public static string EncriptarClave(string contraseña)
        {
            StringBuilder sb = new StringBuilder();
            using (SHA256 hash = SHA256.Create())
            {
                Encoding enc = Encoding.UTF8;

                byte[] result = hash.ComputeHash(enc.GetBytes(contraseña));

                foreach (byte b in result)
                    sb.Append(b.ToString("x2"));

            }
            return sb.ToString();
        }
    }
}
