// SendToConsultantRequest.cs
namespace MyBackend.Models.Requests
{
    public class SendToConsultantRequest
    {
        public string ConsultantId { get; set; } // ID del consultor (1-10)
        public string Text { get; set; }
    }
}

// RespondToClientRequest.cs
namespace MyBackend.Models.Requests
{
    public class RespondToClientRequest
    {
        public string ClientId { get; set; }
        public string Text { get; set; }
    }
}