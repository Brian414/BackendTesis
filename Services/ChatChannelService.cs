using System;
using System.Collections.Generic;
using System.Linq;

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
        
        public static List<string> GetUserChannels(string userId, bool isConsultant)
        {
            // Si es consultor, los canales tienen el formato chat:*:userId
            // Si es cliente, los canales tienen el formato chat:userId:*
            if (isConsultant)
            {
                return new List<string> { $"chat:*:{userId}" };
            }
            else
            {
                return new List<string> { $"chat:{userId}:*" };
            }
        }
        
        public static bool IsUserInChannel(string userId, string channelName, bool isConsultant)
        {
            try
            {
                var (clientId, consultantId) = ParseChannelName(channelName);
                
                if (isConsultant)
                {
                    return consultantId == userId;
                }
                else
                {
                    return clientId == userId;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}