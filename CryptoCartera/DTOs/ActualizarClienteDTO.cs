using System.ComponentModel.DataAnnotations;

namespace CryptoCartera.DTOs
{
    public class ActualizarClienteDTO
    {
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string? Name { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Formato de email inválido.")]
        [StringLength(255, ErrorMessage = "El email no puede superar los 255 caracteres.")]
        public string? Email { get; set; } = string.Empty;
    }
}