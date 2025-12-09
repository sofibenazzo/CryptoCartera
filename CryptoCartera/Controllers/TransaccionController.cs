using CryptoCartera.DTOs;
using CryptoCartera.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CryptoCartera.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransaccionController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public TransaccionController(AppDbContext context, IHttpClientFactory factory)
        {
            _context = context;
            _httpClient = factory.CreateClient();
        }

        // Método para obtener precio de la criptomoneda según acción (compra o venta)
        private async Task<decimal?> ObtenerPrecioCripto(string cryptoCode, string action)
        {
            if (string.IsNullOrWhiteSpace(cryptoCode)) return null;

            var url = $"https://criptoya.com/api/satoshitango/{cryptoCode.ToLower()}/ars";
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                using var stream = await response.Content.ReadAsStreamAsync();
                using var json = await JsonDocument.ParseAsync(stream);

                // dependiendo de la acción usamos totalAsk o totalBid
                if (action.ToLower() == "purchase")
                {
                    if (!json.RootElement.TryGetProperty("totalAsk", out var precioElement)) return null;
                    return precioElement.GetDecimal();
                }
                else if (action.ToLower() == "sale")
                {
                    if (!json.RootElement.TryGetProperty("totalBid", out var precioElement)) return null;
                    return precioElement.GetDecimal();
                }

                return null;
            }
            catch
            {
                return null; // si algo falla devuelvo null
            }
        }

        // GET: todas las transacciones
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransaccionDTO>>> Get()
        {
            var transacciones = await _context.Transacciones
                                      .Include(t => t.Cliente)
                                      .OrderByDescending(t => t.DateTime)
                                      .ToListAsync();

            var transaccionDTOs = transacciones.Select(t => new TransaccionDTO
            {
                Id = t.Id,
                CryptoCode = t.CryptoCode,
                Action = t.Action,
                CryptoAmount = t.CryptoAmount,
                Money = t.Money,
                DateTime = t.DateTime,
                ClienteId = t.ClienteId,
                ClienteNombre = t.Cliente?.Name ?? string.Empty,
                ClienteEmail = t.Cliente?.Email ?? string.Empty
            }).ToList();

            return Ok(transaccionDTOs);
        }

        // GET: transacción por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<TransaccionDTO>> Get(int id)
        {
            var transaccion = await _context.Transacciones
                .Include(t => t.Cliente)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaccion == null)
                return NotFound();

            var dto = new TransaccionDTO
            {
                Id = transaccion.Id,
                CryptoCode = transaccion.CryptoCode,
                Action = transaccion.Action,
                CryptoAmount = transaccion.CryptoAmount,
                Money = transaccion.Money,
                DateTime = transaccion.DateTime,
                ClienteId = transaccion.ClienteId,
                ClienteNombre = transaccion.Cliente?.Name ?? string.Empty,
                ClienteEmail = transaccion.Cliente?.Email ?? string.Empty
            };

            return Ok(dto);
        }

        // GET: todas las transacciones de un cliente
        [HttpGet("cliente/{clienteId}")]
        public async Task<ActionResult<IEnumerable<TransaccionDTO>>> GetByCliente(int clienteId)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null)
                return NotFound($"Cliente con ID {clienteId} no existe.");

            var transacciones = await _context.Transacciones
                .Include(t => t.Cliente)
                .Where(t => t.ClienteId == clienteId)
                .OrderByDescending(t => t.DateTime)
                .ToListAsync();

            var dtoList = transacciones.Select(t => new TransaccionDTO
            {
                Id = t.Id,
                CryptoCode = t.CryptoCode,
                Action = t.Action,
                CryptoAmount = t.CryptoAmount,
                Money = t.Money,
                DateTime = t.DateTime,
                ClienteId = t.ClienteId,
                ClienteNombre = t.Cliente?.Name ?? "",
                ClienteEmail = t.Cliente?.Email ?? ""
            }).ToList();

            return Ok(dtoList);
        }

        // GET: estado de cartera del cliente
        [HttpGet("estado/{clienteId}")]
        public async Task<ActionResult<CarteraDTO.CarteraResumenDTO>> GetEstado(int clienteId)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null)
                return NotFound($"Cliente con ID {clienteId} no existe.");

            var saldos = await _context.Transacciones
                .Where(t => t.ClienteId == clienteId)
                .GroupBy(t => t.CryptoCode)
                .Select(g => new
                {
                    CryptoCode = g.Key,
                    Amount = g.Sum(t => t.Action == "purchase" ? t.CryptoAmount : -t.CryptoAmount)
                })
                .ToListAsync();

            var items = new List<CarteraDTO.CarteraItemDTO>();

            foreach (var saldo in saldos)
            {
                if (saldo.Amount <= 0) continue;

                decimal? precio = await ObtenerPrecioCripto(saldo.CryptoCode, "purchase");
                if (precio == null) continue;

                items.Add(new CarteraDTO.CarteraItemDTO
                {
                    CryptoCode = saldo.CryptoCode,
                    CryptoAmount = saldo.Amount,
                    PriceARS = precio,
                    ValueARS = saldo.Amount * precio.Value
                });
            }

            var resumen = new CarteraDTO.CarteraResumenDTO
            {
                ClienteId = cliente.Id,
                ClienteNombre = cliente.Name,
                Items = items
            };

            return Ok(resumen);
        }

        // POST: crear transacción
        [HttpPost]
        public async Task<ActionResult<TransaccionDTO>> Post([FromBody] CrearTransaccionDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.CryptoAmount <= 0)
                return BadRequest("La cantidad debe ser mayor a cero.");

            // Validación de acción
            if (dto.Action.ToLower() != "purchase" && dto.Action.ToLower() != "sale")
                return BadRequest("La acción debe ser 'purchase' o 'sale'.");

            var cliente = await _context.Clientes.FindAsync(dto.ClienteId);
            if (cliente == null)
                return BadRequest("El cliente no existe.");

            var precio = await ObtenerPrecioCripto(dto.CryptoCode, dto.Action);
            if (precio == null)
                return BadRequest("No se pudo obtener el precio de la criptomoneda");

            // Verifico saldo si es venta
            if (dto.Action.ToLower() == "sale")
            {
                var saldo = await _context.Transacciones
                    .Where(t => t.ClienteId == dto.ClienteId && t.CryptoCode == dto.CryptoCode)
                    .SumAsync(t => t.Action == "purchase" ? t.CryptoAmount : -t.CryptoAmount);

                if (saldo < dto.CryptoAmount)
                    return BadRequest("No se puede vender más criptomonedas de las que se poseen");
            }

            var transaccion = new Transaccion
            {
                CryptoCode = dto.CryptoCode.ToLower(),
                Action = dto.Action.ToLower(),
                CryptoAmount = dto.CryptoAmount,
                Money = dto.CryptoAmount * precio.Value,
                DateTime = dto.DateTime == default ? DateTime.Now : dto.DateTime,
                ClienteId = dto.ClienteId
            };

            _context.Transacciones.Add(transaccion);
            await _context.SaveChangesAsync();

            var transaccionDTO = new TransaccionDTO
            {
                Id = transaccion.Id,
                CryptoCode = transaccion.CryptoCode,
                Action = transaccion.Action,
                CryptoAmount = transaccion.CryptoAmount,
                Money = transaccion.Money,
                DateTime = transaccion.DateTime,
                ClienteId = cliente.Id,
                ClienteNombre = cliente.Name,
                ClienteEmail = cliente.Email
            };

            return CreatedAtAction(nameof(Get), new { id = transaccion.Id }, transaccionDTO);
        }

        // PUT: actualizar transacción
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, CrearTransaccionDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var transaccion = await _context.Transacciones.FindAsync(id);
            if (transaccion == null)
                return NotFound();

            var precio = await ObtenerPrecioCripto(dto.CryptoCode, dto.Action);
            if (precio == null)
                return BadRequest("Error al obtener el precio.");

            var saldoDisponible = await _context.Transacciones
                .Where(t => t.ClienteId == transaccion.ClienteId && t.CryptoCode == dto.CryptoCode && t.Id != id)
                .SumAsync(t => t.Action.ToLower() == "purchase" ? t.CryptoAmount : -t.CryptoAmount);

            if (dto.Action.ToLower() == "sale" && dto.CryptoAmount > saldoDisponible)
                return BadRequest("Saldo insuficiente.");

            // Actualizo campos
            transaccion.CryptoCode = dto.CryptoCode.ToLower();
            transaccion.Action = dto.Action.ToLower();
            transaccion.CryptoAmount = dto.CryptoAmount;
            transaccion.Money = dto.CryptoAmount * precio.Value;
            transaccion.DateTime = dto.DateTime == default ? DateTime.Now : dto.DateTime;

            _context.Entry(transaccion).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent(); // no devuelvo nada, solo confirmo
        }

        // DELETE: eliminar transacción
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var transaccion = await _context.Transacciones.FindAsync(id);
            if (transaccion == null)
                return NotFound();

            _context.Transacciones.Remove(transaccion);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
