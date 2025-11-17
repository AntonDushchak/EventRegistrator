using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.States;

namespace EventRegistrator.Application.Factories
{
    public class MenuStateFactory : IMenuStateFactory
    {
        private readonly IMenuService _menuService;

        public MenuStateFactory(IMenuService menuService)
        {
            _menuService = menuService;
        }

        public MenuState Create(MenuKey key, MenuContext ctx, int startPage = 0)
        {
            return new MenuState(
                menuService: _menuService,
                key: key,
                ctx: ctx,
                startPage: startPage
            );
        }
    }
}
