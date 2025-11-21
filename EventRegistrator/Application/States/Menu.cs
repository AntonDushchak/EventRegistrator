using EventRegistrator.Application.Commands;
using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Objects;
using EventRegistrator.Application.Services;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Domain.Interfaces;
using EventRegistrator.Infrastructure.Utils;

namespace EventRegistrator.Application.States
{
    public abstract class Menu
    {
        protected const int _maxObjPerPage = 3;
        protected const int _participantPerPage = 9;

        protected readonly MenuContext _menuContext;
        protected readonly IUserRepository _userRepository;

        protected Func<MenuContext, string> _title;
        protected string _text;
        protected MenuButtons _buttons;

        protected Menu(MenuContext menuContext, IUserRepository userRepository)
        {
            _menuContext = menuContext;
            _userRepository = userRepository;
        }

        public string GetTitle() => BuildTitle(_menuContext);
        public MenuButtons GetButtons() => BuildButtons(_menuContext);

        protected abstract string BuildTitle(MenuContext ctx);
        protected abstract MenuButtons BuildButtons(MenuContext ctx);
    }

    public class TargetChatsMenu : Menu
    {
        public TargetChatsMenu(MenuContext menuContext, IUserRepository userRepository)
            : base(menuContext, userRepository) { }

        protected override string BuildTitle(MenuContext ctx) =>
            "Виберiть канал";

        protected override MenuButtons BuildButtons(MenuContext ctx) =>
            new(
                new[]
                {
                new MenuExtra("➕ Додати чат", "chat_add",
                    _ => new SwitchState(StateType.AddChat)),
                },
                _maxObjPerPage,
                () => _userRepository.GetUser(ctx.ChatId).GetAllTargetChats(),
                ip =>
                {
                    var chat = (TargetChat)ip;
                    return new NavigateMenu(
                        NextKey: MenuKey.Hashtags,
                        Ctx: ctx with { TargetChatId = chat.Id },
                        StartPage: 0);
                });
    }

    public class HashtagsMenu : Menu
    {
        public HashtagsMenu(MenuContext menuContext, IUserRepository userRepository)
            : base(menuContext, userRepository) { }

        protected override string BuildTitle(MenuContext ctx)
        {
            var user = _userRepository.GetUserByTargetChat(ctx.TargetChatId.Value);
            var chat = user.GetTargetChat(ctx.TargetChatId.Value);
            return $"Хэштеги каналу {chat.Name}";
        }

        protected override MenuButtons BuildButtons(MenuContext ctx) =>
            new(
                new[]
                {
                new MenuExtra("➕ Додати хэштег", "tag_add",
                    _ => new SwitchState(StateType.AddHashtag)),

                new MenuExtra("Iвенти", "events",
                    _ => new NavigateMenu(MenuKey.Events, ctx)),
                },
                //new MenuExtra("🔙 Назад", "back",
                //    _ => new NavigateMenu(MenuKey.TargetChats, ctx with { TargetChatId = null }))
                _maxObjPerPage,
                () => _userRepository
                    .GetUserByTargetChat(ctx.TargetChatId.Value)
                    .GetAllHashtags(ctx.TargetChatId.Value),
                ip =>
                {
                    var tag = (Hashtag)ip;
                    return new NavigateMenu(
                        NextKey: MenuKey.HashtagDetails,
                        Ctx: ctx with { HashtagName = tag.Name });
                });
    }


    public class HashtagDetailsMenu : Menu
    {
        public HashtagDetailsMenu(MenuContext menuContext, IUserRepository userRepository) : base(menuContext, userRepository)
        {
        }

        protected override MenuButtons BuildButtons(MenuContext ctx) =>
            new(
                new[]
                {
                    new MenuExtra("Редагувати", Constants.EditTemplateText,
                        c => new SwitchState(StateType.EditTemplateText)),
                    new MenuExtra("Видалити", Constants.DeleteHashtag,
                    c => new RunCommand((message, user) => new DeleteHashtag().Execute(message, user))),
                    new MenuExtra("🔙 Назад", "back",
                        _ => new NavigateMenu(MenuKey.Hashtags, ctx with { HashtagName = null }))
                },
                _maxObjPerPage
            );



