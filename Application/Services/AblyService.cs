using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IO.Ably; // Esta es la referencia correcta
using MyBackend.Models;

namespace MyBackend.Services
{
    public class AblyService
    {
        private readonly AblyRealtime _realtime;
        private readonly AblyRest _rest;

        public AblyService(string apiKey)
        {
            var clientOptions = new ClientOptions(apiKey);
            _realtime = new AblyRealtime(clientOptions);
            _rest = new AblyRest(clientOptions);
        }

        public async Task SendMessageAsync(string channelName, object message)
        {
            var channel = _realtime.Channels.Get(channelName);
            await channel.PublishAsync("message", message);
        }
        
        public async Task<List<ChatMessage>> GetChannelHistoryAsync(string channelName)
        {
            var channel = _rest.Channels.Get(channelName);
            var historyPage = await channel.HistoryAsync(new PaginatedRequestParams { Limit = 100 });
            
            var messages = new List<ChatMessage>();
            
            // Verificar si hay elementos en historyPage.Items
            if (historyPage.Items.Count() == 0)
            {
                // No hay mensajes en este canal
                return messages;
            }
            
            foreach (var message in historyPage.Items)
            {
                try
                {
                    var data = message.Data as Newtonsoft.Json.Linq.JObject;
                    if (data != null)
                    {
                        var (clientId, consultantId) = ChatChannelService.ParseChannelName(channelName);
                        
                        var fromUserId = data["from"]?.ToString();
                        var toUserId = fromUserId == clientId ? consultantId : clientId;
                        
                        messages.Add(new ChatMessage
                        {
                            Id = Guid.Parse(message.Id), // Convertir string a Guid
                            ChannelName = channelName,
                            Text = data["text"]?.ToString(),
                            FromUserId = fromUserId,
                            ToUserId = toUserId,
                            Timestamp = message.Timestamp.HasValue ? message.Timestamp.Value.DateTime.ToUniversalTime() : DateTime.UtcNow,
                            Source = "Ably"
                        });
                    }
                }
                catch (Exception)
                {
                    // Ignorar mensajes con formato incorrecto
                    continue;
                }
            }
            
            return messages;
        }
    }
}