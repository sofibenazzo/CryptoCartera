using System.ComponentModel.DataAnnotations;

namespace CryptoCartera.DTOs
{
    public class TransaccionDTO
    {
        public int Id { get; set; }
        public string CryptoCode { get; set; } = string.Empty; 
        public string Action { get; set; } = string.Empty; // "purchase" o "sale"
        public decimal CryptoAmount { get; set; }
        public decimal Money { get; set; } // pesos
        public DateTime DateTime { get; set; } = DateTime.UtcNow;
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string ClienteEmail { get; set; } = string.Empty;
    }

    public class CrearTransaccionDTO
    {
        [Required]
        public string CryptoCode { get; set; } = string.Empty; 

        [Required]
        [RegularExpression("purchase|sale", ErrorMessage = "La acción debe ser 'purchase' o 'sale'.")]
        public string Action { get; set; } = string.Empty; 

        [Required]
        [Range(0.0001, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
        public decimal CryptoAmount { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required]
        public DateTime DateTime { get; set; } = DateTime.UtcNow;
    }

    public class ActualizarTransaccionDTO
    {
        public string? CryptoCode { get; set; }

        [RegularExpression("purchase|sale", ErrorMessage = "La acción debe ser 'purchase' o 'sale'.")]
        public string? Action { get; set; }

        [Range(0.0001, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
        public decimal? CryptoAmount { get; set; }

        public int? ClienteId { get; set; }

        public DateTime? DateTime { get; set; }
    }
}
