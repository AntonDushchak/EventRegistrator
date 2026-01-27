using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.States;

namespace EventRegistrator.Application.Factories
{
    public class MenuStateFactory : IMenuStateFactory
    {
        private readonly IMenuService _menuService;
        private readonly IMenuActionHandler _menuActionHandler;

        public MenuStateFactory(IMenuService menuService, IMenuActionHandler menuActionHandler)
        {
            _menuService = menuService;
            _menuActionHandler = menuActionHandler;
        }

        public MenuState Create(MenuKey key, MenuContext ctx, int startPage = 0)
        {
            return new MenuState(
                menuService: _menuService,
                menuActionHandler: _menuActionHandler,
                key: key,
                ctx: ctx,
                startPage: startPage
            );
        }
    }
}
