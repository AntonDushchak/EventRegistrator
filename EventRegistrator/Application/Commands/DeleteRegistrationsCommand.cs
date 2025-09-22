using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.Services;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Models;
using EventRegistrator.Infrastructure.Utils;
using Telegram.Bot.Types;

namespace EventRegistrator.Application.Commands
{
    [Command("DeleteRegistrations", "Удаление регистраций в одном сообщении")]
    public class DeleteRegistrationsCommand : ICommand
    {
        private readonly ResponseManager _responseManager;
        private readonly RegistrationService _registrationService;

        public DeleteRegistrationsCommand(RegistrationService registrationService, ResponseManager responseManager)
        {
            _registrationService = registrationService;
            _responseManager = responseManager;
        }

        public async Task<List<Response>> Execute(MessageDTO message, UserAdmin user)
        {
            var @event = user.GetEventByThreadId(message.ChatId, message.ThreadId ?? 0);
            if (@event == null)
            {
                Console.WriteLine("Не удалось найти ивент");
                return [];
            }

            if (message.IsEdit && message.IsReply && message.ReplyToMessageId == @event.PostId)
            {
                var resultUndo = _registrationService.CancelRegistration(@event, message.Id);
                if (resultUndo.Success)
                {
                    message.IsEdit = false;
                    var text = TimeSlotParser.UpdateTemplateText(@event.TemplateText, @event.Slots);
                    @event.UpdateTemplate(text);
                    return GetSuccessResponsesForEdit(user, resultUndo, message.Id);
                }

                return [];
            }

            else if (message.IsReply && message.UserId == message.ReplyToMessage?.UserId)
            {
                var resultUndo = _registrationService.CancelRegistration(@event, message.ReplyToMessageId ?? 0);
                if (resultUndo.Success)
                {
                    message.IsEdit = false;
                    var text = TimeSlotParser.UpdateTemplateText(@event.TemplateText, @event.Slots);
                    @event.UpdateTemplate(text);
                    return GetSuccessResponsesForEdit(user, resultUndo, message.Id);
                }
                return [];
            }

            Console.WriteLine("Ошибка при удалении регистраций");
            return [];
        }

        private List<Response> GetSuccessResponsesForEdit(UserAdmin user, RegistrationResult result, int messageId)
        {
            var messages = _responseManager.PrepareNotificationMessages(user, result.Event);
            messages.Add(_responseManager.CreateUnlikeMessage(result.Event.TargetChatId, result.MessageIds.FirstOrDefault()));
            messages.Add(_responseManager.CreateLikeMessage(result.Event.TargetChatId, messageId));
            return messages;
        }
    }
}
