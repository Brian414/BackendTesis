using System;
using System.Collections.Generic;

namespace MyBackend.Models
{
    public class ChatMessage
    {
        public Guid Id { get; set; }
        public string ChannelName { get; set; }
        public string Text { get; set; }
        public string FromUserId { get; set; }
        public string ToUserId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; }
    }
    
    
}