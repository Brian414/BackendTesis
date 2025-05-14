using System;
using System.Collections.Generic;

namespace MyBackend.Models
{
    public class ChatMessage
    {
        public Guid Id { get; set; }
        public required string ChannelName { get; set; }
        public required string Text { get; set; }
        public required string FromUserId { get; set; }
        public required string ToUserId { get; set; }
        public required DateTime Timestamp { get; set; }
        public required string Source { get; set; }
    }
    
    
}