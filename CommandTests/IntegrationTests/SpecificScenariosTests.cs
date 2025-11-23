using EventRegistrator.Application.Commands;
using EventRegistrator.Application.DTOs;

namespace CommandTests.IntegrationTests
{
    [TestFixture]
    public class SpecificScenariosTests : TestBase
    {
        [SetUp]
        public void Setup()
        {
            base.Setup();

            CreateTestEvent().Wait();
        }

        private async Task CreateTestEvent()
        {
            var createEventMessage = new MessageDTO
            {
                ChatId = 123456,
                UserId = 101112,
                Id = 1000,
                Text = "Тестовое событие \n#test",
                Created = DateTime.Now
            };

            await CreateEventCommand.Execute(createEventMessage, UserAdmin);
            Event = UserAdmin.GetLastEvent();
        }

        [Test]
        public async Task EventEdit_PreventsDuplicateCreation()
        {
            int initialEventsCount = UserAdmin.GetEvents(123456).Count;

            var editEventMessage = new MessageDTO
            {
                ChatId = 123456,
                UserId = 101112,
                Id = 1000,
                Text = "Обновленное тестовое событие \n#test",
                Created = DateTime.Now,
                IsEdit = true
            };

            var response = await CreateEventCommand.Execute(editEventMessage, UserAdmin);

            int currentEventsCount = UserAdmin.GetEvents(123456).Count;
            Assert.That(currentEventsCount, Is.EqualTo(initialEventsCount), "При редактировании сообщения создался дубликат события");
        }

        [Test]
        public async Task InvalidRegistrationFormat_ReturnsEmptyResponse()
        {
            var invalidFormatMessage = new MessageDTO
            {
                ChatId = 123456,
                UserId = 201112,
                Id = 2001,
                Text = "Иван без указания слотов",
                ReplyToMessageId = Event.PostId,
                IsReply = true
            };

            var response = await RegisterCommand.Execute(invalidFormatMessage, UserAdmin);

            Assert.That(response, Is.Empty, "Неправильный формат регистрации должен приводить к пустому ответу");

            foreach (var slot in Event.Slots)
            {
                Assert.That(slot.Contains("Иван"), Is.False, "Регистрация с неправильным форматом не должна добавляться");
            }
        }

        [Test]
        public async Task PlainText_NotRegistered()
        {
            var plainTextMessage = new MessageDTO
            {
                ChatId = 123456,
                UserId = 301112,
                Id = 3001,
                Text = "Обычный текст без регистрации",
                IsReply = false
            };

            var response = await RegisterCommand.Execute(plainTextMessage, UserAdmin);

            Assert.That(response, Is.Empty, "Обычный текст не должен обрабатываться как регистрация");

            foreach (var slot in Event.Slots)
            {
                Assert.That(slot.CurrentRegistrationCount, Is.EqualTo(0), "После обычного текста не должно быть регистраций");
            }
        }

        [Test]
        public async Task RegistrationWithQuestionMark_IsProcessedCorrectly()
        {
            var questionMarkMessage = new MessageDTO
            {
                ChatId = 123456,
                UserId = 401112,
                Id = 4001,
                Text = "Петр 1 2 ?",
                ReplyToMessageId = Event.PostId,
                IsReply = true
            };

            var response = await RegisterCommand.Execute(questionMarkMessage, UserAdmin);

            Assert.That(response, Is.Empty, "Регистрация со знаком вопроса не должна обрабатываться");

            var slot1 = Event.Slots.ElementAt(0); // Слот 10:00
            var slot2 = Event.Slots.ElementAt(1); // Слот 11:00

            Assert.That(slot1.Contains("Петр"), Is.False, "Регистрация со знаком вопроса не должна добавляться в слот 1");
            Assert.That(slot2.Contains("Петр"), Is.False, "Регистрация со знаком вопроса не должна добавляться в слот 2");
        }
    }
}