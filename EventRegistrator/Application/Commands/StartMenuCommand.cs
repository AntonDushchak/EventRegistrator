using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Domain.Entities;

namespace EventRegistrator.Application.Commands
{
    [Command("StartMenu", "Запуск меню")]
    public sealed class StartMenuCommand : ICommand
    {
        private readonly MenuKey _key;
        private readonly IMenuStateFactory _menuStateFactory;
        public StartMenuCommand(IMenuStateFactory menuStateFactory, MenuKey key = MenuKey.Hashtags)
        {
            _menuStateFactory = menuStateFactory;
            _key = key;
        }

        public async Task<List<Response>> Execute(MessageDTO message, UserAdmin user)
        {
            user.ClearStateHistory();
            user.SetCurrentState(_menuStateFactory.Create(key: _key, ctx: new MenuContext(message.ChatId, user.GetAllTargetChats().First().Id), startPage: 0));
            var response = await user.State.Handle(message, user);
            return [response];
        }
    }
}
