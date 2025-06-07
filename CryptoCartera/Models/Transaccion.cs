using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoCartera.Models
{
    public class Transaccion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CryptoCode { get; set; } = string.Empty;

        [Required]
        [Range(0.0001, double.MaxValue, ErrorMessage = "No se permiten valores negativos ni cero")]
        public decimal CryptoAmount { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "No se permiten valores negativos ni cero")]
        public decimal Money { get; set; }

        [Required]
        [RegularExpression("purchase|sale", ErrorMessage = "La acción debe ser 'purchase' o 'sale'.")]
        public string Action { get; set; }

        [Required]
        public DateTime DateTime { get; set; } = DateTime.UtcNow;

        //Relacion con el Cliente
        [Required(ErrorMessage = "El cliente es obligatorio")]
        [ForeignKey("Cliente")]
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }
    }
}
