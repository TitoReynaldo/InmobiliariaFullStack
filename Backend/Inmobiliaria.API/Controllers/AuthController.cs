using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Inmobiliaria.API.Models;
using Inmobiliaria.API.DTOs.Auth;
using Microsoft.AspNetCore.Identity;

namespace Inmobiliaria.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly InmobiliariaContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(InmobiliariaContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto request)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest("El nombre de usuario ya existe.");
            }

            var passwordHasher = new PasswordHasher<Usuario>();
            var user = new Usuario
            {
                Username = request.Username,
                Rol = request.Rol,
                FechaRegistro = DateTime.Now
            };

            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

            _context.Usuarios.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuario registrado exitosamente." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto request)
        {
            var user = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                return Unauthorized(new { message = "Credenciales incorrectas." });
            }

            var passwordHasher = new PasswordHasher<Usuario>();
            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Credenciales incorrectas." });
            }

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token = token,
                username = user.Username,
                rol = user.Rol
            });
        }

        private string GenerateJwtToken(Usuario user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");

            var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("La clave JWT no está configurada en appsettings.json.");
            var key = Encoding.ASCII.GetBytes(jwtKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UsuarioId.ToString()),

                new Claim(ClaimTypes.Name, user.Username ?? string.Empty),

                new Claim(ClaimTypes.Role, user.Rol ?? "Cliente")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(4),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

    }
}