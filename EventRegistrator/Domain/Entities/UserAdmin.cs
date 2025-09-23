using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Interfaces;
using Newtonsoft.Json;

namespace EventRegistrator.Domain.Entities
{
    [Serializable]
    public class UserAdmin
    {
        [JsonProperty]
        private readonly List<Event> _events;
        [JsonProperty]
        private readonly Dictionary<long, TargetChat> _targetChats;
        [JsonIgnore]
        private readonly Stack<IState> _stateHistory = new();
        [JsonIgnore]
        private IState _state;
        [JsonIgnore]
        public IState State => _state;
        public long Id { get; set; }
        public long PrivateChatId { get; set; }
        public MenuContext CurrentContext { get; set; }
        public int? LastMessageId { get; set; }

        public UserAdmin(long id)
        {
            Id = id;
            _events = new();
            _targetChats = new();
        }

        public bool AddEvent(Event @event)
        {
            if (_events.Count >= 6)
            {
                _events.RemoveRange(0, 4);
            }
            if (_events.Any(e => e.PostId == @event.PostId))
            {
                Console.WriteLine("Уже есть ивент к этому посту");
                return false;
            }
            _events.Add(@event);
            return true;
        }

        public Event GetLastEvent()
        {
            return _events.Last();
        }

        public Event? GetEvent(Guid guid)
        {
            return _events.FirstOrDefault(e => e.Id == guid);
        }

        public Event? GetEventByThreadId(long targetChatId, int threadId)
        {
            var s = _events.Where(e => e.TargetChatId == targetChatId).FirstOrDefault(e => e.ThreadId == threadId);
            return s;
        }

        public Event? GetEventByPostId(long targetChatId, int postId)
        {
            var s = _events.Where(e => e.TargetChatId == targetChatId).FirstOrDefault(e => e.PostId == postId);
            return s;
        }

        public IReadOnlyCollection<Event> GetEvents(long targetChatId)
        {
            var events = _events.Where(e => e.TargetChatId == targetChatId).Reverse().ToList();
            return events;
        }

        public void RemoveEvent(Event @event)
        {
            _events.Remove(@event);
        }

        public void AddTargetChat(TargetChat chat)
        {
            //ArgumentNullException.ThrowIfNull(chat);
            _targetChats[chat.Id] = chat;
        }

        public TargetChat GetTargetChat(long targetChatId)
        {
            return _targetChats[targetChatId];
        }

        public IReadOnlyCollection<TargetChat> GetAllTargetChats()
        {
            return _targetChats.Values;
        }

        public IReadOnlyCollection<Hashtag> GetAllHashtags(long targetChatId)
        {
            return _targetChats[targetChatId].GetAllHashtags();
        }

        public void SetCurrentState(IState state)
        {
            if (_state != null)
                _stateHistory.Push(_state);
            _state = state;
        }

        public IState? RevertState()
        {
            if (_stateHistory.Count > 0)
            {
                _state = _stateHistory.Pop();
                return _state;
            }

            return null;
        }
        
        public void ClearStateHistory() => _stateHistory.Clear();

        public bool ContainsTargetChat(long id)
        {
            return _targetChats.ContainsKey(id);
        }

        public bool ContainsHashtag(string hashtag)
        {
            foreach (var chat in _targetChats.Values)
            {
                if (chat.ContainsHashtag(hashtag))
                {
                    return true;
                }
            }
            return false;
        }

        public bool ContainsChannel(long id)
        {
            foreach (var chat in _targetChats.Values)
            {
                if (chat.ChannelId == id)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
