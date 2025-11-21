using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.States;

namespace EventRegistrator.Application.Factories
{
    public class MenuStateFactory : IMenuStateFactory
    {
        private readonly IMenuService _menuService;
        private readonly IStateFactory _stateFactory;

        public MenuStateFactory(IMenuService menuService, IStateFactory stateFactory)
        {
            _menuService = menuService;
            _stateFactory = stateFactory;
        }

        public MenuState Create(MenuKey key, MenuContext ctx, int startPage = 0)
        {
            return new MenuState(
                menuService: _menuService,
                key: key,
                ctx: ctx,
                stateFactory: _stateFactory,
                startPage: startPage
            );
        }
    }
}
