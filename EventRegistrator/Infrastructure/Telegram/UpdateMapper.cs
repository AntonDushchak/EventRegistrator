using EventRegistrator.Application.DTOs;
using Telegram.Bot.Types;

namespace EventRegistrator.Infrastructure.Telegram
{
    public class UpdateMapper
    {
        private static readonly TimeSpan _timeZoneOffset = TimeSpan.FromHours(3);
        private readonly IAdminCache _adminCache;

        public UpdateMapper(IAdminCache adminCache)
        {
            _adminCache = adminCache;
        }

        public MessageDTO Map(Message message)
        {
            var messageDto = new MessageDTO
            {
                ChatId = message.Chat.Id,
                Id = message.MessageId,
                Text = message.Text ?? message.Caption,
                UserId = message.From?.Id,
                ReplyToMessageId = message.ReplyToMessage?.Id,
                Created = message.Date.Add(_timeZoneOffset),
                IsFromAdmin = message.From != null && _adminCache.IsAdmin(message.Chat.Id, message.From.Id)
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

        public List<MessageDTO> Map(List<Message> messages)
        {
            var result = new List<MessageDTO>();
            foreach (var message in messages)
            {
                result.Add(Map(message));
            }
            return result;
        }

        public MessageDTO Map(CallbackQuery callbackQuery)
        {
            var message = Map(callbackQuery.Message);
            var messageDto = new MessageDTO
            {
                ChatId = message.ChatId,
                Id = message.Id,
                Text = callbackQuery.Data,
                UserId = callbackQuery.From.Id,
                ReplyToMessageId = message.ReplyToMessageId,
                Created = message.Created,
                IsFromAdmin = callbackQuery.From != null && _adminCache.IsAdmin(message.ChatId, callbackQuery.From.Id)
            };

            return messageDto;
        }
    }
}
