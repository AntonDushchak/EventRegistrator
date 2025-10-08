using EventRegistrator.Application.DTOs;
using EventRegistrator.Domain;
using Telegram.Bot.Types;

namespace EventRegistrator.Infrastructure.Telegram
{
    public class CallbackQueryHandler
    {
        private readonly MessageSender _messageSender;
        private readonly UpdateRouter _updateRouter;

        public CallbackQueryHandler(MessageSender messageSender, UpdateRouter updateRouter)
        {
            _messageSender = messageSender;
            _updateRouter = updateRouter;
        }

        public async Task ProcessCallbackQuery(CallbackQuery callbackQuery)
        {
            await _messageSender.AnswerAsync(callbackQuery.Id);
            var messageDto = UpdateMapper.Map(callbackQuery);
            var responses = await _updateRouter.RouteCallback(messageDto);
            await ProcessMessagesAsync(responses);
        }

        private async Task ProcessMessagesAsync(List<Response> messages)
        {
            foreach (var message in messages)
            {
                try
                {
                    var saveMessageIdCallback = message.SaveMessageIdCallback;
                    message.SaveMessageIdCallback = null;

                    var sentMessage = await _messageSender.SendMessage(message);

                    saveMessageIdCallback?.Invoke(sentMessage.MessageId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при отправке сообщения: {ex}");
                    Console.WriteLine($"Данные сообщения: ChatId={message.ChatId}, Text={message.Text}, MessageToEditId={message.MessageToEditId}, MessageToReplyId={message.MessageToReplyId}, Like={message.Like}, UnLike={message.UnLike}");
                }
            }
        }
    }
}