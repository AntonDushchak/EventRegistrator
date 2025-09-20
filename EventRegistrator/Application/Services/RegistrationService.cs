using EventRegistrator.Application.DTOs;
using EventRegistrator.Domain;
using EventRegistrator.Domain.Models;

namespace EventRegistrator.Application.Services
{
    public class RegistrationService
    {
        public RegistrationResult ProcessRegistration(Event @event, List<Registration> registrations)
        {
            var addedRegistrations = new List<Registration>();
            foreach (var registration in registrations)
            {
                if (@event.AddRegistration(registration))
                {
                    addedRegistrations.Add(registration);
                }
                else
                {
                    Console.WriteLine("Ошибка добавления во временной слот");

                    foreach (var addedReg in addedRegistrations)
                    {
                        @event.RemoveRegistrations(addedReg.MessageId);
                    }

                    return new RegistrationResult { Success = false };
                }
            }
            
            return new RegistrationResult { Event = @event, Success = true };
        }

        public RegistrationResult CancelRegistration(Event @event, int messageId)
        {
            @event.RemoveRegistrations(messageId);

            return new RegistrationResult { Event = @event, MessageIds = [messageId], Success = true };
        }
        public RegistrationResult CancelRegistration(Event @event, string name)
        {
            var ids = @event.RemoveRegistrations(name);
            if (ids.Count == 0)
            {
                return new RegistrationResult { Event = @event, MessageIds = ids, Success = false };
            }
            return new RegistrationResult { Event = @event, MessageIds = ids, Success = true };
        }

        public RegistrationResult CancelAllRegistrations(Event @event, long userId)
        {
            var ids = @event.RemoveRegistrations(userId);
            if (ids.Count == 0)
            {
                return new RegistrationResult { Event = @event, MessageIds = ids, Success = false };
            }
            return new RegistrationResult { Event = @event, MessageIds = ids, Success = true };
        }
    }
}
