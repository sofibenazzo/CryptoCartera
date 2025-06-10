using System.ComponentModel.DataAnnotations;

namespace CryptoCartera.DTOs
{
    public class ClienteDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class CrearClienteDTO
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
}