using EventRegistrator.Domain.Entities;
using EventRegistrator.Infrastructure.Persistence;
using System.Text;

namespace EventRegistrator.Infrastructure.Utils
{
    public static class TextFormatter
    {
        public static string FormatRegistrationsInfo(Event lastEvent)
        {
            var slots = lastEvent.Slots ?? new List<TimeSlot>();

            if (slots.Count == 0)
                return "Нет доступных временных слотов";

            var sb = new StringBuilder();
            sb.AppendLine($"Запис на: {lastEvent.Title}");
            sb.AppendLine();

            slots = slots.OrderBy(s => s.Time).ToList();

            foreach (var slot in slots)
            {
                sb.AppendLine($"{slot.Time.ToString(@"hh\:mm")}     {slot.CurrentRegistrationCount} / {slot.MaxCapacity}");

                var registrations = GetRegistrationsFromTimeSlot(slot);

                if (registrations.Any())
                {
                    foreach (var registration in registrations)
                    {
                        sb.AppendLine(registration.Name);
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static List<Registration> GetRegistrationsFromTimeSlot(TimeSlot slot)
        {
            var registrationsField = typeof(TimeSlot).GetField("_currentRegistrations",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (registrationsField != null)
            {
                var registrations = registrationsField.GetValue(slot) as List<Registration>;
                return registrations ?? new List<Registration>();
            }

            return new List<Registration>();
        }

        public static string GetAllUsersInfo(UserRepository userRepository)
        {
            var sb = new StringBuilder();
            var users = userRepository.GetAllUsers();
            
            if (users.Count == 0)
            {
                return "Нет зарегистрированных пользователей.";
            }
            
            sb.AppendLine($"Всего пользователей: {users.Count}");
            sb.AppendLine();
            
            foreach (var user in users)
            {
                sb.AppendLine($"===== Пользователь ID: {user.Id} =====");
                sb.AppendLine($"Приватный чат ID: {user.PrivateChatId}");
                
                // Получаем событие пользователя через доступный метод GetLastEvent
                try
                {
                    var lastEvent = user.GetLastEvent();
                    if (lastEvent != null)
                    {
                        sb.AppendLine();
                        sb.AppendLine($"Последнее событие:");
                        sb.AppendLine($"  - ID: {lastEvent.Id}");
                        sb.AppendLine($"    Название: {lastEvent.Title}");
                        sb.AppendLine($"    Канал ID: {lastEvent.TargetChatId}");
                        sb.AppendLine($"    Хэштег: {lastEvent.HashtagName}");
                        sb.AppendLine($"    Пост ID: {lastEvent.PostId}");
                        sb.AppendLine($"    ID сообщения в привате: {lastEvent.PrivateMessageId}");
                        sb.AppendLine($"    ID сообщения-комментария: {lastEvent.CommentMessageId}");
                        
                        var slots = lastEvent.Slots;
                        if (slots != null && slots.Any())
                        {
                            sb.AppendLine($"    Временные слоты ({slots.Count}):");
                            foreach (var slot in slots.OrderBy(s => s.Time))
                            {
                                var regs = GetRegistrationsFromTimeSlot(slot);
                                sb.AppendLine($"      - {slot.Time.ToString(@"hh\:mm")} (Занято: {slot.CurrentRegistrationCount}/{slot.MaxCapacity})");
                                
                                if (regs.Any())
                                {
                                    foreach (var reg in regs)
                                    {
                                        sb.AppendLine($"        * {reg.Name} (ID: {reg.UserId}, Сообщение: {reg.MessageId})");
                                    }
                                }
                            }
                        }
                        else
                        {
                            sb.AppendLine("    У события нет временных слотов.");
                        }
                    }
                    else
                    {
                        sb.AppendLine("У пользователя нет событий.");
                    }
                }
                catch (InvalidOperationException)
                {
                    // Обработка случая, когда у пользователя нет событий (_events пуст)
                    sb.AppendLine("У пользователя нет событий.");
                }
                
                sb.AppendLine();
                sb.AppendLine("------------------------------");
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
    }
}
