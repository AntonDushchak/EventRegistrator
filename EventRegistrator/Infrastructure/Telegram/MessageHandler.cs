using EventRegistrator.Application.DTOs;
using EventRegistrator.Domain;
using EventRegistrator.Domain.DTO;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace EventRegistrator.Infrastructure.Telegram
{
    public class MessageHandler
    {
        private readonly MessageSender _messageSender;
        private readonly UpdateRouter _updateRouter;

        public MessageHandler(MessageSender messageSender, UpdateRouter updateRouter)
        {
            _messageSender = messageSender;
            _updateRouter = updateRouter;
        }

        public async Task ProcessMessage(Message message)
        {
            if (message.Type == MessageType.MigrateFromChatId || message.Type == MessageType.MigrateToChatId) return;
            if (message.Text == null && message.Caption == null) return;
            var messageDto = UpdateMapper.Map(message);
            var responses = GetResponse(messageDto);
            await ProcessMessagesAsync(responses.Result);
        }

        public async Task ProcessEditMessage(Message message)
        {
            var messageDto = UpdateMapper.Map(message);
            messageDto.IsEdit = true;
            var responses = GetResponse(messageDto);
            await ProcessMessagesAsync(responses.Result);
        }

        private async Task<List<Response>> GetResponse(MessageDTO message)
        {
            return await _updateRouter.RouteMessage(message);
        }

        private async Task ProcessMessagesAsync(List<Response> messages)
        {
            foreach (var message in messages)
            {
                try
                {
                    var saveMessageIdCallback = message.SaveMessageIdCallback;
                    var saveMessageThreadIdCallback = message.SaveMessageThreadIdCallback;

                    message.SaveMessageIdCallback = null;
                    message.SaveMessageThreadIdCallback = null;

                    var sentMessage = await _messageSender.SendMessage(message);

                    if (sentMessage.MessageId == default)
                    {
                        continue;
                    }
                    saveMessageIdCallback?.Invoke(sentMessage.MessageId);
                    if (sentMessage.MessageThreadId == default)
                    {
                        continue;
                    }
                    
                    saveMessageThreadIdCallback?.Invoke(sentMessage.MessageThreadId ?? 0);
                }
                catch (Exception ex)
                {
                    // Логируем ошибку, чтобы не падал весь цикл
                    // Можно использовать ваш логгер, если он есть
                    Console.WriteLine($"Ошибка при отправке сообщения: {ex}");
                    Console.WriteLine($"Данные сообщения: ChatId={message.ChatId}, Text={message.Text}, MessageToEditId={message.MessageToEditId}, MessageToReplyId={message.MessageToReplyId}, Like={message.Like}, UnLike={message.UnLike}");
                }
            }
        } 
    }
}