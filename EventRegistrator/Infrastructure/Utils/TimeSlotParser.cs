using EventRegistrator.Application.DTOs;
using EventRegistrator.Domain.Entities;
using System.Text.RegularExpressions;

namespace EventRegistrator.Infrastructure.Utils
{
    public static class TimeSlotParser
    {
        private static readonly Regex TokenSplit = new Regex(@"[\s,]+");
        private static readonly Regex SlotOrTimePattern = new Regex(@"^\d{1,2}([:.;]\d{2})?[\.\)]?$");
        private static readonly Regex TemplateRegex = new Regex(@"(?:.*?)(\d{1,2}[:\.]\d{2})\s*[-–]\s*(\d+)\s+вільних місць", RegexOptions.Compiled);

        public static List<TimeSlot> ExtractTimeSlotsFromTemplate(string templateText)
        {
            var timeSlots = new List<TimeSlot>();

            if (string.IsNullOrWhiteSpace(templateText))
                return timeSlots;

            var lines = templateText.Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.RemoveEmptyEntries
            );

            foreach (var line in lines)
            {
                var match = TemplateRegex.Match(line);
                if (match.Success)
                {
                    string timeStr = match.Groups[1].Value.Replace('.', ':');
                    string capacityStr = match.Groups[2].Value;

                    if (TimeSpan.TryParse(timeStr, out TimeSpan time) &&
                        int.TryParse(capacityStr, out int capacity))
                    {
                        timeSlots.Add(new TimeSlot(time, capacity));
                    }
                }
            }

            return timeSlots;
        }

        public static Dictionary<int, TimeSpan> GetMaper(string templateText)
        {
            var dic = new Dictionary<int, TimeSpan>();

            if (string.IsNullOrWhiteSpace(templateText))
                return dic;

            var lines = templateText.Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.RemoveEmptyEntries
            );

            int i = 1;
            foreach (var line in lines)
            {
                var match = TemplateRegex.Match(line);
                if (match.Success)
                {
                    string timeStr = match.Groups[1].Value.Replace('.', ':');

                    if (TimeSpan.TryParse(timeStr, out TimeSpan time))
                    {
                        dic.Add(i, time);
                        i++;
                    }
                }
            }

