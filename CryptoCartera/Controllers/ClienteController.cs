using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CryptoCartera.Models;
using Microsoft.EntityFrameworkCore;
using CryptoCartera.DTOs;

namespace CryptoCartera.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClienteController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClienteController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Cliente
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClienteDTO>>> Get()
        {
            // Traigo todos los clientes y los convierto a DTO para no exponer info sensible
            var clienteDTO = await _context.Clientes.Select(c => new ClienteDTO
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email
            }).ToListAsync();

            return Ok(clienteDTO);
        }

        //GET: api/cliente/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> Get(int id)
        {
            // Traigo el cliente con sus transacciones
            var cliente = await _context.Clientes
                .Include(c => c.Transacciones)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
                return NotFound(); // si no existe el cliente

            // Devuelvo cliente + transacciones (con fecha formateada)
            return Ok(new
            {
                cliente.Id,
                cliente.Name,
                cliente.Email,

                Transacciones = cliente.Transacciones.Select(t => new
                {
                    t.Id,
                    t.CryptoCode,
                    t.Action,
                    t.CryptoAmount,
                    t.Money,
                    DateTime = t.DateTime.ToString("yyyy-MM-dd HH:mm")
                })
            });
        }

        //POST: api/cliente
        [HttpPost]
        public async Task<ActionResult<ClienteDTO>> Post([FromBody] CrearClienteDTO dto)
        {
            // Validaciones básicas
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verifico que no exista otro cliente con el mismo email
            if (await _context.Clientes.AnyAsync(c => c.Email == dto.Email))
                return BadRequest("Ya existe un cliente con ese email");

            var cliente = new Cliente
            {
                Name = dto.Name,
                Email = dto.Email
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            // Devuelvo el cliente creado
            return CreatedAtAction(nameof(Get), new { id = cliente.Id }, new ClienteDTO
            {
                Id = cliente.Id,
                Name = cliente.Name,
                Email = cliente.Email
            });
        }

        //PATCH: api/cliente/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, [FromBody] ActualizarClienteDTO dto)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound($"Cliente con ID {id} no encontrado");

            // Si no mandó nada
            if (dto.Name == null && dto.Email == null)
                return BadRequest("Debe proporcionar al menos un campo para actualizar");

            // Validación de email duplicado
            if (dto.Email != null &&
                await _context.Clientes.AnyAsync(c => c.Email == dto.Email && c.Id != id))
                return BadRequest("Ya existe un cliente con ese email.");

            // Actualizaciones 
            if (dto.Name != null)
                cliente.Name = dto.Name;

            if (dto.Email != null)
                cliente.Email = dto.Email;

            await _context.SaveChangesAsync();

            return Ok(new ClienteDTO
            {
                Id = cliente.Id,
                Name = cliente.Name,
                Email = cliente.Email
            });
        }

        //DELETE: api/cliente/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound($"Cliente con ID {id} no encontrado");

            // No se puede borrar si tiene transacciones
            if (await _context.Transacciones.AnyAsync(t => t.ClienteId == id))
                return BadRequest("No se puede eliminar el cliente, porque tiene transacciones");

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
