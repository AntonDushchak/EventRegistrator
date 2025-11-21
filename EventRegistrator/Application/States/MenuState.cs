using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.Objects;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Entities;

namespace EventRegistrator.Application.States
{
    public class MenuState : IState
    {
        private readonly IMenuService _menuService;
        private readonly IStateFactory _stateFactory;
        private MenuKey _key;
        private MenuContext _ctx;
        private int _page;

        private const string _pagePrefix = "page_";

        public MenuState(IMenuService menuService, MenuKey key, MenuContext ctx, IStateFactory stateFactory, int startPage = 0)
        {
            _menuService = menuService;
            _key = key;
            _ctx = ctx;
            _stateFactory = stateFactory;
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
            switch (action)
            {
                case NavigateMenu nm:
                    //var menu = _menuService.Get(nm.NextKey, nm.Ctx);
                    //user.CurrentContext = nm.Ctx;
                    //user.SaveMenu(menu);
                    _key = nm.NextKey;
                    _ctx = nm.Ctx;
                    return [await Handle(message, user)];

                case SwitchState ss:
                    user.SetCurrentState(_stateFactory.CreateState(ss.StateType));
                    return [await user.State.Handle(message, user)];

                case RunCommand rc:
                    var responses = await rc.Action(message, user);
                    return responses ?? [new Response { /* ... */ }];

                case Noop:
                default:
                    return [await Handle(message, user)];
            }
        }

        private Response GetResponseFromMenu(Menu menu, MenuContext menuContext)
        {
            var b = menu.GetButtons();
            var b1 = b.CreateButtons();
            var t = menu.GetTitle();
            var r = new Response
            {
                Text = t,
                ButtonData = new ButtonData(b1),
                ChatId = menuContext.ChatId,
            };


            return r;
        }
    }

}
