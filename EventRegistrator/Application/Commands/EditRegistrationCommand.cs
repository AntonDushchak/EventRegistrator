using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.Services;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Infrastructure.Utils;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace EventRegistrator.Application.Commands
{
    [Command("EditRegistrations", "Редактиврование записи")]
    public class EditRegistrationCommand : ICommand
    {
        private readonly RegistrationService _registrationService;
        private readonly ResponseManager _responseManager;
        private readonly ILogger<EditRegistrationCommand> _logger;

        public EditRegistrationCommand(RegistrationService registrationService, ResponseManager responseManager, ILogger<EditRegistrationCommand> logger)
        {
            _registrationService = registrationService;
            _responseManager = responseManager;
            _logger = logger;
        }

        public async Task<List<Response>> Execute(MessageDTO message, UserAdmin user)
        {
            var @event = user.GetEventByThreadId(message.ChatId, message.ThreadId ?? 0);
            if (@event == null)
            {
                _logger.LogWarning("Событие не найдено: ChatId={ChatId}, ThreadId={ThreadId}", message.ChatId, message.ThreadId);
                return [];
            }
            if (message.IsEdit && message.IsReply && message.ReplyToMessageId == @event.PostId)
            {
                List<Response> responses = new();
                var resultUndo = _registrationService.CancelRegistration(@event, message.Id);
                if (resultUndo.Success)
                {
                    message.IsEdit = false;
                    var text = TimeSlotParser.UpdateTemplateText(@event.TemplateText, @event.Slots);
                    @event.UpdateTemplate(text);
                    responses.AddRange(GetSuccessResponsesForEdit(user, resultUndo));
                    responses.Add(_responseManager.CreateUnlikeMessage(resultUndo.Event.TargetChatId, resultUndo.MessageIds.FirstOrDefault()));
                }
                else
                {
                    _logger.LogWarning("Не удалось отменить регистрацию при редактировании");
                    return [];
                }

                var map = TimeSlotParser.GetMaper(@event.TemplateText);
                var regs = TimeSlotParser.ParseRegistrationMessage(message, map);
                if (regs.Count == 0)
                {
                    _logger.LogWarning("Не удалось распарсить сообщение для регистранции при редактировании");
                    return responses;
                }
                var result = _registrationService.ProcessRegistration(@event, regs);
                if (result.Success)
                {
                    var text = TimeSlotParser.UpdateTemplateText(@event.TemplateText, @event.Slots);
                    @event.UpdateTemplate(text);
                    result.MessageIds = [message.Id];
                    responses.AddRange(GetSuccessResponsesForEdit(user, result));
                    responses.Add(_responseManager.CreateLikeMessage(resultUndo.Event.TargetChatId, resultUndo.MessageIds.FirstOrDefault()));
                }
                else
                {
                    _logger.LogWarning("Не удалось добавить регистрации при редактировании");
                }
                return responses;
            }
            return [];
        }

        private List<Response> GetSuccessResponsesForEdit(UserAdmin user, RegistrationResult result)
        {
            var messages = _responseManager.PrepareNotificationMessages(user, result.Event);
            return messages;
        }
    }
}
