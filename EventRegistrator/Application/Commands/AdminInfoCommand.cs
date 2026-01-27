using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Domain.Interfaces;
using EventRegistrator.Infrastructure.Persistence;
using EventRegistrator.Infrastructure.Utils;

namespace EventRegistrator.Application.Commands
{
    [Command("/admin", "Информация по последним ивентам")]
    public class AdminInfoCommand : AdminCommandBase
    {
        private readonly IUserRepository _userRepository;

        public AdminInfoCommand(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        protected async override Task<List<Response>> ExecuteAdminCommand(MessageDTO message)
        {
            var text2 = TextFormatter.GetAllUsersInfo(_userRepository as UserRepository);
            return [new Response { ChatId = message.ChatId, Text = text2 }];
        }
    }
}
