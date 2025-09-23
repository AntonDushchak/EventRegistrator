using EventRegistrator.Domain.Interfaces;
using Newtonsoft.Json;

namespace EventRegistrator.Domain.Entities
{
    public class Hashtag : IPagiable
    {
        public string HashtagName { get; }
        public string TemplateText { get; private set; }

        public string Name => HashtagName;

        public string Callback => HashtagName;

        private const string defaultTemplate = "10:00 - 10 вільних місць\r\n10:15 - 10 вільних місць\r\n10:30 - 10 вільних місць";

        public Hashtag(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Имя хэштега не может быть пустым", nameof(name));

            HashtagName = name;
            TemplateText = defaultTemplate;
        }

        [JsonConstructor]
        private Hashtag(string hashtagName, string templateText)
        {
            HashtagName = hashtagName;
            TemplateText = templateText;
        }

        public void EditTemplateText(string text)
        {
            if (IsTemplateValid(text))
            {
                TemplateText = text;
            }
        }

        private bool IsTemplateValid(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split('-');
                if (parts.Length < 2)
                    return false;

                var timePart = parts[0].Trim();
                var restPart = parts[1].Trim();

                var match = System.Text.RegularExpressions.Regex.Match(timePart, @"\b\d{1,2}[:\.]\d{2}\b");
                if (!match.Success)
                    return false;

                if (!TimeSpan.TryParse(match.Value.Replace('.', ':'), out _))
                    return false;

                var numberPart = restPart.Split(' ')[0];
                if (!int.TryParse(numberPart, out _))
                    return false;
            }

            return true;
        }
    }
}
