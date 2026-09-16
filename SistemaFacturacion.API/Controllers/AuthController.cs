using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.API.Data;
using SistemaFacturacion.Core.Entities;

namespace SistemaFacturacion.API.Controllers
{
    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly FacturacionDbContext _db;

        public AuthController(FacturacionDbContext db)
        {
            _db = db;
        }

        [HttpPost("login")]
        public async Task<ActionResult<Usuario>> Login([FromBody] LoginDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Username))
            {
                return BadRequest("Debe especificar usuario y contraseña.");
            }

            var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Username.ToLower() == dto.Username.ToLower() && u.Activo);
            if (usuario == null || usuario.PasswordHash != dto.Password)
            {
                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });
            }

            return Ok(usuario);
        }

        [HttpGet("usuarios")]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            return await _db.Usuarios.ToListAsync();
        }

        [HttpPost("usuarios")]
        public async Task<ActionResult<Usuario>> CrearUsuario([FromBody] Usuario usuario)
        {
            if (usuario == null || string.IsNullOrWhiteSpace(usuario.Username) || string.IsNullOrWhiteSpace(usuario.Nombre))
            {
                return BadRequest("El nombre y el nombre de usuario son obligatorios.");
            }

            var existente = await _db.Usuarios.AnyAsync(u => u.Username.ToLower() == usuario.Username.ToLower());
            if (existente)
            {
                return BadRequest($"El nombre de usuario '{usuario.Username}' ya está registrado.");
            }

            if (string.IsNullOrEmpty(usuario.PasswordHash))
            {
                usuario.PasswordHash = "123456";
            }

            usuario.Activo = true;
            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();

            return Ok(usuario);
        }

        [HttpDelete("usuarios/{id}")]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            _db.Usuarios.Remove(usuario);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Usuario eliminado correctamente." });
        }
    }
}
