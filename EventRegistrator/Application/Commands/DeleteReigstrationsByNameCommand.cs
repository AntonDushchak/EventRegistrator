using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.Services;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Infrastructure.Utils;

namespace EventRegistrator.Application.Commands
{
    [Command("DeleteRegistrationsByName", "Удаление регистраций по имени")]
    public class DeleteReigstrationsByNameCommand : ICommand
    {
        private readonly ResponseManager _responseManager;
        private readonly RegistrationService _registrationService;

        public DeleteReigstrationsByNameCommand(ResponseManager responseManager, RegistrationService registrationService)
        {
            _responseManager = responseManager;
            _registrationService = registrationService;
        }

        public async Task<List<Response>> Execute(MessageDTO message, UserAdmin user)
        {
            var @event = user.GetEventByThreadId(message.ChatId, message.ThreadId ?? 0);
            if (@event == null)
            {
                Console.WriteLine("Не удалось найти ивент");
                return [];

            }

            var name = message.Text?.Trim('-', ' ');

            var resultUndo = _registrationService.CancelRegistration(@event, name);
            if (resultUndo.Success)
            {
                var text = TimeSlotParser.UpdateTemplateText(@event.TemplateText, @event.Slots);
                @event.UpdateTemplate(text);
                return GetSuccessResponsesForEdit(user, resultUndo, message);
            }

            return [];
        }

        private List<Response> GetSuccessResponsesForEdit(UserAdmin user, RegistrationResult result, MessageDTO message)
        {
            var messages = _responseManager.PrepareNotificationMessages(user, result.Event);
            foreach (var id in result.MessageIds)
            {
                messages.Add(_responseManager.CreateUnlikeMessage(result.Event.TargetChatId, id));
            }
            if (message.UserId is not null)
            {
                messages.Add(_responseManager.CreateLikeMessage(result.Event.TargetChatId, message.Id));
            }
            
            return messages;
        }
    }
}
