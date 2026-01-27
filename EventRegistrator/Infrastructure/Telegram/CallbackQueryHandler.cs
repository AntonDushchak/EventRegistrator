using EventRegistrator.Application.DTOs;
using EventRegistrator.Domain;
using Telegram.Bot.Types;

namespace EventRegistrator.Infrastructure.Telegram
{
    public class CallbackQueryHandler
    {
        private readonly MessageSender _messageSender;
        private readonly UpdateRouter _updateRouter;
        private readonly UpdateMapper _updateMapper;

        public CallbackQueryHandler(MessageSender messageSender, UpdateRouter updateRouter, UpdateMapper updateMapper)
        {
            _messageSender = messageSender;
            _updateRouter = updateRouter;
            _updateMapper = updateMapper;
        }

        public async Task ProcessCallbackQuery(CallbackQuery callbackQuery)
        {
            await _messageSender.AnswerAsync(callbackQuery.Id);
            var messageDto = _updateMapper.Map(callbackQuery);
            var responses = await _updateRouter.RouteCallback(messageDto);
            await ProcessMessagesAsync(responses);
        }

        private async Task ProcessMessagesAsync(List<Response> messages)
        {
            var messagesList = messages.ToList();

            await Task.WhenAll(messagesList.Select(async message =>
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
            }));
        }
    }
}