using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json.Linq;
using MyBackend.Services;
using Microsoft.Extensions.Configuration;
using MyBackend.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using MyBackend.DataBase;

namespace MyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly AblyService _ablyService;
        private readonly IConfiguration _config;
        private readonly string _ablySecret;
        private readonly DBContext _context;

        public ChatController(AblyService ablyService, IConfiguration config, DBContext context)
        {
            _ablyService = ablyService;
            _config = config;
            _context = context;
            var ablyApiKey = _config["Ably:ApiKey"];
            _ablySecret = ablyApiKey?.Split(':')[1] ?? throw new ArgumentNullException("Ably:ApiKey no configurado");
        }

        [HttpGet("token")]
        public IActionResult GenerateToken()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
            {
                return Unauthorized("No se encontró el token de autorización");
            }

            var user = GetAuthenticatedUser();
            if (user == null)
            {
                return Unauthorized("Usuario no autenticado");
            }

            var capabilities = new JObject();
            string userChannelPattern = user.IsConsultant ? 
                $"chat:*:{user.Id}" : // Para consultores
                $"chat:{user.Id}:*";  // Para clientes

            capabilities[userChannelPattern] = new JArray("publish", "subscribe");

            var token = GenerateJwtForAbly(user.Id, capabilities);
            return Ok(new { token });
        }

        [HttpGet("channel/{consultantId}")]
        public IActionResult GetChannelName(string consultantId)
        {
            var user = GetAuthenticatedUser();
            if (user == null)
                return Unauthorized("Usuario no autenticado");

            string channelName = ChatChannelService.GenerateChannelName(user.Id, consultantId);
            return Ok(new { channelName });
        }

        [HttpPost("send-to-consultant")]
        public async Task<IActionResult> SendToConsultant([FromBody] SendToConsultantRequest request)
        {
            var client = GetAuthenticatedUser();
            if (client == null || client.IsConsultant)
                return Unauthorized("Solo clientes pueden enviar mensajes a consultores");

            try
            {
                string channelName = ChatChannelService.GenerateChannelName(client.Id, request.ConsultantId);
                await _ablyService.SendMessageAsync(
                    channelName: channelName,
                    message: new
                    {
                        text = request.Text,
                        from = client.Id,
                        timestamp = DateTime.UtcNow
                    }
                );

                return Ok(new { success = true, channelName });
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
                string channelName = ChatChannelService.GenerateChannelName(request.ClientId, consultant.Id);
                await _ablyService.SendMessageAsync(
                    channelName: channelName,
                    message: new
                    {
                        text = request.Text,
                        from = consultant.Id,
                        timestamp = DateTime.UtcNow
                    }
                );

                return Ok(new { success = true, channelName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private dynamic GetAuthenticatedUser()
        {
            if (!User.Identity.IsAuthenticated) return null;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var esConsultor = User.FindFirst("EsConsultor")?.Value == "True";

            return new
            {
                Id = userId,
                IsConsultant = esConsultor
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
