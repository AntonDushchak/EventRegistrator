using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Interfaces;
using EventRegistrator.Infrastructure.Persistence;

namespace EventRegistrator.Application.Commands
{
    [Command("/sync", "Синхронизация кэша с бд")]
    public class AdminSync : AdminCommandBase
    {
        private readonly IUserRepository _userRepository;
        private readonly RepositoryLoader _repositoryLoader;

        public AdminSync(RepositoryLoader repositoryLoader, IUserRepository userRepository)
        {
            _repositoryLoader = repositoryLoader;
            _userRepository = userRepository;
        }

        protected async override Task<List<Response>> ExecuteAdminCommand(MessageDTO message)
        {
            var user = _userRepository.GetUser(message.ChatId);
            var rep = _repositoryLoader.LoadData();
            var command = new AdminSaveCommand(rep, _repositoryLoader);
            return await command.Execute(message, user);
        }
    }
}
