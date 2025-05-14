using System;

namespace MyBackend.Models
{
    public class Conversation
    {
        public int Id { get; set; }
        public required string ClientId { get; set; }
        public required string ConsultantId { get; set; }
        public required string ChannelName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}