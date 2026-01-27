using EventRegistrator.Application.Commands;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Services;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace CommandTests.IntegrationTests
{
    public class TestBase
    {
        protected Mock<IUserRepository> UserRepositoryMock;
        protected Mock<ILogger<EditRegistrationCommand>> LoggerMock;

        protected EventService EventService;
        protected RegistrationService RegistrationService;
        protected ResponseManager ResponseManager;

        protected CreateEventCommand CreateEventCommand;
        protected RegisterCommand RegisterCommand;
        protected EditRegistrationCommand EditRegistrationCommand;
        protected DeleteRegistrationsCommand DeleteRegistrationsCommand;
        protected DeleteReigstrationsByNameCommand DeleteReigstrationsByNameCommand;

        protected UserAdmin UserAdmin;
        protected TargetChat TargetChat;
        protected Hashtag Hashtag;
        protected Event Event;

        protected const int CREATE_EVENT_MESSAGE_ID = 1000;
        protected const int REGISTER_IVAN_MESSAGE_ID = 1001;
        protected const int REGISTER_PETR_MESSAGE_ID = 1002;
        protected const int REGISTER_ALEXEY_MESSAGE_ID = 1003;
        protected const int DELETE_BY_NAME_MESSAGE_ID = 1004;
        protected const int DELETE_BY_REPLY_MESSAGE_ID = 1005;

        [SetUp]
        public virtual void Setup()
        {
            UserRepositoryMock = new Mock<IUserRepository>();
            LoggerMock = new Mock<ILogger<EditRegistrationCommand>>();

            InitializeTestData();

            ConfigureMocks();

            InitializeServices();
            InitializeCommands();
        }

        protected virtual void InitializeTestData()
        {
            UserAdmin = new UserAdmin(101112);

            TargetChat = new TargetChat(123456, 3, "Тестовый чат");
            Hashtag = new Hashtag("test");
            Hashtag.EditTemplateText("10:00 - 2 вільних місць\r\n11:00 - 2 вільних місць\r\n12:00 - 3 вільних місць");

            TargetChat.AddHashtag(Hashtag);
            UserAdmin.AddTargetChat(TargetChat);
        }

        protected virtual void ConfigureMocks()
        {
            UserRepositoryMock
                .Setup(repo => repo.GetUser(It.IsAny<long>()))
                .Returns(UserAdmin);

            UserRepositoryMock
                .Setup(repo => repo.GetUserByTargetChat(It.IsAny<long>()))
                .Returns(UserAdmin);
        }

        protected virtual void InitializeServices()
        {
            RegistrationService = new RegistrationService();
            ResponseManager = new ResponseManager();
            EventService = new EventService(UserRepositoryMock.Object);
        }

        protected virtual void InitializeCommands()
        {
            CreateEventCommand = new CreateEventCommand(EventService, ResponseManager);
            RegisterCommand = new RegisterCommand(RegistrationService, ResponseManager);
            EditRegistrationCommand = new EditRegistrationCommand(RegistrationService, ResponseManager, LoggerMock.Object);
            DeleteRegistrationsCommand = new DeleteRegistrationsCommand(RegistrationService, ResponseManager);
            DeleteReigstrationsByNameCommand = new DeleteReigstrationsByNameCommand(ResponseManager, RegistrationService);
        }

        protected async Task<Event> CreateTestEvent(string text = "Тестовое событие \n#test", int messageId = CREATE_EVENT_MESSAGE_ID)
        {
            var createEventMessage = new MessageDTO
            {
                ChatId = 123456,
                UserId = 101112,
                Id = messageId,
                Text = text,
                Created = DateTime.Now
            };

            await CreateEventCommand.Execute(createEventMessage, UserAdmin);
            return UserAdmin.GetLastEvent();
        }

        protected void VerifyEventSlots(Event @event, int expectedSlotsCount = 3)
        {
            Assert.That(@event.Slots.Count, Is.EqualTo(expectedSlotsCount), "Неверное количество временных слотов");
            Assert.That(@event.Slots.ElementAt(0).Time, Is.EqualTo(new TimeSpan(10, 0, 0)), "Неверное время первого слота");
            Assert.That(@event.Slots.ElementAt(0).MaxCapacity, Is.EqualTo(2), "Неверная вместимость первого слота");
        }
    }
}