using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json.Linq;
using MyBackend.Services;
using Microsoft.Extensions.Configuration;
using MyBackend.Models.Requests;

namespace MyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly AblyService _ablyService;
        private readonly IConfiguration _config;
        private readonly string _ablySecret;
        private readonly List<string> _predefinedConsultants = new List<string>
        {
            "c81ecbb6-ac62-4e5b-b3ad-71890f3ec22a", 
            "5d05addc-8e42-4fdc-8994-8323b116f91c", 
            "consultor_3",
            "consultor_4", "consultor_5", "consultor_6",
            "consultor_7", "consultor_8", "consultor_9",
            "consultor_10"
        };

        public ChatController(AblyService ablyService, IConfiguration config)
        {
            _ablyService = ablyService;
            _config = config;
            var ablyApiKey = _config["Ably:ApiKey"];
            _ablySecret = ablyApiKey?.Split(':')[1] ?? throw new ArgumentNullException("Ably:ApiKey no configurado");
        }

        [HttpGet("token")]
        public IActionResult GenerateToken()
        {
            var user = GetAuthenticatedUser();
            if (user == null) return Unauthorized("Usuario no autenticado");

            var capabilities = new JObject();

            if (user.IsConsultant)
            {
                // Consultores: Solo reciben mensajes en su canal y responden a canales de clientes
                capabilities[$"consultor_{user.Id}"] = new JArray("subscribe");
                capabilities["cliente_*"] = new JArray("publish");
            }
            else
            {
                // Clientes: Solo envían a consultores específicos y reciben en su canal
                capabilities[$"cliente_{user.Id}"] = new JArray("subscribe");

                // Permite publicar solo en los consultores predefinidos
                foreach (var consultant in _predefinedConsultants)
                {
                    capabilities[consultant] = new JArray("publish");
                }
            }

            var token = GenerateJwtForAbly(user.Id, capabilities);
            return Ok(new { token });
        }

        [HttpPost("send-to-consultant")]
        public async Task<IActionResult> SendToConsultant([FromBody] SendToConsultantRequest request)
        {
            var client = GetAuthenticatedUser();
            if (client == null || client.IsConsultant)
                return Unauthorized("Solo clientes pueden enviar mensajes a consultores");

            if (!_predefinedConsultants.Contains($"consultor_{request.ConsultantId}"))
                return BadRequest("Consultor no válido");

            try
            {
                await _ablyService.SendMessageAsync(
                    channelName: $"consultor_{request.ConsultantId}",
                    message: new
                    {
                        text = request.Text,
                        from = client.Id,
                        timestamp = DateTime.UtcNow
                    }
                );

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("respond-to-client")]
        public async Task<IActionResult> RespondToClient([FromBody] RespondToClientRequest request)
        {
            var consultant = GetAuthenticatedConsultant();
            if (consultant == null)
                return Unauthorized("Solo consultores pueden responder a clientes");

            try
            {
                await _ablyService.SendMessageAsync(
                    channelName: $"cliente_{request.ClientId}",
                    message: new
                    {
                        text = request.Text,
                        from = consultant.Id,
                        timestamp = DateTime.UtcNow
                    }
                );

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private dynamic GetAuthenticatedUser()
        {
            if (!User.Identity.IsAuthenticated) return null;

            return new
            {
                Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                IsConsultant = User.HasClaim("role", "consultor") // Cambiado a "consultor" para consistencia
            };
        }

        private dynamic GetAuthenticatedConsultant()
        {
            var user = GetAuthenticatedUser();
            return user?.IsConsultant == true ? user : null;
        }

        private string GenerateJwtForAbly(string clientId, JObject capabilities)
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_ablySecret));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("sub", clientId),
                new Claim("cap", capabilities.ToString(Newtonsoft.Json.Formatting.None)),
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
