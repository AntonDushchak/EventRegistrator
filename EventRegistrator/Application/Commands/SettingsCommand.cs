using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Domain;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Entities;

namespace EventRegistrator.Application.Commands
{
    [Command("/settings", "Настройки")]
    public class SettingsCommand : ICommand
    {
        private readonly IMenuStateFactory _menuStateFactory;

        public SettingsCommand(IMenuStateFactory menuStateFactory)
        {
            _menuStateFactory = menuStateFactory;
        }

        public async Task<List<Response>> Execute(MessageDTO message, UserAdmin user)
        {
            var state = new StartMenuCommand(_menuStateFactory, MenuKey.Hashtags);
            user.LastMessageId = null;
            user.ClearStateHistory();
            var response = await state.Execute(message, user);
            if (response.Count != 1)
            {
                Console.WriteLine("Ошибка. Нетипичный ответ");
                return [];
            }
            response.First().SaveMessageIdCallback = id => user.LastMessageId = id;
            return response;
        }
    }
}
