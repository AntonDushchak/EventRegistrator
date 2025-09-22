using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.Services;
using EventRegistrator.Application.States;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Domain.Interfaces;
using EventRegistrator.Infrastructure;

namespace EventRegistrator.Application.Commands
{
    [Command("StartMenu", "Запуск меню")]
    public sealed class StartMenuCommand : ICommand
    {
        private readonly MenuKey _key;
        private readonly IMenuStateFactory _menuStateFactory;
        public StartMenuCommand(IMenuStateFactory menuStateFactory, MenuKey key)
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

    public class MenuStateFactory : IMenuStateFactory
    {
        private readonly IStateFactory _stateFactory;
        private readonly IUserRepository _userRepository;

        public MenuStateFactory(IUserRepository userRepository, IStateFactory stateFactory)
        {
            _userRepository = userRepository;
            _stateFactory = stateFactory;
        }

        public MenuState Create(MenuKey key, MenuContext ctx, int startPage = 0)
        {
            return new MenuState(
                menuService: new MenuService(_userRepository, _stateFactory),
                key: key,
                ctx: ctx,
                startPage: startPage
            );
        }
    }
}
