using System.ComponentModel.DataAnnotations;

namespace CryptoCartera.Models
{
    public class Cliente
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Mail incorrecto")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        public ICollection<Transaccion> Transacciones { get; set; } = new List<Transaccion>();
    }
}