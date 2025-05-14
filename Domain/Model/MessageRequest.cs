namespace MyBackend.Models.Requests
{
    public class MessageRequest
    {
        public required string ClientId { get; set; }  
        public required string Text { get; set; }
    }
}