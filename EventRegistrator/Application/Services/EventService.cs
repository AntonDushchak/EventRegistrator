using EventRegistrator.Application.DTOs;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Domain.Interfaces;
using EventRegistrator.Infrastructure.Utils;

namespace EventRegistrator.Application.Services
{
    public class EventService
    {
        private readonly IUserRepository _userRepository;
        private const string _defaultTitle = "SWS";
        private const char _hashtag = '#';
        public EventService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public RegistrationResult AddNewEvent(Event @event, DateTime eventTime)
        {
            var user = _userRepository.GetUserByTargetChat(@event.TargetChatId);
            if (user.AddEvent(@event))
            {
                return new RegistrationResult { Event = @event, Success = true };
            }

            return new RegistrationResult { Event = @event, Success = false };
        }

        public static Event Create(MessageDTO message)
        {
            var hashtagName = ParseHashtagName(message.Text);
            return new Event(message.Created.ToString(), message.Id, message.ChatId, hashtagName);
        }

        public Event Transport(MessageDTO message, Event sourceEvent)
        {
            if (!CanTransport(message, sourceEvent))
                throw new InvalidOperationException("Невозможно перенести записи в новый ивент");

            var newEvent = Create(message);

            var newHashtag = ParseHashtagName(message.Text);

            var hashtag = _userRepository.GetUserByTargetChat(sourceEvent.TargetChatId).GetTargetChat(sourceEvent.TargetChatId).GetHashtagByName(newHashtag);

            var newSlots = TimeSlotParser.ExtractTimeSlotsFromTemplate(hashtag!.TemplateText);

            foreach (var slot in newSlots)
            {
                newEvent.AddSlot(slot);
            }

            newEvent.UpdateTemplate(hashtag!.TemplateText);

            var sourceSlots = sourceEvent.Slots.ToList();
            var targetSlots = newEvent.Slots.ToList();

            for (int i = 0; i < sourceSlots.Count && i < targetSlots.Count; i++)
            {
                var sourceSlot = sourceSlots[i];
                var targetSlot = targetSlots[i];

                if (sourceSlot.CurrentRegistrationCount > 0)
                {
                    var registrations = sourceSlot.GetRegistrations().ToList();

                    foreach (var registration in registrations)
                    {
                        var newRegistration = new Registration(
                            registration.UserId,
                            registration.Name,
                            targetSlot.Time,
                            registration.MessageId
                        );

                        targetSlot.AddRegistration(newRegistration);
                    }

                    foreach (var registration in registrations)
                    {
                        sourceSlot.RemoveRegistration(registration);
                    }
                }
            }

            return newEvent;
        }

        public bool CanTransport(MessageDTO message, Event sourceEvent)
        {
            var newHashtag = ParseHashtagName(message.Text);
            if (newHashtag == null || newHashtag == sourceEvent.HashtagName) return false;

            var hashtag = _userRepository.GetUserByTargetChat(sourceEvent.TargetChatId).GetTargetChat(sourceEvent.TargetChatId).GetHashtagByName(newHashtag);

            if (hashtag == null) return false;

            var newSlots = TimeSlotParser.ExtractTimeSlotsFromTemplate(hashtag.TemplateText);
            if (newSlots.Count == 0)
                return false;

            var sourceSlots = sourceEvent.Slots.ToList();

            for (int i = 0; i < sourceSlots.Count; i++)
            {
                if (sourceSlots[i].CurrentRegistrationCount > 0 && i >= newSlots.Count)
                {
                    return false;
                }

                if (sourceSlots[i].CurrentRegistrationCount > 0 &&
                    sourceSlots[i].CurrentRegistrationCount > newSlots[i].MaxCapacity)
                {
                    return false;
                }
            }

            return true;
        }

        private static string ParseHashtagName(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var lines = text.Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.RemoveEmptyEntries
            );

            if (lines.Length == 0)
                return null;

            var lastLine = lines.Last().Trim();

            if (!lastLine.StartsWith(_hashtag) || lastLine.Contains(' '))
            {
                Console.WriteLine("Ошибка при парсинге хештега. Нету диеза");
                return null;
            }

            return lastLine.Trim(_hashtag);
        }
    }
}
