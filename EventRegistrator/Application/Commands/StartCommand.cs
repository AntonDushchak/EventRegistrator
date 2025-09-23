using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Domain.Interfaces;
using EventRegistrator.Infrastructure.Utils;

namespace EventRegistrator.Application.Commands
{
    [Command("/start", "Начать работу с ботом")]
    public class StartCommand : ICommand
    {
        private readonly IUserRepository _userRepository;

        public StartCommand(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<Response>> Execute(MessageDTO message, UserAdmin user)
        {
            _userRepository.AddUser(message.ChatId);
            user.LastMessageId = null;
            user.ClearStateHistory();
            var response = new Response { ChatId = message.ChatId, Text = Constants.Greetings };
            response.SaveMessageIdCallback = id => user.LastMessageId = id;
            return [response];
        }
    }
}
