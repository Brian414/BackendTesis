using System;

namespace MyBackend.Models
{
    public class Conversation
    {
        public int Id { get; set; }
        public string ClientId { get; set; }
        public string ConsultantId { get; set; }
        public string ChannelName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}