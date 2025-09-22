using EventRegistrator.Application.Enums;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Models;
using EventRegistrator.Infrastructure.Utils;

namespace EventRegistrator.Application.Factories
{
    public static class CommandTypeResolver
    {
        private const char _hashtag = '#';
        public static string? DetermineCommandName(MessageDTO message, UserAdmin user)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (message.Text.StartsWith("/say"))
                return "/say";
            if (message.Text.StartsWith('/'))
                return message.Text;
            if (message.IsEdit && IsReplyToPostMessage(message, user))
                return "EditRegistrations";
            if (message.IsEdit && IsFromChannel(message, user) && IsHasHashtag(message, user))
                return "EditEvent";
            if (IsFromChannel(message, user) && IsHasHashtag(message, user))
                return "CreateEvent";
            if (message.Text.EndsWith('?'))
                return string.Empty;
            if (message.Text == "-")
                return "DeleteRegistrations";
            if (message.Text.EndsWith('-'))
                return "DeleteRegistrationsByName";
            if (IsReplyToPostMessage(message, user))
                return "Register";
            return null;
        }

        private static bool IsHasHashtag(MessageDTO message, UserAdmin user)
        {
            var lastPart = message.Text.Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.None
            ).Last();
            if (user.ContainsHashtag(lastPart.Trim(_hashtag)))
            {
                return true;
            }
            return false;
        }

        private static bool IsFromChannel(MessageDTO message, UserAdmin user)
        {
            if (message.ForwardFromChat != null)
            {
                return user.ContainsChannel(message.ForwardFromChat.Id);
            }

            return false;
        }

        private static bool IsReplyToPostMessage(MessageDTO message, UserAdmin user)
        {
            if (message.ReplyToMessage != null && message.ReplyToMessage.ForwardFromChat != null)
            {
                return user.ContainsChannel(message.ReplyToMessage.ForwardFromChat.Id);
            }

            return false;
        }

        
    }
}
