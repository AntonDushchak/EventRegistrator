using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Domain.Interfaces;
using EventRegistrator.Infrastructure.Persistence;

namespace EventRegistrator.Application.Commands
{
    [Command("/save", "Сохранение")]
    public class AdminSaveCommand : AdminCommandBase
    {
        private readonly IUserRepository _userRepository;
        private readonly RepositoryLoader _repositoryLoader;

        public AdminSaveCommand(IUserRepository userRepository, RepositoryLoader repositoryLoader)
        {
            _userRepository = userRepository;
            _repositoryLoader = repositoryLoader;
        }

        protected async override Task<List<Response>> ExecuteAdminCommand(MessageDTO message)
        {
            await _repositoryLoader.SaveDataAsync(_userRepository as UserRepository);
            var r = new Response { ChatId = message.ChatId, Text = "Сохранение" };
            return [r];
        }
    }
}
