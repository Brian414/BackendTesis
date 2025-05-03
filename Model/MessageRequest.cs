namespace MyBackend.Models.Requests
{
    public class MessageRequest
    {
        public string ClientId { get; set; }  // ID del cliente (obsoleto)
        public string Text { get; set; }
    }
}