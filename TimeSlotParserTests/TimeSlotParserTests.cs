using EventRegistrator.Application.DTOs;
using EventRegistrator.Infrastructure.Utils;

namespace TimeSlotParserTests
{
    public class TimeSlotParserTests
    {
        private Dictionary<int, TimeSpan> _slotMap;

        [SetUp]
        public void Setup()
        {
            _slotMap = new Dictionary<int, TimeSpan>
            {
                { 1, new TimeSpan(10, 0, 0) }, // 10:00
                { 2, new TimeSpan(11, 0, 0) }, // 11:00
                { 3, new TimeSpan(12, 0, 0) }, // 12:00
                { 4, new TimeSpan(13, 30, 0) }, // 13:30
                { 5, new TimeSpan(15, 0, 0) }  // 15:00
            };
        }

        private MessageDTO CreateMessage(string text, long userId, DateTime eventDate, int messageId = 1)
        {
            return new MessageDTO
            {
                Text = text,
                UserId = userId,
                Created = eventDate,
                Id = messageId
            };
        }

        [Test]
        public void ParseRegistrationMessage_MultipleSlotsSingleLine_ReturnsMultipleRegistrations()
        {
            // Arrange
            var message = CreateMessage("Karlenko 1 2 3", 123456789, new DateTime(2025, 8, 8));

            // Act
            var result = TimeSlotParser.ParseRegistrationMessage(message, _slotMap);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));

            Assert.That(result[0].UserId, Is.EqualTo(123456789));
            Assert.That(result[0].Name, Is.EqualTo("Karlenko"));
            Assert.That(result[0].RegistrationOnTime, Is.EqualTo(new TimeSpan(10, 0, 0)));

