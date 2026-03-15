using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduSyncAI.WebAPI.Data;
using EduSyncAI.WebAPI.Models;
using System.Security.Cryptography;
using System.Text;

namespace EduSyncAI.WebAPI.Controllers
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly EduSyncDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(EduSyncDbContext context, ILogger<AuthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var lecturer = await _context.Lecturers
                    .FirstOrDefaultAsync(l => l.Username == request.Username || l.Email == request.Username);

                if (lecturer == null)
                {
                    return Unauthorized(new { error = "Invalid username or password" });
                }

                // Check if lecturer is active
                if (!lecturer.IsActive)
                {
                    return Unauthorized(new { error = "Account is not active. Please contact administrator." });
                }

                // Verify password
                if (!VerifyPassword(request.Password, lecturer.PasswordHash))
                {
                    return Unauthorized(new { error = "Invalid username or password" });
                }

                // Generate JWT token
                var token = GenerateJwtToken(lecturer);

                // Return token and user info
                return Ok(new
                {
                    token = token,
                    user = new
                    {
                        id = lecturer.Id,
                        username = lecturer.Username,
                        fullName = lecturer.FullName,
                        email = lecturer.Email
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { error = "Login failed" });
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Check if username already exists
                var existing = await _context.Lecturers
                    .FirstOrDefaultAsync(l => l.Username == request.Username);

                if (existing != null)
                {
                    return BadRequest(new { error = "Username already exists" });
                }

                // Create new lecturer
                var lecturer = new Lecturer
                {
                    Username = request.Username,
                    FullName = request.FullName,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    IsActive = true
                };

                _context.Lecturers.Add(lecturer);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Lecturer registered successfully", id = lecturer.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new { error = "Registration failed" });
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private string GenerateJwtToken(Lecturer lecturer)
        {
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "EduSyncAI-Super-Secret-Key-For-JWT-Authentication-Min-32-Chars";
            var jwtIssuer = "EduSyncAI";
            var jwtAudience = "EduSyncAI-Web";
            var expirationHours = 24;

            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, lecturer.Id.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, lecturer.Username),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, lecturer.Email),
                new System.Security.Claims.Claim("FullName", lecturer.FullName)
            };

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expirationHours),
                signingCredentials: credentials
            );

            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == hash;
        }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
