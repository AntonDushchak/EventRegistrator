using EventRegistrator.Application.DTOs;

namespace EventRegistrator.Application.Interfaces
{
    public interface IHandler
    {
        Task<List<Response>> HandleAsync(MessageDTO message);
        bool CanHandle(MessageDTO message);
    }
}
