namespace orion.Servicios
{
    public interface IEmailService
    {
        Task EnviarAsync(
            List<(string Email, string Nombre)> destinatarios,
            string asunto,
            string cuerpoHtml
        );
    }
}
