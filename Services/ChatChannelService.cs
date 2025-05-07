using System;

namespace MyBackend.Services
{
    public static class ChatChannelService
    {
        public static string GenerateChannelName(string clientId, string consultantId)
        {
            // Ordenamos los IDs para garantizar consistencia
            var orderedIds = new[] { clientId, consultantId }.OrderBy(id => id).ToArray();
            return $"chat:{orderedIds[0]}:{orderedIds[1]}";
        }

        public static (string clientId, string consultantId) ParseChannelName(string channelName)
        {
            var parts = channelName.Split(':');
            if (parts.Length != 3 || parts[0] != "chat")
                throw new ArgumentException("Formato de canal inválido");
                
            return (parts[1], parts[2]);
        }
    }
}