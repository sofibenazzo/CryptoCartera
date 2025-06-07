using CryptoCartera.DTOs;
using CryptoCartera.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
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

        private async Task<decimal?> ObtenerPrecioCripto(string CryptoCode)
        {
            var url = $"https://criptoya.com/api/satoshitango/{CryptoCode}/ars";
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                using var stream = await response.Content.ReadAsStreamAsync();
                using var json = await JsonDocument.ParseAsync(stream);

                if (!json.RootElement.TryGetProperty("totalAsk", out var precioElement)) return null;

                return precioElement.GetDecimal();
            }
            catch
            {
                return null;
            }
        }

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

        [HttpPost]
        public async Task<ActionResult<TransaccionDTO>> Post([FromBody] CrearTransaccionDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.CryptoAmount <= 0)
                return BadRequest("La cantidad debe ser mayor a cero.");

            if (dto.Action.ToLower() != "purchase" && dto.Action.ToLower() != "sale")
                return BadRequest("La acción debe ser 'purchase' o 'sale'.");

            var cliente = await _context.Clientes.FindAsync(dto.ClienteId);
            if (cliente == null)
                return BadRequest("El cliente no existe.");

            // Obtener el precio desde CriptoYa
            string url = $"https://criptoya.com/api/satoshitango/{dto.CryptoCode.ToLower()}/ars";
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return BadRequest("No se pudo obtener el precio de la criptomoneda.");
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al consultar el precio de la criptomoneda.");
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(stream);

            if (!json.RootElement.TryGetProperty("totalAsk", out var precioElement))
                return BadRequest("Respuesta inválida de la API. No contiene el precio.");

            decimal precio = precioElement.GetDecimal();

            // Validar saldo si es venta
            if (dto.Action.ToLower() == "sale")
            {
                var historial = await _context.Transacciones
                    .Where(t => t.ClienteId == dto.ClienteId && t.CryptoCode == dto.CryptoCode)
                    .ToListAsync();

                var totalCompra = historial
                    .Where(t => t.Action == "purchase")
                    .Sum(t => t.CryptoAmount);

                var totalVenta = historial
                    .Where(t => t.Action == "sale")
                    .Sum(t => t.CryptoAmount);

                if (dto.CryptoAmount > (totalCompra - totalVenta))
                    return BadRequest("Saldo insuficiente para realizar la venta.");
            }

            var transaccion = new Transaccion
            {
                CryptoCode = dto.CryptoCode.ToLower(),
                Action = dto.Action.ToLower(),
                CryptoAmount = dto.CryptoAmount,
                Money = dto.CryptoAmount * precio,
                DateTime = DateTime.Now,
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

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, CrearTransaccionDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var transaccion = await _context.Transacciones.FindAsync(id);
            if (transaccion == null)
                return NotFound();

            var precio = await ObtenerPrecioCripto(dto.CryptoCode);
            if (precio == null)
                return BadRequest("Error al obtener el precio.");

            var saldoDisponible = await _context.Transacciones
                .Where(t => t.ClienteId == transaccion.ClienteId && t.CryptoCode == dto.CryptoCode && t.Id != id)
                .GroupBy(t => t.CryptoCode)
                .Select(g => g.Sum(t => t.Action.ToLower() == "purchase" ? t.CryptoAmount : -t.CryptoAmount))
                .FirstOrDefaultAsync();

            if (dto.Action.ToLower() == "sale" && dto.CryptoAmount > saldoDisponible)
                return BadRequest("Saldo insuficiente.");

            transaccion.CryptoCode = dto.CryptoCode;
            transaccion.Action = dto.Action;
            transaccion.CryptoAmount = dto.CryptoAmount;
            transaccion.Money = dto.CryptoAmount * precio.Value;
            transaccion.DateTime = dto.DateTime == default ? DateTime.UtcNow : dto.DateTime;

            _context.Entry(transaccion).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

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