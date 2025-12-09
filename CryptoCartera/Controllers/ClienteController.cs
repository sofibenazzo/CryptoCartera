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

        [HttpPost]
        public async Task<ActionResult<ClienteDTO>> Post([FromBody] CrearClienteDTO dto)
        {
            // Validaciones básicas
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("El nombre es obligatorio"); // nombre obligatorio

            if (string.IsNullOrWhiteSpace(dto.Email) || !dto.Email.Contains("@"))
                return BadRequest("Debe ingresar un email válido"); // email debe ser válido

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

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, [FromBody] ActualizarClienteDTO dto)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
                return NotFound($"Cliente con ID {id} no encontrado.");

            if (string.IsNullOrWhiteSpace(dto.Name) && string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("Debe proporcionar al menos un campo para actualizar.");

            // Verifico que no haya duplicado de email en otro cliente
            if (!string.IsNullOrEmpty(dto.Email) &&
                await _context.Clientes.AnyAsync(c => c.Email == dto.Email && c.Id != id))
                return BadRequest("Ya existe un cliente con ese correo.");

            // Actualizo campos si vienen en el DTO
            if (!string.IsNullOrEmpty(dto.Name))
                cliente.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Email))
                cliente.Email = dto.Email;

            await _context.SaveChangesAsync();

            // Devuelvo el cliente actualizado
            var result = new ClienteDTO
            {
                Id = cliente.Id,
                Name = cliente.Name,
                Email = cliente.Email
            };

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
                return NotFound();

            // No se puede borrar si tiene transacciones
            if (await _context.Transacciones.AnyAsync(t => t.ClienteId == id))
                return BadRequest("No se puede eliminar el cliente, porque tiene transacciones");

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
