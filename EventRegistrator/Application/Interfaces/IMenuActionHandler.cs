using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Enums;
using EventRegistrator.Domain.Entities;

namespace EventRegistrator.Application.Interfaces
{
    public interface IMenuActionHandler
    {
        Task<List<Response>> Handle(MenuAction action, MessageDTO message, UserAdmin user);
    }
}