        protected override string BuildTitle(MenuContext ctx)
        {
            return $"Шаблон для хэштегу #{ctx.HashtagName}\n{_userRepository.GetUserByTargetChat(ctx.TargetChatId.Value).GetTargetChat(ctx.TargetChatId.Value).GetHashtagByName(ctx.HashtagName).TemplateText}";
        }

    }

    public class EventsMenu : Menu
    {
        public EventsMenu(MenuContext menuContext, IUserRepository userRepository) : base(menuContext, userRepository)
        {
        }

        protected override MenuButtons BuildButtons(MenuContext ctx) =>
            new(
                new[]
                {
                new MenuExtra("🔙 Назад", "back",
                    _ => new NavigateMenu(MenuKey.Hashtags, ctx))
                },
                _maxObjPerPage,
                () => _userRepository.GetUserByTargetChat(ctx.TargetChatId.Value).GetEvents(ctx.TargetChatId.Value),
                (ip) =>
                {
                    var @event = (Event)ip;
                    return new NavigateMenu(
                        NextKey: MenuKey.EventDetailts,
                        Ctx: ctx with { EventId = @event.Id }
                    );
                }
            );

        protected override string BuildTitle(MenuContext ctx)
        {
            return $"Недавнi iвенти чату {_userRepository.GetUserByTargetChat(ctx.TargetChatId.Value).GetTargetChat(ctx.TargetChatId.Value).ChannelName}";
        }
    }

    public class EventDetailtsMenu : Menu
    {
        public EventDetailtsMenu(MenuContext menuContext, IUserRepository userRepository) : base(menuContext, userRepository)
        {
        }

        protected override MenuButtons BuildButtons(MenuContext ctx) =>
            new(
                new[]
                {
                new MenuExtra("Редагувати шаблон", Constants.EditTemplateText,
                    c => new SwitchState(StateType.EditTemplateText)),
                new MenuExtra("Редагувати список", Constants.EditList,
                    c => new NavigateMenu(MenuKey.List, ctx)),
                new MenuExtra("🔙 Назад", "back",
                    _ => new NavigateMenu(MenuKey.Events, ctx with { EventId = null }))
                },
                _maxObjPerPage
            );

        protected override string BuildTitle(MenuContext ctx)
        {
            return $"{TextFormatter.FormatRegistrationsInfo(_userRepository.GetUserByTargetChat(ctx.TargetChatId.Value).GetEvent(ctx.EventId.Value))}";
        }
    }

    //public class ListMenu : Menu
    //{
    //    public ListMenu(MenuContext menuContext, IUserRepository userRepository) : base(menuContext, userRepository)
    //    {
    //    }

    //    protected override MenuButtons BuildButtons(MenuContext ctx) =>
    //        new(
    //            new[]
    //            {
    //                new MenuExtra("🔙 Назад", "back",
    //                    _ => new NavigateMenu(MenuKey.EventDetailts, ctx))
    //            },
    //            _maxObjPerPage,
    //            () =>
    //            {
    //                var @event = _userRepository.GetUserByTargetChat(ctx.TargetChatId.Value).GetEvent(ctx.EventId.Value);
    //                var participants = new List<ParticipantItem>();
    //                var slotList = @event.Slots.OrderBy(s => s.Time).ToList();
    //                foreach (var slot in slotList)
    //                {
    //                    var timeStr = slot.Time.ToString(@"hh\:mm");
    //                    var registrations = slot.GetRegistrations();

    //                    foreach (var reg in registrations)
    //                    {
    //                        participants.Add(new ParticipantItem(
    //                            reg.Name,
    //                            timeStr,
    //                            reg.Name,
    //                            slot.Time
    //                        ));
    //                    }
    //                }

    //                return participants;
    //            },
    //            (ip) =>
    //            {
    //                var participant = (ParticipantItem)ip;

