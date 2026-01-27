using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Domain.Interfaces;
using EventRegistrator.Infrastructure.Persistence;

namespace EventRegistrator.Application.Commands
{
    [Command("/say", "Уведомить всех юзеров")]
    public class AdminNotificationCommand : AdminCommandBase
    {
        private readonly IUserRepository _userRepository;

        public AdminNotificationCommand(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        protected async override Task<List<Response>> ExecuteAdminCommand(MessageDTO message)
        {
            var text = message.Text.Replace("/say", string.Empty);
            var rep = _userRepository as UserRepository;
            var users = rep.GetAllUsers();
            List<Response> result = new();
            foreach (var user in users)
            {
                result.Add(new Response { ChatId = user.Id, Text = text });
            }
            return result;
        }
    }
}
