using EventRegistrator.Domain.DTO;
using Telegram.Bot.Types;

namespace EventRegistrator.Infrastructure.Telegram
{
    public static class UpdateMapper
    {
        private static readonly TimeSpan _timeZoneOffset = TimeSpan.FromHours(3);
        public static MessageDTO Map(Message message)
        {
            var messageDto = new MessageDTO
            {
                ChatId = message.Chat.Id,
                Id = message.MessageId,
                Text = message.Text ?? message.Caption,
                UserId = message.From?.Id,
                ReplyToMessageId = message.ReplyToMessage?.Id,
                Created = message.Date.Add(_timeZoneOffset),
                //IsFromAdmin = message.From.
            };

            if (messageDto.ReplyToMessageId != null)
                messageDto.IsReply = true;

            if (message.ForwardFromChat != null)
            {
                messageDto.ForwardFromChat = new ChatDTO
                {
                    Id = message.ForwardFromChat.Id,
                    Title = message.ForwardFromChat.Title,
                    Type = message.ForwardFromChat.Type.ToString()
                };
            }

            if (message.ReplyToMessage != null)
            {
                messageDto.ReplyToMessage = Map(message.ReplyToMessage);
            }

            if (message.MessageThreadId != null)
            {
                messageDto.ThreadId = message.MessageThreadId;
            }

            return messageDto;
        }

        public static List<MessageDTO> Map(List<Message> messages)
        {
            var result = new List<MessageDTO>();
            foreach (var message in messages)
            {
                result.Add(Map(message));
            }
            return result;
        }

        public static MessageDTO Map(CallbackQuery callbackQuery)
        {
            var message = Map(callbackQuery.Message);
            var messageDto = new MessageDTO
            {
                ChatId = message.ChatId,
                Id = message.Id,
                Text = callbackQuery.Data,
                UserId = callbackQuery.From.Id,
                ReplyToMessageId = message.ReplyToMessageId,
                Created = message.Created
            };

            return messageDto;
        }
    }
}
