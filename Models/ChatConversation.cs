using System;
using System.Collections.Generic;

namespace MyBackend.Models
{
    public class ChatConversation
    {
        public string ChannelName { get; set; }
        public string OtherUserName { get; set; }
        public string OtherUserId { get; set; }
        public DateTime LastMessageTime { get; set; }
        public List<ChatMessage> Messages { get; set; }
        public int TotalMessages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}