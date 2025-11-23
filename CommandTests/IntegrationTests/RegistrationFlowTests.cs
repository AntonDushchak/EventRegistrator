using EventRegistrator.Application.DTOs;

namespace CommandTests.IntegrationTests
{
    [TestFixture]
    public class RegistrationFlowTests : TestBase
    {
        [Test]
        public async Task CompleteRegistrationFlow_Test()
        {
            await CreateEvent();
            await RegisterFirstUser();
            await RegisterSecondUser();
            await AttemptToRegisterInFullSlot();
            await EditRegistration();
            await DeleteRegistrationByName();
            await DeleteRegistrationByReply();

            Assert.That(Event.Slots.Sum(s => s.CurrentRegistrationCount), Is.EqualTo(0), "В конце теста остались регистрации");
        }

        private async Task CreateEvent()
        {
            var createEventMessage = new MessageDTO
            {
                ChatId = 123456,
                UserId = 101112,
                Id = CREATE_EVENT_MESSAGE_ID,
                Text = "Тестовое событие \n#test",
                Created = DateTime.Now
            };

            var createEventResponse = await CreateEventCommand.Execute(createEventMessage, UserAdmin);
            Assert.That(createEventResponse, Is.Not.Empty, "Ошибка при создании события");

            Event = UserAdmin.GetLastEvent();
            Assert.That(Event, Is.Not.Null, "Событие не было создано");
            Assert.That(Event.HashtagName, Is.EqualTo("test"), "Неверный хештег события");

            VerifyEventSlots();
        }

        private void VerifyEventSlots()
        {
            Assert.That(Event.Slots.Count, Is.EqualTo(3), "Неверное количество временных слотов");
            Assert.That(Event.Slots.ElementAt(0).Time, Is.EqualTo(new TimeSpan(10, 0, 0)), "Неверное время первого слота");
            Assert.That(Event.Slots.ElementAt(0).MaxCapacity, Is.EqualTo(2), "Неверная вместимость первого слота");
        }

