using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.Objects;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Domain.Interfaces;

namespace EventRegistrator.Application.States
{
    public class MenuState : IState
    {
        private readonly IMenuService _menuService;
        private readonly MenuKey _key;
        private readonly MenuContext _ctx;
        private int _page;

        private const string PagePrefix = "page_";

        public MenuState(IMenuService menuService, MenuKey key, MenuContext ctx, int startPage = 0)
        {
            _menuService = menuService;
            _key = key;
            _ctx = ctx;
            _page = startPage;
        }

        public async Task<List<Response>> Execute(MessageDTO message, UserAdmin user)
        {
            var d = _menuService.Get(_key, _ctx);
            if (message.Text.StartsWith(PagePrefix))
            {
                if (int.TryParse(message.Text.AsSpan(PagePrefix.Length), out var p))
                    _page = Math.Max(0, p);
                return [await Handle(message, user)];
            }
            var extra = d.Extras.FirstOrDefault(x => x.Callback == message.Text);
            if (extra is not null)
                return await ApplyAction(extra.Action(_ctx), message, user);

            if (d.GetItems is not null && d.OnItem is not null)
            {
                var items = d.GetItems();
                var selected = items.FirstOrDefault(i => i.Callback == message.Text);
                if (selected is not null)
                    return await ApplyAction(d.OnItem(selected), message, user);
            }
            return new List<Response>();
        }

        public async Task<Response> Handle(MessageDTO message, UserAdmin user)
        {
            var d = _menuService.Get(_key, _ctx);
            var items = d.GetItems?.Invoke() ?? Array.Empty<IPagiable>();

            var maxPage = Math.Max(0, (int)Math.Ceiling(items.Count / (double)Math.Max(1, d.PageSize)) - 1);
            if (_page > maxPage) _page = maxPage;

            var buttons = new List<List<Button>>();

            if (d.GetItems is not null)
            {
                var pageItems = items.Skip(_page * d.PageSize).Take(d.PageSize);
                int rowSize = d.RowSize;
                var currentRow = new List<Button>();
                int currentCount = 0;

                foreach (var it in pageItems)
                {
                    currentRow.Add(new Button(it.Name, it.Callback));
                    currentCount++;

                    if (currentCount >= rowSize)
                    {
                        buttons.Add(currentRow);
                        currentRow = new List<Button>();
                        currentCount = 0;
                    }
                }

                if (currentRow.Count > 0)
                {
                    buttons.Add(currentRow);
                }

                AddNavigationButtons(buttons, maxPage);
            }

            foreach (var ex in d.Extras)
                buttons.Add(new() { new(ex.Label, ex.Callback) });

            var pageCounterText = maxPage < 2 ? "" : $"\nСтр. {_page + 1}/{Math.Max(1, maxPage + 1)}";

            user.CurrentContext = _ctx;
            if (user.LastMessageId == null)
            {
                return await Task.FromResult(new Response
                {
                    ChatId = message.ChatId,
                    Text = d.GetItems is null
                    ? d.Title.Invoke(_ctx)
                    : $"{d.Title.Invoke(_ctx)}" + pageCounterText,
                    ButtonData = new ButtonData(buttons),
                    SaveMessageIdCallback = id => user.LastMessageId = id,
                });
            }

            return await Task.FromResult(new Response
            {
                ChatId = message.ChatId,
                Text = d.GetItems is null
                    ? d.Title.Invoke(_ctx)
                    : $"{d.Title.Invoke(_ctx)}" + pageCounterText,
                ButtonData = new ButtonData(buttons),
                MessageToEditId = user.LastMessageId,
            });
        }

        private void AddNavigationButtons(List<List<Button>> buttons, int maxPage)
        {
            var nav = new List<Button>();
            if (_page > 0) nav.Add(new Button("⬅️", $"{PagePrefix}{_page - 1}"));
            if (_page < maxPage) nav.Add(new Button("➡️", $"{PagePrefix}{_page + 1}"));
            if (nav.Count > 0) buttons.Add(nav);
        }

        private async Task<List<Response>> ApplyAction(MenuAction action, MessageDTO message, UserAdmin user)
        {
            switch (action)
            {
                case NavigateMenu nm:
                    user.SetCurrentState(new MenuState(_menuService, nm.NextKey, nm.Ctx, nm.StartPage));
                    return [await user.State.Handle(message, user)];

                case SwitchState ss:
                    user.SetCurrentState(ss.Factory());
                    return [await user.State.Handle(message, user)];

                case RunCommand rc:
                    var responses = await rc.Action(message, user);
                    return responses ?? [new Response { /* ... */ }];

                case Noop:
                default:
                    return [await Handle(message, user)];
            }
        }
    }

}
