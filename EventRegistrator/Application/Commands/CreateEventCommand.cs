using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.Services;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Infrastructure.Utils;

namespace EventRegistrator.Application.Commands
{
    [CallbackCommand("CreateEvent", "Создать событие")]
    public class CreateEventCommand : ICommand
    {
        private readonly EventService _eventService;
        private readonly ResponseManager _responseManager;

        public CreateEventCommand(EventService eventService, ResponseManager responseManager)
        {
            _eventService = eventService;
            _responseManager = responseManager;
        }

        public async Task<List<Response>> Execute(MessageDTO message, UserAdmin user)
        {
            var @event = EventService.Create(message);
            @event.UpdateTemplate(user.GetTargetChat(message.ChatId).GetHashtagByName(@event.HashtagName).TemplateText);
            @event.AddSlots(TimeSlotParser.ExtractTimeSlotsFromTemplate(@event.TemplateText));
            var result = _eventService.AddNewEvent(@event, message.Created);
            if (result.Success)
            {
                return _responseManager.PrepareNotificationMessages(user, @event);
            }

            return [new Response()];
        }
    }
}
