using EventRegistrator.Application.Commands;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Services;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Entities;
using Moq;

namespace CommandTests
{
    [TestFixture]
    public class DeleteRegistrationsCommandTests
    {
        private Mock<RegistrationService> _registrationServiceMock;
        private Mock<ResponseManager> _responseManagerMock;
        private DeleteRegistrationsCommand _command;
        private MessageDTO _message;
        private UserAdmin _userAdmin;
        private Event _event;

        [SetUp]
        public void Setup()
        {
            // Создаем моки для зависимостей
            _registrationServiceMock = new Mock<RegistrationService>();
            _responseManagerMock = new Mock<ResponseManager>();

            // Создаем команду с моками
            _command = new DeleteRegistrationsCommand(_registrationServiceMock.Object, _responseManagerMock.Object);

            // Создаем тестовое сообщение
            _message = new MessageDTO
            {
                ChatId = 123456,
                ThreadId = 789,
                UserId = 101112,
                Id = 555
            };

            // Создаем тестовое событие
            _event = new Event("Test Event", 888, 123456, "test");

            // Создаем тестового пользователя
            _userAdmin = new UserAdmin(101112);
            _userAdmin.AddEvent(_event);
        }

        [Test]
        public async Task Execute_WhenEventIsNull_ReturnsEmptyList()
        {
            // Arrange
            var userAdmin = new UserAdmin(101113);

            // Act
            var result = await _command.Execute(_message, userAdmin);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task Execute_WithEditAndReplyToPost_CancelsRegistrationAndReturnsResponses()
        {
            // Arrange
            _message.IsEdit = true;
            _message.IsReply = true;
            _message.ReplyToMessageId = 888;

            var registrationResult = new RegistrationResult
            {
                Success = true,
                Event = _event,
                MessageIds = new List<int> { _message.Id }
            };

            _registrationServiceMock
                .Setup(s => s.CancelRegistration(_event, _message.Id))
                .Returns(registrationResult);

            _responseManagerMock
                .Setup(m => m.PrepareNotificationMessages(_userAdmin, _event))
                .Returns(new List<Response> { new Response() });

            _responseManagerMock
                .Setup(m => m.CreateUnlikeMessage(_event.TargetChatId, It.IsAny<int>()))
                .Returns(new Response());

            _responseManagerMock
                .Setup(m => m.CreateLikeMessage(_event.TargetChatId, It.IsAny<int>()))
                .Returns(new Response());

            // Act
            var result = await _command.Execute(_message, _userAdmin);

            // Assert
            Assert.That(result, Is.Not.Empty);
            _registrationServiceMock.Verify(s => s.CancelRegistration(_event, _message.Id), Times.Once);
        }

        [Test]
        public async Task Execute_WithReplyToSameUser_CancelsRegistrationAndReturnsResponses()
        {
            // Arrange
            _message.IsReply = true;
            _message.ReplyToMessageId = 111;
            _message.ReplyToMessage = new MessageDTO { UserId = _message.UserId, Id = 111 };

            var registrationResult = new RegistrationResult
            {
                Success = true,
                Event = _event,
                MessageIds = new List<int> { 111 }
            };

            _registrationServiceMock
                .Setup(s => s.CancelRegistration(_event, 111))
                .Returns(registrationResult);

            _responseManagerMock
                .Setup(m => m.PrepareNotificationMessages(_userAdmin, _event))
                .Returns(new List<Response> { new Response() });

            _responseManagerMock
                .Setup(m => m.CreateUnlikeMessage(_event.TargetChatId, It.IsAny<int>()))
                .Returns(new Response());

            _responseManagerMock
                .Setup(m => m.CreateLikeMessage(_event.TargetChatId, It.IsAny<int>()))
                .Returns(new Response());

            // Act
            var result = await _command.Execute(_message, _userAdmin);

            // Assert
            Assert.That(result, Is.Not.Empty);
            _registrationServiceMock.Verify(s => s.CancelRegistration(_event, 555), Times.Once);
        }

        [Test]
        public async Task Execute_WhenCancelRegistrationFails_ReturnsEmptyList()
        {
            // Arrange
            _message.IsReply = true;
            _message.ReplyToMessageId = 888;
            _message.ReplyToMessage = new MessageDTO { UserId = _message.UserId };

            var registrationResult = new RegistrationResult
            {
                Success = false,
                Event = _event
            };

            _registrationServiceMock
                .Setup(s => s.CancelRegistration(_event, 888))
                .Returns(registrationResult);

            // Act
            var result = await _command.Execute(_message, _userAdmin);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task Execute_WithInvalidConditions_ReturnsEmptyList()
        {
            // Arrange - сообщение без признаков редактирования и не является ответом
            _message.IsEdit = false;
            _message.IsReply = false;

            // Act
            var result = await _command.Execute(_message, _userAdmin);

            // Assert
            Assert.That(result, Is.Empty);
        }
    }
}