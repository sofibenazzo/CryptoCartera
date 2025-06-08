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
            var clienteDTO = await _context.Clientes.Select(c => new ClienteDTO
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email
            }).ToListAsync();
            return Ok(clienteDTO);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ClienteDTO>> Get(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            return cliente == null ? NotFound() : Ok(new ClienteDTO
            {
                Id = cliente.Id,
                Name = cliente.Name,
                Email = cliente.Email
            });
        }

        [HttpPost]
        public async Task<ActionResult<ClienteDTO>> Post([FromBody] CrearClienteDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if(string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("El nombre es obligatorio");

            if (string.IsNullOrWhiteSpace(dto.Email) || !dto.Email.Contains("@"))
                return BadRequest("Debe ingresar un email válido");

            if (await _context.Clientes.AnyAsync(c => c.Email == dto.Email))
                return BadRequest("Ya existe un cliente con ese email");

            var cliente = new Cliente
            {
                Name= dto.Name,
                Email = dto.Email
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = cliente.Id }, new ClienteDTO
            {
                Id = cliente.Id,
                Name = cliente.Name,
                Email = cliente.Email
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] ActualizarClienteDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) && string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("Debe proporcionar al menos un campo para actualizar.");

            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null) 
                return NotFound();

            if (!string.IsNullOrEmpty(dto.Name))
                cliente.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Email))
                cliente.Email = dto.Email;

            _context.Entry(cliente).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
            {
                return NotFound();
            }
           
            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}