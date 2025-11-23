using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.Services;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Infrastructure.Utils;

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

            if (IsEditOfOriginalMessage(message, @event.PostId))
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

            else if (IsSelfReply(message) || message.IsFromAdmin)
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

        private bool IsEditOfOriginalMessage(MessageDTO message, int postId)
        {
            return message.IsEdit && message.IsReply && message.ReplyToMessageId == postId;
        }

        private bool IsSelfReply(MessageDTO message)
        {
            return message.IsReply && message.UserId == message.ReplyToMessage?.UserId;
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
