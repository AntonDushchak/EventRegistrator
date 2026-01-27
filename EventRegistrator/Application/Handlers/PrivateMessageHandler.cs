using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Factories;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.States;
using EventRegistrator.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EventRegistrator.Application.Handlers
{
    public class PrivateMessageHandler : IHandler
    {
        private readonly IUserRepository _userRepository;
        private readonly ICommandFactory _commandFactory;
        private readonly ILogger<PrivateMessageHandler> _logger;

        public PrivateMessageHandler(IUserRepository userRepository, ICommandFactory commandFactory, ILogger<PrivateMessageHandler> logger)
        {
            _userRepository = userRepository;
            _commandFactory = commandFactory;
            _logger = logger;
        }

        public async Task<List<Response>> HandleAsync(MessageDTO message)
        {
            var user = _userRepository.GetUser(message.ChatId);
            if (user == null)
            {
                _logger.LogWarning("User not found for chat {ChatId}", message.ChatId);
                return new List<Response>();
            }

            if (IsCommand(message))
            {
                var commandName = CommandTypeResolver.DetermineCommandName(message, user);
                if (commandName == null)
                {
                    _logger.LogWarning("HandleAsync: command type not determined for user {UserId}", user.Id);
                    return new List<Response>();
                }

                var command = _commandFactory.CreateCommand(commandName);
                if (command == null)
                {
                    _logger.LogError("HandleAsync: failed to create command for type {CommandType}", commandName);
                    return new List<Response>();
                }

                var response = await command.Execute(message, user);
                if (response == null || response.Count == 0)
                {
                    _logger.LogError("Failed to execute command. Command: {Command}", command);
                    return new List<Response>();
                }
                return response;
            }

            if (user.State != null)
            {
                var response = await user.State.Execute(message, user);
                if (response == null || response.Count == 0)
                {
                    _logger.LogError("Failed to execute state. State: {State}", user.State);
                    return new List<Response>();
                }
                await _userRepository.Save(user);
                return response;
            }

            _logger.LogError("Failed to handle private message. Message: {@Message}", message);
            return new List<Response>();
        }

        public bool CanHandle(MessageDTO message)
        {
            return IsPrivateMessage(message);
        }

        private bool IsCommand(MessageDTO message)
        {
            return message.Text.StartsWith('/');
        }

        private bool IsPrivateMessage(MessageDTO message)
        {
            return message.ChatId > 0;
        }
    }
}
