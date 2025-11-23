using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace EventRegistrator.Infrastructure.Telegram
{
    public class BotHandler : IUpdateHandler
    {
        private readonly MessageHandler _messageHandler;
        private readonly CallbackQueryHandler _callbackQueryHandler;

        public BotHandler(MessageHandler messageHandler, CallbackQueryHandler callbackQueryHandler)
        {
            _callbackQueryHandler = callbackQueryHandler;
            _messageHandler = messageHandler;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await (update switch
            {
                { Message: { } message } => _messageHandler.ProcessMessage(message),
                { CallbackQuery: { } callbackQuery } => _callbackQueryHandler.ProcessCallbackQuery(callbackQuery),
                { EditedMessage: { } message } => _messageHandler.ProcessEditMessage(message),
                //{ MyChatMember: { } myChatMember } => _messageHandler.ProcessChatMember(myChatMember),
                //{ ChannelPost: { } post } => _updateProcessor.ProcessPost(post),
                //{ InlineQuery: { } inlineQuery } => OnInlineQuery(inlineQuery),
                //{ ChosenInlineResult: { } chosenInlineResult } => OnChosenInlineResult(chosenInlineResult),
                _ => UnknownUpdateHandlerAsync(update)
            });
        }

        private async Task UnknownUpdateHandlerAsync(Update update)
        {
            Console.WriteLine($"Unknown update type: {update.Type}");
            await Task.Delay(500);
        }

        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            Console.WriteLine(exception);
            await Task.Delay(2000, cancellationToken);
        }

    }
}
