using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.Services;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Entities;

namespace EventRegistrator.Application.Commands
{
    [CallbackCommand("EditEvent", "Редактировать событие")]
    public class EditEventCommand : ICommand
    {
        private readonly EventService _eventService;
        private readonly ResponseManager _responseManager;

        public EditEventCommand(EventService eventService, ResponseManager responseManager)
        {
            _eventService = eventService;
            _responseManager = responseManager;
        }

        public async Task<List<Response>> Execute(MessageDTO message, UserAdmin user)
        {
            var @event = user.GetEventByPostId(message.ChatId, message.Id);
            if (@event == null)
            {
                var createCommand = new CreateEventCommand(_eventService, _responseManager);
                return await createCommand.Execute(message, user);
            }

            if (!_eventService.CanTransport(message, @event))
            {
                Console.WriteLine("Ошибка при изменении хештега.");
                return [_responseManager.CreatePrivateMessage(user, "Ошибка при изменении хештега. Возможно количество доступных мест не совпадает с количеством записавшихся. Хештег не изменился.")];
            }

            var newEvent = _eventService.Transport(message, @event);
            if (newEvent == null)
            {
                Console.WriteLine("Ошибка при изменении хештега. Хештег изменился, но он null");
            }
            user.RemoveEvent(@event);
            user.AddEvent(newEvent!);

            return _responseManager.PrepareNotificationMessages(user, @event);
        }
    }
}
