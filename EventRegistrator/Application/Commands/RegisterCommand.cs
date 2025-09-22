using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.Services;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Models;
using EventRegistrator.Infrastructure.Utils;

namespace EventRegistrator.Application.Commands
{
    [CallbackCommand("Register", "Регистрация на событие")]
    public class RegisterCommand : ICommand
    {
        private readonly ResponseManager _responseManager;
        private readonly RegistrationService _registrationService;

        public RegisterCommand(RegistrationService registrationService, ResponseManager responseManager)
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
            if (@event.PostId != message.ReplyToMessageId)
            {
                Console.WriteLine("Попытка зарегистрировать на другой пост");
                return [];
            }

            var map = TimeSlotParser.GetMaper(@event.TemplateText);
            var regs = TimeSlotParser.ParseRegistrationMessage(message, map);
            if (regs.Count == 0)
            {
                Console.WriteLine("Неудалость распарись сообщение для регистраниции");
                return [];
            }
            
            var result = _registrationService.ProcessRegistration(@event, regs);
            if (result.Success)
            {
                var text = TimeSlotParser.UpdateTemplateText(@event.TemplateText, @event.Slots);
                @event.UpdateTemplate(text);
                result.MessageIds = [message.Id];
                return GetSuccessResponses(user, result);
            }
            return [];
        }


        private List<Response> GetSuccessResponses(UserAdmin user, RegistrationResult result)
        {
            var messages = _responseManager.PrepareNotificationMessages(user, result.Event);
            messages.Add(_responseManager.CreateLikeMessage(result.Event.TargetChatId, result.MessageIds.FirstOrDefault()));
            return messages;
        }
    }
}
