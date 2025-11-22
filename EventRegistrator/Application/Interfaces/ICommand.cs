using EventRegistrator.Application.DTOs;
using EventRegistrator.Domain.Entities;

namespace EventRegistrator.Application.Interfaces
{
    public interface ICommand
    {
        Task<List<Response>> Execute(MessageDTO message, UserAdmin user = null);
    }
}
