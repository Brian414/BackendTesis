using System;
using System.Collections.Generic;

namespace MyBackend.Models
{
    public class ChatConversation
    {
        public required string ChannelName { get; set; }
        public required string OtherUserName { get; set; }
        public required string OtherUserId { get; set; }
        public DateTime LastMessageTime { get; set; }
        public required List<ChatMessage> Messages { get; set; }
        public int TotalMessages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}