

namespace MyBackend.Models.Requests
{
    public class SendToConsultantRequest
    {
        public required string ConsultantId { get; set; }
        public required string Text { get; set; }
    }

    public class RespondToClientRequest
    {
        public required string ClientId { get; set; }
        public required string Text { get; set; }
    }
}