using EventRegistrator.Application.Commands;
using EventRegistrator.Application.Services;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

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
                Text = "“естовое событие \n#test",
                Created = DateTime.Now
            };

            await CreateEventCommand.Execute(createEventMessage, UserAdmin);
            Event = UserAdmin.GetLastEvent();
        }

        [Test]
        public async Task EventEdit_PreventsDuplicateCreation()
        {
            // Arrange - получаем начальное количество событий
            int initialEventsCount = UserAdmin.GetEvents(123456).Count;
            
            // –едактируем сообщение с созданным событием
            var editEventMessage = new MessageDTO
            {
                ChatId = 123456,
                UserId = 101112,
                Id = 1000,
                Text = "ќбновленное тестовое событие \n#test",
                Created = DateTime.Now,
                IsEdit = true
            };

            // Act
            var response = await CreateEventCommand.Execute(editEventMessage, UserAdmin);
            
            // Assert - провер€ем, что новое событие не создалось
            int currentEventsCount = UserAdmin.GetEvents(123456).Count;
            Assert.That(currentEventsCount, Is.EqualTo(initialEventsCount), "ѕри редактировании сообщени€ создалс€ дубликат событи€");
        }

        [Test]
        public async Task InvalidRegistrationFormat_ReturnsEmptyResponse()
        {
            // Arrange - создаем сообщение с неправильным форматом регистрации
            var invalidFormatMessage = new MessageDTO
            {
                ChatId = 123456,
                UserId = 201112,
                Id = 2001,
                Text = "»ван без указани€ слотов", // Ќет указани€ номеров слотов
                ReplyToMessageId = Event.PostId,
                IsReply = true
            };

            // Act
            var response = await RegisterCommand.Execute(invalidFormatMessage, UserAdmin);
            
            // Assert
            Assert.That(response, Is.Empty, "Ќеправильный формат регистрации должен приводить к пустому ответу");
            
            // ѕровер€ем, что регистрации не добавились
            foreach (var slot in Event.Slots)
            {
                Assert.That(slot.Contains("»ван"), Is.False, "–егистраци€ с неправильным форматом не должна добавл€тьс€");
            }
        }

        [Test]
        public async Task PlainText_NotRegistered()
        {
            // Arrange - обычное сообщение без ответа на событие
            var plainTextMessage = new MessageDTO
            {
                ChatId = 123456,
                UserId = 301112,
                Id = 3001,
                Text = "ќбычный текст без регистрации",
                IsReply = false
            };

            // Act
            var response = await RegisterCommand.Execute(plainTextMessage, UserAdmin);
            
            // Assert
            Assert.That(response, Is.Empty, "ќбычный текст не должен обрабатыватьс€ как регистраци€");
            
            // ѕровер€ем, что регистрации не добавились
            foreach (var slot in Event.Slots)
            {
                Assert.That(slot.CurrentRegistrationCount, Is.EqualTo(0), "ѕосле обычного текста не должно быть регистраций");
            }
        }

        [Test]
        public async Task RegistrationWithQuestionMark_IsProcessedCorrectly()
        {
            // Arrange - сообщение с регистрацией и знаком вопроса
            var questionMarkMessage = new MessageDTO
            {
                ChatId = 123456,
                UserId = 401112,
                Id = 4001,
                Text = "ѕетр 1 2 ?", // –егистраци€ со знаком вопроса
                ReplyToMessageId = Event.PostId,
                IsReply = true
            };

            // Act
            var response = await RegisterCommand.Execute(questionMarkMessage, UserAdmin);
            
            // Assert
            Assert.That(response, Is.Empty, "–егистраци€ со знаком вопроса не должна обрабатыватьс€");
            
            // ѕровер€ем, что регистрации добавились
            var slot1 = Event.Slots.ElementAt(0); // —лот 10:00
            var slot2 = Event.Slots.ElementAt(1); // —лот 11:00
            
            Assert.That(slot1.Contains("ѕетр"), Is.False, "–егистраци€ со знаком вопроса не должна добавл€тьс€ в слот 1");
            Assert.That(slot2.Contains("ѕетр"), Is.False, "–егистраци€ со знаком вопроса не должна добавл€тьс€ в слот 2");
        }
    }
}