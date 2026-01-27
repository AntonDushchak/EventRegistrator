using System.Reflection;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramUpdatesTests
{
    public class UpdateMapTests
    {
        /// <summary>
        /// Создаёт объект Message с использованием Reflection для обхода read-only свойств
        /// </summary>
        private static Message CreateMessage(
            int messageId,
            long chatId = 0,
            long userId = 0,
            string? text = null,
            DateTime? date = null,
            Message? replyToMessage = null,
            ChatType chatType = ChatType.Group)
        {
            var message = new Message();

            SetProperty(message, nameof(Message.MessageId), messageId);
            SetProperty(message, nameof(Message.Date), date ?? DateTime.UtcNow);

            if (chatId != 0)
            {
                var chat = new Chat();
                SetProperty(chat, nameof(Chat.Id), chatId);
                SetProperty(chat, nameof(Chat.Type), chatType);
                SetProperty(message, nameof(Message.Chat), chat);
            }

            if (userId != 0)
            {
                var user = new User();
                SetProperty(user, nameof(User.Id), userId);
                SetProperty(user, nameof(User.IsBot), false);
                SetProperty(message, nameof(Message.From), user);
            }

            if (text != null)
            {
                SetProperty(message, nameof(Message.Text), text);
            }

            if (replyToMessage != null)
            {
                SetProperty(message, nameof(Message.ReplyToMessage), replyToMessage);
            }

            return message;
        }

        /// <summary>
        /// Устанавливает значение read-only свойства через Reflection
        /// </summary>
        private static void SetProperty<T>(object obj, string propertyName, T value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, value);
            }
            else
            {
                var backingField = obj.GetType()
                    .GetField($"<{propertyName}>k__BackingField",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                backingField?.SetValue(obj, value);
            }
        }

        protected Update CreateMessageUpdate(
            long chatId,
            long userId,
            int messageId,
            string text,
            DateTime? date = null)
        {
            var message = CreateMessage(messageId, chatId, userId, text, date);

            var update = new Update();
            SetProperty(update, nameof(Update.Id), messageId);
            SetProperty(update, nameof(Update.Message), message);

            return update;
        }

        protected Update CreateReplyUpdate(
            long chatId,
            long userId,
            int messageId,
            string text,
            int replyToMessageId)
        {
            var replyToMessage = CreateMessage(replyToMessageId, chatId);
            var message = CreateMessage(messageId, chatId, userId, text, replyToMessage: replyToMessage);

            var update = new Update();
            SetProperty(update, nameof(Update.Id), messageId);
            SetProperty(update, nameof(Update.Message), message);

            return update;
        }

        protected Update CreateCallbackQueryUpdate(
            long chatId,
            long userId,
            int messageId,
            string callbackData)
        {
            var message = CreateMessage(messageId, chatId);

            var callbackQuery = new CallbackQuery();
            SetProperty(callbackQuery, nameof(CallbackQuery.Id), messageId.ToString());
            SetProperty(callbackQuery, nameof(CallbackQuery.Message), message);

            var user = new User();
            SetProperty(user, nameof(User.Id), userId);
            SetProperty(user, nameof(User.IsBot), false);
            SetProperty(callbackQuery, nameof(CallbackQuery.From), user);
            SetProperty(callbackQuery, nameof(CallbackQuery.Data), callbackData);

            var update = new Update();
            SetProperty(update, nameof(Update.Id), messageId);
            SetProperty(update, nameof(Update.CallbackQuery), callbackQuery);

            return update;
        }
    }
}
