using EventRegistrator.Domain.Interfaces;
using Newtonsoft.Json;

namespace EventRegistrator.Domain.Entities
{
    public class TargetChat : IPagiable
    {
        [JsonProperty]
        private readonly Dictionary<string, Hashtag> _hashtags;
        public long Id { get; }
        public long ChannelId { get; }
        public string ChannelName { get; }

        public string Name => ChannelName;

        public string Callback => Id.ToString();

        public TargetChat(long id, long channelId, string channelName)
        {
            Id = id;
            ChannelId = channelId;
            ChannelName = channelName;
            _hashtags = new();
        }

        public void AddHashtag(Hashtag hashtag)
        {
            if (_hashtags.TryAdd(hashtag.HashtagName, hashtag))
            {
                return;
            }
            Console.WriteLine("Попытка добавить повторяющийся хештег");
        }

        public Hashtag? GetHashtagByName(string name)
        {
            if (_hashtags.TryGetValue(name, out var hashtag))
            {
                return hashtag;
            }
            return null;
        }

        public bool ContainsHashtag(string hashtag)
        {
            return _hashtags.ContainsKey(hashtag);
        }

        public IReadOnlyCollection<Hashtag> GetAllHashtags()
        {
            return _hashtags.Values;
        }

        public void RemoveHashtag(string hashtag)
        {
            if (ContainsHashtag(hashtag))
            {
                _hashtags.Remove(hashtag);
            }
        }
    }
}
