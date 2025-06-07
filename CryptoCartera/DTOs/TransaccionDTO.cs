using System.ComponentModel.DataAnnotations;

namespace CryptoCartera.DTOs
{
    public class TransaccionDTO
    {
        public int Id { get; set; }
        public string CryptoCode { get; set; } // ejemplos, "bitcoin", "usdc"
        public string Action { get; set; } // "purchase" o "sale"
        public decimal CryptoAmount { get; set; }
        public decimal Money { get; set; } // pesos
        public DateTime DateTime { get; set; } = DateTime.UtcNow;
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; }
        public string ClienteEmail { get; set; }
    }

    public class CrearTransaccionDTO
    {
        [Required]
        public string CryptoCode { get; set; }

        [Required]
        [RegularExpression("purchase|sale", ErrorMessage = "La acción debe ser 'purchase' o 'sale'.")]
        public string Action { get; set; }

        [Required]
        [Range(0.0001, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
        public decimal CryptoAmount { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required]
        public DateTime DateTime { get; set; } = DateTime.UtcNow;
    }
}
