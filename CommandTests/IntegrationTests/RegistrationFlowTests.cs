using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Factories;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Domain.Interfaces;
using EventRegistrator.Infrastructure.Telegram;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CommandTests.IntegrationTests
{
    [TestFixture]
    public class RegistrationFlowTests : TestBase
    {
        private BotHandler _botHandler;
        private Mock<ITelegramBotClient> _botClientMock;
        private MessageHandler _messageHandler;
        private CallbackQueryHandler _callbackQueryHandler;
        private MessageSender _messageSender;
        private UpdateRouter _updateRouter;
        private UpdateMapper _updateMapper;
        private Mock<IAdminCache> _adminCacheMock;
        private Mock<ILogger<MessageSender>> _messageSenderLoggerMock;
        private Mock<ILogger<UpdateRouter>> _updateRouterLoggerMock;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            InitializeTelegramInfrastructure();
        }

        private void InitializeTelegramInfrastructure()
        {
            _botClientMock = new Mock<ITelegramBotClient>();
            _adminCacheMock = new Mock<IAdminCache>();
            _messageSenderLoggerMock = new Mock<ILogger<MessageSender>>();
            _updateRouterLoggerMock = new Mock<ILogger<UpdateRouter>>();

            // Настройка AdminCache - все пользователи в тестах считаются администраторами
            _adminCacheMock
                .Setup(cache => cache.IsAdmin(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(true);

            _messageSender = new MessageSender(_botClientMock.Object, _messageSenderLoggerMock.Object);

            var messageHandlers = new List<IHandler>
            {
                new TestCommandHandler(CreateEventCommand, UserAdmin, UserRepositoryMock.Object),
                new TestCommandHandler(RegisterCommand, UserAdmin, UserRepositoryMock.Object),
                new TestCommandHandler(EditRegistrationCommand, UserAdmin, UserRepositoryMock.Object),
                new TestCommandHandler(DeleteRegistrationsCommand, UserAdmin, UserRepositoryMock.Object),
                new TestCommandHandler(DeleteReigstrationsByNameCommand, UserAdmin, UserRepositoryMock.Object)
            };

            var callbackHandlers = new List<IHandler>();

            _updateRouter = new UpdateRouter(messageHandlers, callbackHandlers, _updateRouterLoggerMock.Object);
            _updateMapper = new UpdateMapper(_adminCacheMock.Object);

            _messageHandler = new MessageHandler(_messageSender, _updateRouter, _updateMapper);
            _callbackQueryHandler = new CallbackQueryHandler(_messageSender, _updateRouter, _updateMapper);

            _botHandler = new BotHandler(_messageHandler, _callbackQueryHandler);
        }

        [Test]
        public async Task CompleteRegistrationFlow_FromBotHandler_Test()
        {
            await CreateEventViaBotHandler();
            await RegisterFirstUserViaBotHandler();
            await RegisterSecondUserViaBotHandler();
            await AttemptToRegisterInFullSlotViaBotHandler();
            await EditRegistrationViaBotHandler();
            await DeleteRegistrationByNameViaBotHandler();
            await DeleteRegistrationByReplyViaBotHandler();

            Assert.That(Event.Slots.Sum(s => s.CurrentRegistrationCount), Is.EqualTo(0), "В конце теста остались регистрации");
        }

        private async Task CreateEventViaBotHandler()
        {
            var update = CreateUpdate(
                messageId: CREATE_EVENT_MESSAGE_ID,
                chatId: 123456,
                userId: 101112,
                text: "Тестовое событие \n#test"
            );

            await _botHandler.HandleUpdateAsync(_botClientMock.Object, update, CancellationToken.None);

            Event = UserAdmin.GetLastEvent();
            Assert.That(Event, Is.Not.Null, "Событие не было создано");
            Assert.That(Event.HashtagName, Is.EqualTo("test"), "Неверный хештег события");

            VerifyEventSlots(Event);
        }

        private async Task RegisterFirstUserViaBotHandler()
        {
            var update = CreateUpdate(
                messageId: REGISTER_IVAN_MESSAGE_ID,
                chatId: 123456,
                userId: 201112,
                text: "Иван 1 2",
                replyToMessageId: Event.PostId
            );

            await _botHandler.HandleUpdateAsync(_botClientMock.Object, update, CancellationToken.None);

            VerifyFirstUserRegistration();
        }

        private void VerifyFirstUserRegistration()
        {
            var slot1 = Event.Slots.ElementAt(0);
            var slot2 = Event.Slots.ElementAt(1);

            Assert.That(slot1.CurrentRegistrationCount, Is.EqualTo(1), "Регистрация не добавлена в первый слот");
            Assert.That(slot2.CurrentRegistrationCount, Is.EqualTo(1), "Регистрация не добавлена во второй слот");
            Assert.That(slot1.Contains("Иван"), Is.EqualTo(true), "Неверное имя в регистрации");
        }

        private async Task RegisterSecondUserViaBotHandler()
        {
            var update = CreateUpdate(
                messageId: REGISTER_PETR_MESSAGE_ID,
                chatId: 123456,
                userId: 301112,
                text: "Петр 1, 3",
                replyToMessageId: Event.PostId
            );

            await _botHandler.HandleUpdateAsync(_botClientMock.Object, update, CancellationToken.None);

            VerifySecondUserRegistration();
        }

        private void VerifySecondUserRegistration()
        {
            var slot1 = Event.Slots.ElementAt(0);
            Assert.That(slot1.CurrentRegistrationCount, Is.EqualTo(2), "Регистрация второго пользователя не добавлена в первый слот");
            Assert.That(slot1.Contains("Петр"), Is.True, "Нет регистрации Петра в первом слоте");
        }

        private async Task AttemptToRegisterInFullSlotViaBotHandler()
        {
            var update = CreateUpdate(
                messageId: REGISTER_ALEXEY_MESSAGE_ID,
                chatId: 123456,
                userId: 401112,
                text: "Алексей 1",
                replyToMessageId: Event.PostId
            );

            await _botHandler.HandleUpdateAsync(_botClientMock.Object, update, CancellationToken.None);

            var slot1 = Event.Slots.ElementAt(0);
            Assert.That(slot1.CurrentRegistrationCount, Is.EqualTo(2), "Неожиданное количество регистраций после попытки переполнения");
        }

        private async Task EditRegistrationViaBotHandler()
        {
            var update = CreateUpdate(
                messageId: REGISTER_IVAN_MESSAGE_ID,
                chatId: 123456,
                userId: 201112,
                text: "Иван 2 3",
                replyToMessageId: Event.PostId,
                isEdited: true
            );

            await _botHandler.HandleUpdateAsync(_botClientMock.Object, update, CancellationToken.None);

            VerifyEditedRegistration();
        }

        private void VerifyEditedRegistration()
        {
            var slot1 = Event.Slots.ElementAt(0);
            var slot2 = Event.Slots.ElementAt(1);

            Assert.That(slot1.Contains(201112), Is.EqualTo(false), "Регистрация не удалена из первого слота");
            Assert.That(slot2.Contains(201112), Is.EqualTo(true), "Регистрация не добавлена во второй слот");
        }

        private async Task DeleteRegistrationByNameViaBotHandler()
        {
            var update = CreateUpdate(
                messageId: DELETE_BY_NAME_MESSAGE_ID,
                chatId: 123456,
                userId: 101112,
                text: "Петр-",
                threadId: Event.ThreadId
            );

            await _botHandler.HandleUpdateAsync(_botClientMock.Object, update, CancellationToken.None);

            Assert.That(Event.Slots.All(s => !s.Contains("Петр")), Is.True, "Регистрации Петра не удалены");
        }

        private async Task DeleteRegistrationByReplyViaBotHandler()
        {
            var update = CreateUpdate(
                messageId: DELETE_BY_REPLY_MESSAGE_ID,
                chatId: 123456,
                userId: 201112,
                text: "",
                replyToMessageId: REGISTER_IVAN_MESSAGE_ID,
                replyFromUserId: 201112,
                threadId: Event.ThreadId
            );

            await _botHandler.HandleUpdateAsync(_botClientMock.Object, update, CancellationToken.None);

            Assert.That(Event.Slots.All(s => !s.Contains("Иван")), Is.True, "Регистрации Ивана не удалены");
        }

        // Вспомогательные методы для создания Update через JSON десериализацию
        private Update CreateUpdate(
            int messageId,
            long chatId,
            long userId,
            string text,
            int? replyToMessageId = null,
            long? replyFromUserId = null,
            int? threadId = null,
            bool isEdited = false)
        {
            var messageJson = BuildMessageJson(messageId, chatId, userId, text, replyToMessageId, replyFromUserId, threadId, isEdited);

            string updateJson;
            if (isEdited)
            {
                updateJson = $@"{{
                    ""update_id"": {messageId},
                    ""edited_message"": {messageJson}
                }}";
            }
            else
            {
                updateJson = $@"{{
                    ""update_id"": {messageId},
                    ""message"": {messageJson}
                }}";
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<Update>(updateJson, options);
        }

        private string BuildMessageJson(
            int messageId,
            long chatId,
            long userId,
            string text,
            int? replyToMessageId = null,
            long? replyFromUserId = null,
            int? threadId = null,
            bool isEdited = false)
        {
            var json = $@"{{
                ""message_id"": {messageId},
                ""chat"": {{
                    ""id"": {chatId},
                    ""type"": ""supergroup""
                }},
                ""from"": {{
                    ""id"": {userId},
                    ""first_name"": ""Test"",
                    ""is_bot"": false
                }},
                ""date"": {DateTimeOffset.UtcNow.ToUnixTimeSeconds()},
                ""text"": ""{EscapeJson(text)}""";

            if (threadId.HasValue)
            {
                json += $@",
                ""message_thread_id"": {threadId.Value}";
            }

            if (isEdited)
            {
                json += $@",
                ""edit_date"": {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            }

            if (replyToMessageId.HasValue)
            {
                var replyFromUserIdValue = replyFromUserId ?? userId;
                json += $@",
                ""reply_to_message"": {{
                    ""message_id"": {replyToMessageId.Value},
                    ""chat"": {{
                        ""id"": {chatId},
                        ""type"": ""supergroup""
                    }},
                    ""from"": {{
                        ""id"": {replyFromUserIdValue},
                        ""first_name"": ""Test"",
                        ""is_bot"": false
                    }},
                    ""date"": {DateTimeOffset.UtcNow.ToUnixTimeSeconds()},
                    ""text"": ""Reply text""
                }}";
            }

            json += "}";
            return json;
        }

        private string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        // Обёртка для команд, чтобы они работали как IHandler
        // Обёртка для команд, чтобы они работали как IHandler
        private class TestCommandHandler : IHandler
        {
            private readonly ICommand _command;
            private readonly UserAdmin _user;
            private readonly IUserRepository _userRepository;
            private readonly string _commandName;

            public TestCommandHandler(ICommand command, UserAdmin user, IUserRepository userRepository)
            {
                _command = command;
                _user = user;
                _userRepository = userRepository;

                // Определяем имя команды по типу
                _commandName = command.GetType().Name.Replace("Command", "");
            }

            public async Task<List<Response>> HandleAsync(MessageDTO message)
            {
                return await _command.Execute(message, _user);
            }

            public bool CanHandle(MessageDTO message)
            {
                var user = _userRepository.GetUserByTargetChat(message.ChatId);
                if (user == null)
                    return false;

                var commandName = CommandTypeResolver.DetermineCommandName(message, user);

                // Сравниваем имя команды с определённым типом
                return commandName == _commandName;
            }
        }
    }
}