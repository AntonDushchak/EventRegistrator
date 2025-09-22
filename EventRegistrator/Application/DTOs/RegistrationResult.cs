using EventRegistrator.Domain.Entities;

namespace EventRegistrator.Application.DTOs
{
    public class RegistrationResult
    {
        public bool Success { get; set; }
        public Event Event { get; set; }
        public List<int> MessageIds { get; set; }
    }
}
