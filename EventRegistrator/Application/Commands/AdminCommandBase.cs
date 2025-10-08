using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Infrastructure.Config;

namespace EventRegistrator.Application.Commands
{
    public abstract class AdminCommandBase : ICommand
    {
        public async Task<List<Response>> Execute(MessageDTO message, UserAdmin user)
        {
            if (EnvLoader.GetAdminId() == message.UserId)
            {
                user.LastMessageId = null;
                user.ClearStateHistory();
                var response = await ExecuteAdminCommand(message);
                Console.WriteLine("всего ответов " + response.Count);
                //if (response.Count != 1)
                //{
                //    Console.WriteLine("Ошибка. Нетипичный ответ");
                //    return [];
                //}
                //response.First().SaveMessageIdCallback = id => user.LastMessageId = id;
                return response;
            }
            return [];
        }

        protected abstract Task<List<Response>> ExecuteAdminCommand(MessageDTO message);
    }
}
