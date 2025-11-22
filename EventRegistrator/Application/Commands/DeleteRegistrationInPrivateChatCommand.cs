using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Domain.Entities;

namespace EventRegistrator.Application.Commands
{
    [Command("DeleteRegistrationsByNameInPrivate", "Удаление регистраций по имени")]
    public class DeleteRegistrationInPrivateChatCommand : ICommand
    {
        private readonly ICommandFactory _commandFactory;

        public DeleteRegistrationInPrivateChatCommand(ICommandFactory commandFactory)
        {
            _commandFactory = commandFactory;
        }

        public async Task<List<Response>> Execute(MessageDTO message, UserAdmin user)
        {
            var deleteMessage = new MessageDTO
            {
                ChatId = user.CurrentContext.TargetChatId.Value,
                Text = message.Text + "-",
                ThreadId = user.CurrentContext?.EventId != null
                    ? user.GetEvent(user.CurrentContext.EventId.Value).ThreadId : 0,
                Id = message.Id
            };

            var deleteCommand = _commandFactory.CreateCommand("DeleteRegistrationsByName");

            var deletedResponses = await deleteCommand.Execute(deleteMessage, user);

            var updatedResponse = await user.State.Handle(message, user);
            deletedResponses.Add(updatedResponse);
            return deletedResponses;
        }
    }
}
