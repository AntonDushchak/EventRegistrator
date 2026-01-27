using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.Objects;
using EventRegistrator.Domain.Entities;

namespace EventRegistrator.Application.States
{
    public class MenuState : IState
    {
        private readonly IMenuService _menuService;
        private readonly IMenuActionHandler _menuActionHandler;
        private MenuKey _key;
        private MenuContext _ctx;
        private int _page;

        private const string _pagePrefix = "page_";

        public MenuState(IMenuService menuService, IMenuActionHandler menuActionHandler, MenuKey key, MenuContext ctx, int startPage = 0)
        {
            _menuService = menuService;
            _menuActionHandler = menuActionHandler;
            _key = key;
            _ctx = ctx;
            _page = startPage;
        }

        public async Task<List<Response>> Execute(MessageDTO message, UserAdmin user)
        {
            var menu = _menuService.Get(_key, _ctx);
            if (message.Text.StartsWith(_pagePrefix))
            {
                if (int.TryParse(message.Text.AsSpan(_pagePrefix.Length), out var p))
                    _page = Math.Max(0, p);
                return [await Handle(message, user)];
            }
            var buttons = menu.GetButtons();
            var extraFactory = buttons.GetMenuExtra(message.Text);
            if (extraFactory is not null)
            {
                var action = extraFactory.Action(_ctx);
                if (action is not null)
                    return await ApplyAction(action, message, user);
                return new List<Response>();
            }

            var itemAction = buttons.GetMenuItemAction(message.Text);
            if (itemAction is not null)
            {
                return await ApplyAction(itemAction, message, user);
            }

            return new List<Response>();
        }

        public async Task<Response> Handle(MessageDTO message, UserAdmin user)
        {
            var menu = _menuService.Get(_key, _ctx);
            var buttons = menu.GetButtons().CreateButtons(_page);

            user.CurrentContext = _ctx;
            if (user.LastMessageId == null)
            {
                return await Task.FromResult(new Response
                {
                    ChatId = message.ChatId,
                    Text = menu.GetTitle(),
                    ButtonData = new ButtonData(buttons),
                    SaveMessageIdCallback = id => user.LastMessageId = id,
                });
            }

            return await Task.FromResult(new Response
            {
                ChatId = message.ChatId,
                Text = menu.GetTitle(),
                ButtonData = new ButtonData(buttons),
                MessageToEditId = user.LastMessageId,
            });
        }

        private async Task<List<Response>> ApplyAction(MenuAction action, MessageDTO message, UserAdmin user)
        {
            if (action is NavigateMenu nm)
            {
                _key = nm.NextKey;
                _ctx = nm.Ctx;
                _page = nm.StartPage;
                return new List<Response> { await Handle(message, user) };
            }

            return await _menuActionHandler.Handle(action, message, user);
        }  
    }
}
