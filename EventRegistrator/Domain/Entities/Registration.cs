using EventRegistrator.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace EventRegistrator.Domain.Entities
{
    [Serializable]
    public class Registration
    {
        [JsonConstructor]
        public Registration(long userId, string name, TimeSpan registrationTime, int messageId)
        {
            UserId = userId;
            Name = name;
            RegistrationOnTime = registrationTime;
            MessageId = messageId;
        }

        public long UserId { get; }
        public string Name { get; }
        [JsonProperty("RegistrationTime")]
        [JsonConverter(typeof(TimeSpanOrDateTimeConverter))]
        public TimeSpan RegistrationOnTime { get; }
        public int MessageId { get; }

    }
}
