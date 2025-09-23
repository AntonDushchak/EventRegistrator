using EventRegistrator.Application.DTOs;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Entities;

namespace EventRegistrator.Application.Interfaces
{
    public interface IState
    {
        Task<Response> Handle(MessageDTO message, UserAdmin user);
        Task<List<Response>> Execute(MessageDTO message, UserAdmin user);
    }
}