            return dic;
        }


        public static List<Registration> ParseRegistrationMessage(MessageDTO message, Dictionary<int, TimeSpan> slotMap)
        {
            var result = new List<Registration>();
            if (string.IsNullOrWhiteSpace(message.Text)) return result;

            var preprocessedText = PreprocessText(message.Text.Trim());
            var tokens = TokenSplit.Split(preprocessedText);
            int i = 0;

            while (i < tokens.Length)
            {
                if (string.IsNullOrWhiteSpace(tokens[i])) { i++; continue; }

                var nameParts = new List<string>();

                while (i < tokens.Length && !IsSlotToken(tokens[i]) && tokens[i] != "+")
                {
                    var token = tokens[i];

                    if (IsNameTimeSeparator(token))
                    {
                        i++;
                        break;
                    }

                    if (token.EndsWith("+") && token.Length > 1)
                    {
                        nameParts.Add(token.Substring(0, token.Length - 1));
                        tokens[i] = "+";
                        break;
                    }
                    else
                    {
                        nameParts.Add(token);
                        i++;
                    }
                }

                if (nameParts.Count == 0)
                {
                    i++;
                    continue;
                }

                var name = string.Join(" ", nameParts).Trim();
                bool anySlot = false;

                while (i < tokens.Length && (IsSlotToken(tokens[i]) || tokens[i] == "+"))
                {
                    var slotToken = tokens[i++];

                    if (slotToken == "+" && slotMap != null && slotMap.Count == 1)
                    {
                        var slot = slotMap.First();
                        var resolvedRegistrationTime = slot.Value;
                        result.Add(new Registration(message.UserId.Value, name, resolvedRegistrationTime, message.Id));
                        anySlot = true;
                        continue;
                    }

                    if (TryResolveSlotToken(slotToken, slotMap, out TimeSpan slotTime))
                    {
                        result.Add(new Registration(message.UserId.Value, name, slotTime, message.Id));
                        anySlot = true;
                    }
                }
            }

            return result;
        }

        private static string PreprocessText(string text)
        {
            return Regex.Replace(text, @"(\d+),(\d{2})", "$1_INVALID_$2");
        }

        private static bool IsSlotToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            return SlotOrTimePattern.IsMatch(token.Trim());
        }

        private static bool IsNameTimeSeparator(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            var trimmed = token.Trim();
            return trimmed == "-" || trimmed == "—" || trimmed == "–" || trimmed == ":";
        }

        private static bool TryResolveSlotToken(string token, Dictionary<int, TimeSpan> slotMap, out TimeSpan time)
        {
            time = default;
            if (string.IsNullOrWhiteSpace(token)) return false;

            var t = token.Trim().TrimEnd('.', ')', ';');

            if (int.TryParse(t, out int slot) && slotMap != null && slotMap.TryGetValue(slot, out TimeSpan slotTime))
            {
                time = slotTime;
                return true;
            }

            var normalizedTime = NormalizeTimeString(t);

            if (TimeSpan.TryParse(normalizedTime, out TimeSpan ts))
            {
                time = ts;
                return true;
            }

            return false;
        }

        private static string NormalizeTimeString(string timeStr)
        {
            return timeStr.Replace('.', ':').Replace(';', ':');
        }

        public static TimeSlot FindMatchingTimeSlot(IReadOnlyCollection<TimeSlot> timeSlots, Registration registration)
        {
            return timeSlots.FirstOrDefault(slot =>
                slot.Time.Hours == registration.RegistrationOnTime.Hours &&
                slot.Time.Minutes == registration.RegistrationOnTime.Minutes);
        }

        public static string UpdateTemplateText(string templateText, IReadOnlyCollection<TimeSlot> timeSlots)
        {
            if (string.IsNullOrWhiteSpace(templateText))
                return templateText;

            var regex = new Regex(@"(?:.*?)(\d{1,2}[:\.]\d{2})\s*[-–]\s*(\d+)\s+вільних місць", RegexOptions.Compiled);
            var lines = templateText.Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.None
            );

            var result = new List<string>();

            foreach (var line in lines)
            {
                string updatedLine = line;
                var match = regex.Match(line);

                if (match.Success)
                {
                    string timeStr = match.Groups[1].Value.Replace('.', ':');

                    if (TimeSpan.TryParse(timeStr, out TimeSpan time))
                    {
                        var matchingSlot = timeSlots.FirstOrDefault(slot =>
                            slot.Time.Hours == time.Hours &&
                            slot.Time.Minutes == time.Minutes);

                        if (matchingSlot != null)
                        {
                            int availableSpots = matchingSlot.MaxCapacity - matchingSlot.CurrentRegistrationCount;
                            var prefix = line.Substring(0, match.Groups[1].Index);
                            updatedLine = $"{prefix}{timeStr} - {availableSpots} вільних місць";
                        }
                    }
                }

                result.Add(updatedLine);
            }

            return string.Join(Environment.NewLine, result);
        }

        public static List<(TimeSpan time, int capacity)> ParseTemplate(string templateText)
        {
            var result = new List<(TimeSpan, int)>();
            var regex = new Regex(@"(?:.*?)(\d{1,2}[:\.]\d{2})\s*[-–]\s*(\d+)\s+вільних місць", RegexOptions.Compiled);
            var lines = templateText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    var timeStr = match.Groups[1].Value.Replace('.', ':');
                    var capacityStr = match.Groups[2].Value;

                    if (TimeSpan.TryParse(timeStr, out var time) && int.TryParse(capacityStr, out var capacity))
                    {
                        result.Add((time, capacity));
                    }
                }
            }

            return result;
        }
    }
}