    //                return new RunCommand(async (message, user) =>
    //                {
    //                    var responseManager = new ResponseManager();
    //                    var registrationService = new RegistrationService();
    //                    var command = new DeleteReigstrationsByNameCommand(responseManager, registrationService);

    //                    var deleteMessage = new MessageDTO
    //                    {
    //                        ChatId = user.CurrentContext.TargetChatId.Value,
    //                        Text = participant.ParticipantName + "-",
    //                        ThreadId = user.CurrentContext?.EventId != null
    //                            ? user.GetEvent(user.CurrentContext.EventId.Value).ThreadId : 0,
    //                        Id = message.Id
    //                    };

    //                    var deletedResponses = await command.Execute(deleteMessage, user);
    //                    user.SetCurrentState(new MenuState(this, MenuKey.List, user.CurrentContext, 0));
    //                    var updatedResponse = await user.State.Handle(message, user);
    //                    deletedResponses.Add(updatedResponse);
    //                    return deletedResponses;
    //                });
    //            }
    //        );

    //    protected override string BuildTitle(MenuContext ctx)
    //    {
    //        var @event = _userRepository.GetUserByTargetChat(ctx.TargetChatId.Value).GetEvent(ctx.EventId.Value);
    //        return $"Видалення відбувається по імені. Вибір однієї людини, видалить всі його записи \n\n{TextFormatter.FormatRegistrationsInfo(@event)}";
    //    }
    //}

    public class MenuButtons
    {
        private readonly IReadOnlyList<MenuExtra> _extras;
        private readonly int _pageSize;
        private Func<IReadOnlyCollection<IPagiable>>? _getItems;
        private Func<IPagiable, MenuAction>? _onItem;
        private readonly int _rowSize;
        private const string _pagePrefix = "page_";

        public MenuButtons(
            IReadOnlyList<MenuExtra> extras,
            int pageSize,
            Func<IReadOnlyCollection<IPagiable>>? getItems = null,
            Func<IPagiable, MenuAction>? onItem = null,
            int rowSize = 3)
        {
            _extras = extras;
            _pageSize = pageSize;
            _getItems = getItems;
            _onItem = onItem;
            _rowSize = rowSize;
        }

        public void NullFunc()
        {
            _getItems = null;
            _onItem = null;
        }

        public MenuExtra? GetMenuExtra(string callback)
        {
            var extra = _extras.FirstOrDefault(x => x.Callback == callback);
            if (extra is not null)
                return extra;
            return null;
        }

        public MenuAction? GetMenuItemAction(string callback)
        {
            if (_getItems is not null && _onItem is not null)
            {
                var items = _getItems();
                var selected = items.FirstOrDefault(i => i.Callback == callback);
                if (selected is not null)
                    return _onItem(selected);
            }
            return null;
        }

        public List<List<Button>> CreateButtons(int startPage = 0)
        {
            var items = _getItems?.Invoke() ?? Array.Empty<IPagiable>();

            var maxPage = Math.Max(0, (int)Math.Ceiling(items.Count / (double)Math.Max(1, _pageSize)) - 1);
            if (startPage > maxPage) startPage = maxPage;

            var buttons = new List<List<Button>>();

            if (_getItems is not null)
            {
                var pageItems = items.Skip(startPage * _pageSize).Take(_pageSize);
                int rowSize = _rowSize;
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

                AddNavigationButtons(buttons, maxPage, startPage);
            }

            foreach (var ex in _extras)
                buttons.Add(new() { new(ex.Label, ex.Callback) });

            var pageCounterText = maxPage < 2 ? "" : $"\nСтр. {startPage + 1}/{Math.Max(1, maxPage + 1)}";

            return buttons;
        }

        private void AddNavigationButtons(List<List<Button>> buttons, int maxPage, int startPage)
        {
            var nav = new List<Button>();
            if (startPage > 0) nav.Add(new Button("⬅️", $"{_pagePrefix}{startPage - 1}"));
            if (startPage < maxPage) nav.Add(new Button("➡️", $"{_pagePrefix}{startPage + 1}"));
            if (nav.Count > 0) buttons.Add(nav);
        }
    }
}
