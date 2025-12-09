namespace CryptoCartera.DTOs
{
    public class CarteraDTO
    {
        public class CarteraItemDTO
        {
            public string CryptoCode { get; set; } = string.Empty;
            public decimal CryptoAmount { get; set; }
            public decimal ValueARS { get; set; }
            public decimal? PriceARS { get; set; }
        }

        public class CarteraResumenDTO
        {
            public int ClienteId { get; set; }
            public string ClienteNombre { get; set; } = string.Empty;
            public List<CarteraItemDTO> Items { get; set; } = new();
            public decimal TotalARS => Items.Sum(i => i.ValueARS);
        }
    }
}
