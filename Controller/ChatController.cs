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
using MyBackend.Models;
using Microsoft.EntityFrameworkCore; // Agregamos esta referencia para ToListAsync

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
                
                // Crear objeto de mensaje
                var messageData = new
                {
                    text = request.Text,
                    from = client.Id,
                    timestamp = DateTime.UtcNow
                };
                
                // Enviar a Ably
                await _ablyService.SendMessageAsync(channelName, messageData);
                
                // Guardar en la base de datos
                var chatMessage = new Models.ChatMessage
                {
                    Id = Guid.NewGuid(),
                    ChannelName = channelName,
                    Text = request.Text,
                    FromUserId = client.Id,
                    ToUserId = request.ConsultantId,
                    Timestamp = DateTime.UtcNow,
                    Source = "Local"
                };
                
                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();

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
                
                // Crear objeto de mensaje
                var messageData = new
                {
                    text = request.Text,
                    from = consultant.Id,
                    timestamp = DateTime.UtcNow
                };
                
                // Enviar a Ably
                await _ablyService.SendMessageAsync(channelName, messageData);
                
                // Guardar en la base de datos
                var chatMessage = new Models.ChatMessage
                {
                    Id = Guid.NewGuid(),
                    ChannelName = channelName,
                    Text = request.Text,
                    FromUserId = consultant.Id,
                    ToUserId = request.ClientId,
                    Timestamp = DateTime.UtcNow,
                    Source = "Local"
                };
                
                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, channelName });
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException != null ? 
                    $" Inner exception: {ex.InnerException.Message}" : "";
                return StatusCode(500, new { 
                    error = ex.Message, 
                    innerDetails = innerException,
                    stack = ex.StackTrace 
                });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetChatHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var user = GetAuthenticatedUser();
            if (user == null)
                return Unauthorized("Usuario no autenticado");

            try
            {
                // Convertimos el tipo dinámico a un tipo concreto
                string userId = user.Id;
                bool isConsultant = user.IsConsultant;

                // Obtener canales con mensajes desde la base de datos
                var channelsWithMessages = await _context.ChatMessages
                    .Where(m => m.FromUserId == userId || m.ToUserId == userId)
                    .Select(m => m.ChannelName)
                    .Distinct()
                    .ToListAsync();

                // Obtener los IDs de usuarios con los que se ha tenido conversaciones
                var userIdsWithConversations = new HashSet<Guid>();
                foreach (var channelName in channelsWithMessages)
                {
                    var (clientId, consultantId) = ChatChannelService.ParseChannelName(channelName);
                    var otherUserId = userId == clientId ? consultantId : clientId;
                    
                    // Convertir el string a Guid antes de agregarlo al HashSet
                    if (Guid.TryParse(otherUserId, out Guid otherUserGuid))
                    {
                        userIdsWithConversations.Add(otherUserGuid);
                    }
                }

                // Obtener información de usuarios con los que se ha tenido conversaciones
                var relevantUsers = await _context.Users
                    .Where(u => userIdsWithConversations.Contains(u.UserId))
                    .ToListAsync();

                // Información de depuración
                var debugInfo = new
                {
                    UserId = userId,
                    IsConsultant = isConsultant,
                    TotalRelevantUsers = relevantUsers.Count,
                    ChannelsWithMessages = channelsWithMessages
                };

                var userChannelsInfo = relevantUsers
                    .Select(u => new
                    {
                        UserId = u.UserId.ToString(),
                        UserName = u.Name,
                        ChannelName = isConsultant ? 
                            ChatChannelService.GenerateChannelName(u.UserId.ToString(), userId) :
                            ChatChannelService.GenerateChannelName(userId, u.UserId.ToString())
                    })
                    .ToList();

                var conversations = new List<ChatConversation>();

                foreach (var channel in userChannelsInfo)
                {
                    // Obtener mensajes de la base de datos (con paginación)
                    var dbMessages = await _context.ChatMessages
                        .Where(m => m.ChannelName == channel.ChannelName)
                        .OrderByDescending(m => m.Timestamp)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

                    // Obtener historial de Ably para este canal (como respaldo)
                    var ablyMessages = await _ablyService.GetChannelHistoryAsync(channel.ChannelName);
                    
                    // Combinar mensajes de ambas fuentes (eliminando duplicados por Id)
                  var allMessages = dbMessages.ToList();
foreach (var ablyMsg in ablyMessages)
{
    // Comparar ambos como strings
    if (!allMessages.Any(m => m.Id.ToString() == ablyMsg.Id.ToString()))
    {
        allMessages.Add(ablyMsg);
    }
}
                    
                    // Ordenar por fecha
                    allMessages = allMessages.OrderByDescending(m => m.Timestamp).ToList();
                    
                    // Agregar la conversación si hay mensajes
                    if (allMessages.Any())
                    {
                        conversations.Add(new ChatConversation
                        {
                            ChannelName = channel.ChannelName,
                            OtherUserName = channel.UserName,
                            OtherUserId = channel.UserId,
                            LastMessageTime = allMessages.Max(m => m.Timestamp),
                            Messages = allMessages,
                            TotalMessages = await _context.ChatMessages
                                .Where(m => m.ChannelName == channel.ChannelName)
                                .CountAsync(),
                            CurrentPage = page,
                            PageSize = pageSize
                        });
                    }
                }

                // Ordenar conversaciones por la fecha del último mensaje (más reciente primero)
conversations = conversations
    .OrderByDescending(c => c.LastMessageTime)
    .ToList();

                // Devolver información con paginación
                return Ok(new { 
                    Debug = debugInfo,
                    Conversations = conversations,
                    TotalConversations = conversations.Count,
                    CurrentPage = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
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
