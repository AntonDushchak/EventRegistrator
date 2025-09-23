using EventRegistrator.Domain.Interfaces;
using EventRegistrator.Infrastructure.Utils;
using Newtonsoft.Json;

namespace EventRegistrator.Domain.Entities
{
    [Serializable]
    public class Event : IPagiable
    {
        [JsonProperty]
        private readonly List<TimeSlot> _slots;

        public Event(string title, int postId, long targetChatId, string hashtagName)
        {
            Id = Guid.NewGuid();
            Title = title;
            PostId = postId;
            TargetChatId = targetChatId;
            HashtagName = hashtagName;
            _slots = new List<TimeSlot>();
        }
        public Guid Id { get; }
        public string Title { get; }
        public long TargetChatId { get; }
        public string HashtagName { get; }
        public int PostId { get; }
        public int ThreadId { get; set; }
        public int CommentMessageId { get; set; }
        public int PrivateMessageId { get; set; }
        [JsonProperty]
        private string _templateText;
        public string TemplateText => _templateText;
        public IReadOnlyCollection<TimeSlot> Slots => _slots.AsReadOnly();

        public string Name => Title;

        public string Callback => Id.ToString();

        public void AddSlot(TimeSlot slot)
        {
            ArgumentNullException.ThrowIfNull(slot);

            if (_slots.Any(s => s.Time == slot.Time))
                throw new ArgumentException($"Слот на {slot.Time} уже существует.");

            _slots.Add(slot);
        }

        public void AddSlots(IEnumerable<TimeSlot> slots)
        {
            ArgumentNullException.ThrowIfNull(slots);

            foreach (var slot in slots)
            {
                if (_slots.Any(s => s.Time == slot.Time))
                    throw new ArgumentException($"Слот на {slot.Time} уже существует.");

                _slots.Add(slot);
            }
        }

        public void RemoveSlot(TimeSlot slot)
        {
            ArgumentNullException.ThrowIfNull(slot);
            _slots.Remove(slot);
        }

        public List<int> RemoveRegistrations(string name)
        {
            ArgumentNullException.ThrowIfNull(name);
            var messageIds = new List<int>();
            foreach (var slot in _slots)
            {
                var reg = slot.GetRegistration(name);
                if (reg == default) continue;
                slot.RemoveRegistration(reg);
                messageIds.Add(reg.MessageId);
            }
            return messageIds;
        }

        public void RemoveRegistrations(int messageId)
        {
            foreach (var slot in _slots)
            {
                var reg = slot.GetRegistration(messageId);
                if (reg == default) continue;
                slot.RemoveRegistration(reg);
            }
        }

        public List<int> RemoveRegistrations(long userId)
        {
            var messageIds = new List<int>();
            foreach (var slot in _slots)
            {
                var reg = slot.GetRegistration(userId);
                if (reg == default) continue;
                slot.RemoveRegistration(reg);
                messageIds.Add(reg.MessageId);
            }
            return messageIds;
        }

        public bool AddRegistration(Registration registration)
        {
            var slot = TimeSlotParser.FindMatchingTimeSlot(_slots, registration);
            if (slot == null)
                return false;
            return slot.AddRegistration(registration);
        }

        public void UpdateTemplate(string text)
        {
            _templateText = text;
        }

        public void EditTemplate(string text)
        {
            var values = TimeSlotParser.ParseTemplate(text);
            if (!CanReduceSlotsTo(values.Count))
            {
                Console.WriteLine("Попытка обновить шаблон в котором есть записи убрав слоты");
                return;
            }
            var i = 0;
            if (values.Count >= _slots.Count)
            {
                for (; i < _slots.Count; i++)
                {
                    _slots[i].EditTime(values[i].time);
                    _slots[i].EditCapacity(values[i].capacity);
                }
                for (; i < values.Count; i++)
                {
                    AddSlot(new TimeSlot(values[i].time, values[i].capacity));
                }
            }
            else if (values.Count < _slots.Count)
            {
                for (; i < values.Count; i++)
                {
                    _slots[i].EditTime(values[i].time);
                    _slots[i].EditCapacity(values[i].capacity);
                }

                for (; i < _slots.Count; i++)
                {
                    RemoveSlot(_slots[i]);
                }
            }

            _templateText = TimeSlotParser.UpdateTemplateText(text, _slots);
        }

        private bool CanReduceSlotsTo(int count)
        {
            if (count >= _slots.Count)
            {
                return true;
            }

            for (int i = count; i < _slots.Count; i++)
            {
                if (_slots[i].CurrentRegistrationCount > 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