        private async Task RegisterFirstUser()
        {
            var registerMessage = new MessageDTO
            {
                ChatId = 123456,
                UserId = 201112,
                Id = REGISTER_IVAN_MESSAGE_ID,
                Text = "Иван 1 2",
                ReplyToMessageId = Event.PostId,
                IsReply = true
            };

            var registerResponse = await RegisterCommand.Execute(registerMessage, UserAdmin);
            Assert.That(registerResponse, Is.Not.Empty, "Ошибка при регистрации");

            var likeMessage = registerResponse.FirstOrDefault(r => r.Like);
            Assert.That(likeMessage, Is.Not.Null, "Отсутствует сообщение лайка при регистрации");
            Assert.That(likeMessage.MessageToEditId, Is.EqualTo(REGISTER_IVAN_MESSAGE_ID), "Неверный MessageId для лайка");

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

        private async Task RegisterSecondUser()
        {
            var register2Message = new MessageDTO
            {
                ChatId = 123456,
                UserId = 301112,
                Id = REGISTER_PETR_MESSAGE_ID,
                Text = "Петр 1, 3",
                ReplyToMessageId = Event.PostId,
                IsReply = true
            };

            var register2Response = await RegisterCommand.Execute(register2Message, UserAdmin);
            Assert.That(register2Response, Is.Not.Empty, "Ошибка при регистрации второго пользователя");

            var likeMessage = register2Response.FirstOrDefault(r => r.Like);
            Assert.That(likeMessage, Is.Not.Null, "Отсутствует сообщение лайка при регистрации");
            Assert.That(likeMessage.MessageToEditId, Is.EqualTo(REGISTER_PETR_MESSAGE_ID), "Неверный MessageId для лайка");

            VerifySecondUserRegistration();
        }

        private void VerifySecondUserRegistration()
        {
            var slot1 = Event.Slots.ElementAt(0);
            Assert.That(slot1.CurrentRegistrationCount, Is.EqualTo(2), "Регистрация второго пользователя не добавлена в первый слот");
            Assert.That(slot1.Contains("Петр"), Is.True, "Нет регистрации Петра в первом слоте");
        }

        private async Task AttemptToRegisterInFullSlot()
        {
            var register3Message = new MessageDTO
            {
                ChatId = 123456,
                UserId = 401112,
                Id = REGISTER_ALEXEY_MESSAGE_ID,
                Text = "Алексей 1",
                ReplyToMessageId = Event.PostId,
                IsReply = true
            };

            var register3Response = await RegisterCommand.Execute(register3Message, UserAdmin);

            Assert.That(register3Response, Is.Empty, "Регистрация прошла успешно, хотя слот уже заполнен");

            var slot1 = Event.Slots.ElementAt(0);
            Assert.That(slot1.CurrentRegistrationCount, Is.EqualTo(2), "Неожиданное количество регистраций после попытки переполнения");
        }

        private async Task EditRegistration()
        {
            var editMessage = new MessageDTO
            {
                ChatId = 123456,
                UserId = 201112,
                Id = REGISTER_IVAN_MESSAGE_ID,
                Text = "Иван 2 3",
                ReplyToMessageId = Event.PostId,
                IsReply = true,
                IsEdit = true
            };

            var editResponse = await EditRegistrationCommand.Execute(editMessage, UserAdmin);
            Assert.That(editResponse, Is.Not.Empty, "Ошибка при редактировании регистрации");

            var unlikeMessage = editResponse.FirstOrDefault(r => r.UnLike);
            Assert.That(unlikeMessage, Is.Not.Null, "Отсутствует сообщение отмены лайка при редактировании");

            var likeMessage = editResponse.FirstOrDefault(r => r.Like);
            Assert.That(likeMessage, Is.Not.Null, "Отсутствует сообщение лайка при редактировании");
            Assert.That(likeMessage.MessageToEditId, Is.EqualTo(REGISTER_IVAN_MESSAGE_ID), "Неверный MessageId для лайка");

            VerifyEditedRegistration();
        }

        private void VerifyEditedRegistration()
        {
            var slot1 = Event.Slots.ElementAt(0);
            var slot2 = Event.Slots.ElementAt(1);

            Assert.That(slot1.Contains(201112), Is.EqualTo(false), "Регистрация не удалена из первого слота");
            Assert.That(slot2.Contains(201112), Is.EqualTo(true), "Регистрация не добавлена во второй слот");
        }

        private async Task DeleteRegistrationByName()
        {
            var deleteByNameMessage = new MessageDTO
            {
                ChatId = 123456,
                ThreadId = Event.ThreadId,
                UserId = 101112,
                Id = DELETE_BY_NAME_MESSAGE_ID,
                Text = "Петр-"
            };

            var deleteByNameResponse = await DeleteReigstrationsByNameCommand.Execute(deleteByNameMessage, UserAdmin);
            Assert.That(deleteByNameResponse, Is.Not.Empty, "Ошибка при удалении регистрации по имени");

            var unlikeMessages = deleteByNameResponse.Where(r => r.UnLike).ToList();
            Assert.That(unlikeMessages, Is.Not.Empty, "Отсутствуют сообщения отмены лайков при удалении по имени");

            var likeMessage = deleteByNameResponse.FirstOrDefault(r => r.Like);
            Assert.That(likeMessage, Is.Not.Null, "Отсутствует сообщение лайка для сообщения удаления");
            Assert.That(likeMessage.MessageToEditId, Is.EqualTo(DELETE_BY_NAME_MESSAGE_ID), "Неверный MessageId для лайка при удалении по имени");

            Assert.That(Event.Slots.All(s => !s.Contains("Петр")), Is.True, "Регистрации Петра не удалены");
        }

        private async Task DeleteRegistrationByReply()
        {
            var deleteMessage = new MessageDTO
            {
                ChatId = 123456,
                ThreadId = Event.ThreadId,
                UserId = 201112,
                Id = DELETE_BY_REPLY_MESSAGE_ID,
                ReplyToMessageId = REGISTER_IVAN_MESSAGE_ID,
                ReplyToMessage = new MessageDTO { UserId = 201112, Id = REGISTER_IVAN_MESSAGE_ID },
                IsReply = true
            };

            var deleteResponse = await DeleteRegistrationsCommand.Execute(deleteMessage, UserAdmin);
            Assert.That(deleteResponse, Is.Not.Empty, "Ошибка при удалении регистрации по ID");

            var unlikeMessage = deleteResponse.FirstOrDefault(r => r.UnLike);
            Assert.That(unlikeMessage, Is.Not.Null, "Отсутствует сообщение отмены лайка при удалении по ID");

            var likeMessage = deleteResponse.FirstOrDefault(r => r.Like);
            Assert.That(likeMessage, Is.Not.Null, "Отсутствует сообщение лайка для сообщения удаления");

            Assert.That(unlikeMessage.MessageToEditId, Is.EqualTo(REGISTER_IVAN_MESSAGE_ID), "Неверный MessageId для удаление лайка при удалении по ID");
            Assert.That(likeMessage.MessageToEditId, Is.EqualTo(DELETE_BY_REPLY_MESSAGE_ID), "Неверный MessageId для лайка при удалении по ID");

            Assert.That(Event.Slots.All(s => !s.Contains("Иван")), Is.True, "Регистрации Ивана не удалены");
        }
    }
}