            Assert.That(result[1].RegistrationOnTime, Is.EqualTo(new TimeSpan(11, 0, 0))); 
            Assert.That(result[2].RegistrationOnTime, Is.EqualTo(new TimeSpan(12, 0, 0))); 
        }

        [Test]
        public void ParseRegistrationMessage_MultipleSlotsSingleLine_ReturnsMultipleRegistrations1()
        {
            // Arrange
            var message = CreateMessage("Karlenko 1, 2, 3", 123456789, new DateTime(2025, 8, 8));

            // Act
            var result = TimeSlotParser.ParseRegistrationMessage(message, _slotMap);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[0].RegistrationOnTime, Is.EqualTo(new TimeSpan(10, 0, 0)));
            Assert.That(result[1].RegistrationOnTime, Is.EqualTo(new TimeSpan(11, 0, 0)));
            Assert.That(result[2].RegistrationOnTime, Is.EqualTo(new TimeSpan(12, 0, 0)));
        }

        [Test]
        public void ParseRegistrationMessage_MultipleSlotsSingleLine_ReturnsMultipleRegistrations2()
        {
            // Arrange
            var message = CreateMessage("Karlenko L. 1, 2, 3 \n Karlenko N. 1", 123456789, new DateTime(2025, 8, 8));

            // Act
            var result = TimeSlotParser.ParseRegistrationMessage(message, _slotMap);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(4));
            Assert.That(result[0].Name, Is.EqualTo("Karlenko L."));
            Assert.That(result[0].RegistrationOnTime, Is.EqualTo(new TimeSpan(10, 0, 0)));
            Assert.That(result[1].RegistrationOnTime, Is.EqualTo(new TimeSpan(11, 0, 0)));
            Assert.That(result[2].RegistrationOnTime, Is.EqualTo(new TimeSpan(12, 0, 0)));
            Assert.That(result[3].Name, Is.EqualTo("Karlenko N."));
            Assert.That(result[3].RegistrationOnTime, Is.EqualTo(new TimeSpan(10, 0, 0)));
        }

        [Test]
        public void ParseRegistrationMessage_MultipleSlotsSingleLine_ReturnsMultipleRegistrations3()
        {
            // Arrange
            var message = CreateMessage("Karlenko L. 10:00 11:00", 123456789, new DateTime(2025, 8, 8));

            // Act
            var result = TimeSlotParser.ParseRegistrationMessage(message, _slotMap);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].RegistrationOnTime, Is.EqualTo(new TimeSpan(10, 0, 0)));
            Assert.That(result[1].RegistrationOnTime, Is.EqualTo(new TimeSpan(11, 0, 0)));

        }
        [Test]
        public void ParseRegistrationMessage_MixedNamesAndTimes_ReturnsCorrectRegistrations()
        {
            // Arrange
            var messageText = "Karlenko L. 10:00 11:00\nKarlenko M. 10:00 11:00";
            var message = CreateMessage(messageText, 123456789, new DateTime(2025, 8, 8));

            // Act
            var result = TimeSlotParser.ParseRegistrationMessage(message, _slotMap);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(4));

            Assert.That(result[0].Name, Is.EqualTo("Karlenko L."));
            Assert.That(result[0].RegistrationOnTime, Is.EqualTo(new TimeSpan(10, 0, 0)));
            Assert.That(result[1].Name, Is.EqualTo("Karlenko L."));
            Assert.That(result[1].RegistrationOnTime, Is.EqualTo(new TimeSpan(11, 0, 0)));

            Assert.That(result[2].Name, Is.EqualTo("Karlenko M."));
            Assert.That(result[2].RegistrationOnTime, Is.EqualTo(new TimeSpan(10, 0, 0)));
            Assert.That(result[3].Name, Is.EqualTo("Karlenko M."));
            Assert.That(result[3].RegistrationOnTime, Is.EqualTo(new TimeSpan(11, 0, 0)));
        }

        [Test]
        public void ParseRegistrationMessage_MixedSlotNumbersAndTimes_ReturnsCorrectRegistrations()
        {
            // Arrange
            var messageText = "Karlenko 1 2\nTom 10:00";
            var message = CreateMessage(messageText, 123456789, new DateTime(2025, 8, 8));

            // Act
            var result = TimeSlotParser.ParseRegistrationMessage(message, _slotMap);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));

            Assert.That(result[0].Name, Is.EqualTo("Karlenko"));  
            Assert.That(result[0].RegistrationOnTime, Is.EqualTo(new TimeSpan(10, 0, 0))); 
          
            Assert.That(result[1].Name, Is.EqualTo("Karlenko"));
            Assert.That(result[1].RegistrationOnTime, Is.EqualTo(new TimeSpan(11, 0, 0))); 

            Assert.That(result[2].Name, Is.EqualTo("Tom"));
            Assert.That(result[2].RegistrationOnTime, Is.EqualTo(new TimeSpan(10, 0, 0))); 
        }

        [Test]
        public void ParseRegistrationMessage_SingleSlotWithPlusSymbol_RegistersToSingleSlot()
        {
            // Arrange
            var singleSlotMap = new Dictionary<int, TimeSpan>
            {
                { 1, new TimeSpan(10, 0, 0) }
            };
            var message = CreateMessage("Karlenko +", 123456789, new DateTime(2025, 8, 8));

            // Act
            var result = TimeSlotParser.ParseRegistrationMessage(message, singleSlotMap);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Name, Is.EqualTo("Karlenko"));
            Assert.That(result[0].RegistrationOnTime, Is.EqualTo(new TimeSpan(10, 0, 0)));
        }

        [Test]
        public void ParseRegistrationMessage_SingleSlotWithPlusWithoutSpace_RegistersToSingleSlot()
        {
            // Arrange
            var singleSlotMap = new Dictionary<int, TimeSpan>
            {
                { 1, new TimeSpan(10, 0, 0) }
            };
            var message = CreateMessage("Karlenko+", 123456789, new DateTime(2025, 8, 8));

            // Act
            var result = TimeSlotParser.ParseRegistrationMessage(message, singleSlotMap);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Name, Is.EqualTo("Karlenko"));
            Assert.That(result[0].RegistrationOnTime, Is.EqualTo(new TimeSpan(10, 0, 0)));
        }
        [Test]
        public void ParseRegistrationMessage_DifferentTimeDelimiters_ShouldWork()
        {
            // Arrange
            var messageText = "Тест1 10:00\nТест2 10.00\nТест3 10;00";
            var message = CreateMessage(messageText, 123456789, new DateTime(2025, 8, 8));

            // Act
            var result = TimeSlotParser.ParseRegistrationMessage(message, _slotMap);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));

            var expectedTime = new TimeSpan(10, 0, 0);
            Assert.That(result[0].Name, Is.EqualTo("Тест1"));
            Assert.That(result[0].RegistrationOnTime, Is.EqualTo(expectedTime));
            Assert.That(result[1].Name, Is.EqualTo("Тест2"));
            Assert.That(result[1].RegistrationOnTime, Is.EqualTo(expectedTime));
            Assert.That(result[2].Name, Is.EqualTo("Тест3"));
            Assert.That(result[2].RegistrationOnTime, Is.EqualTo(expectedTime));
        }

        [Test]
        public void ParseRegistrationMessage_CommaAsDelimiter_ShouldNotWork()
        {
            // Arrange
            var messageText = "Тест 10,00";
            var message = CreateMessage(messageText, 123456789, new DateTime(2025, 8, 8));

            // Act
            var result = TimeSlotParser.ParseRegistrationMessage(message, _slotMap);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0), "Время с запятой в качестве разделителя не должно распознаваться");
        }

        [Test]
        public void ParseRegistrationMessage_NameWithDashBeforeTime_ShouldWork()
        {
            // Arrange
            var messageText = "Тест - 10:00";
            var message = CreateMessage(messageText, 123456789, new DateTime(2025, 8, 8));

            // Act
            var result = TimeSlotParser.ParseRegistrationMessage(message, _slotMap);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Name, Is.EqualTo("Тест"));
            Assert.That(result[0].RegistrationOnTime, Is.EqualTo(new TimeSpan(10, 0, 0)));
        }

        [Test]
        public void ParseRegistrationMessage_NameWithDifferentSeparators_ShouldWork()
        {
            // Arrange
            var messageText = "Тест1 - 10:00\nТест2 : 10:00\nТест3 — 10:00\nТест4 – 10:00";
            var message = CreateMessage(messageText, 123456789, new DateTime(2025, 8, 8));

            // Act
            var result = TimeSlotParser.ParseRegistrationMessage(message, _slotMap);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(4));

            var expectedTime = new TimeSpan(10, 0, 0);
            Assert.That(result[0].Name, Is.EqualTo("Тест1"));
            Assert.That(result[0].RegistrationOnTime, Is.EqualTo(expectedTime));
            Assert.That(result[1].Name, Is.EqualTo("Тест2"));
            Assert.That(result[1].RegistrationOnTime, Is.EqualTo(expectedTime));
            Assert.That(result[2].Name, Is.EqualTo("Тест3"));
            Assert.That(result[2].RegistrationOnTime, Is.EqualTo(expectedTime));
            Assert.That(result[3].Name, Is.EqualTo("Тест4"));
            Assert.That(result[3].RegistrationOnTime, Is.EqualTo(expectedTime));
        }

        [Test]
        public void ParseRegistrationMessage_MixedDelimitersInMultipleRegistrations_ShouldWork()
        {
            // Arrange
            var messageText = "Тест1 10:00 11.00\nТест2 10;00 12:00";
            var message = CreateMessage(messageText, 123456789, new DateTime(2025, 8, 8));

            // Act
            var result = TimeSlotParser.ParseRegistrationMessage(message, _slotMap);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(4));

            Assert.That(result[0].Name, Is.EqualTo("Тест1"));
            Assert.That(result[0].RegistrationOnTime, Is.EqualTo(new TimeSpan(10, 0, 0)));
            Assert.That(result[1].Name, Is.EqualTo("Тест1"));
            Assert.That(result[1].RegistrationOnTime, Is.EqualTo(new TimeSpan(11, 0, 0)));

            Assert.That(result[2].Name, Is.EqualTo("Тест2"));
            Assert.That(result[2].RegistrationOnTime, Is.EqualTo(new TimeSpan(10, 0, 0)));
            Assert.That(result[3].Name, Is.EqualTo("Тест2"));
            Assert.That(result[3].RegistrationOnTime, Is.EqualTo(new TimeSpan(12, 0, 0)));
        }

        [Test]
        public void ParseRegistrationMessage_PlusSymbolWithMultipleSlots_ShouldNotRegister()
        {
            // Arrange
            var message = CreateMessage("Тест+", 123456789, new DateTime(2025, 8, 8));

            // Act
            var result = TimeSlotParser.ParseRegistrationMessage(message, _slotMap);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0), "Символ + не должен регистрировать пользователя, когда доступно более одного слота");
        }
    }
}