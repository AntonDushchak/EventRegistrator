using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.States.Menu;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Domain.Interfaces;
using EventRegistrator.Infrastructure.Utils;

namespace EventRegistrator.Application.Services
{
    public class MenuService : IMenuService
    {
        private readonly IUserRepository _userRepository;

        public MenuService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Menu Get(MenuKey key, MenuContext ctx)
        {
            return key switch
            {
                MenuKey.TargetChats => CreateTargetChatsMenu(ctx),
                MenuKey.Hashtags => CreateHashtagsMenu(ctx),
                MenuKey.HashtagDetails => CreateHashtagDetailsMenu(ctx),
                MenuKey.Events => CreateEventsMenu(ctx),
                MenuKey.EventDetailts => CreateEventDetailsMenu(ctx),
                MenuKey.List => CreateListMenu(ctx),
                _ => throw new ArgumentOutOfRangeException(nameof(key))
            };
        }

        private Menu CreateTargetChatsMenu(MenuContext ctx)
        {
            Func<IReadOnlyCollection<IPagiable>?> getItems = () =>
                _userRepository.GetUser(ctx.ChatId).GetAllTargetChats();

            return new TargetChatsMenu(ctx, getItems);
        }

        private Menu CreateHashtagsMenu(MenuContext ctx)
        {
            var user = _userRepository.GetUserByTargetChat(ctx.TargetChatId!.Value);
            var chat = user.GetTargetChat(ctx.TargetChatId.Value);

            Func<IReadOnlyCollection<IPagiable>?> getItems = () =>
                user.GetAllHashtags(ctx.TargetChatId.Value);

            return new HashtagsMenu(ctx, getItems, chat.Name);
        }

        private Menu CreateHashtagDetailsMenu(MenuContext ctx)
        {
            var user = _userRepository.GetUserByTargetChat(ctx.TargetChatId!.Value);
            var hashtag = user.GetTargetChat(ctx.TargetChatId.Value)
                              .GetHashtagByName(ctx.HashtagName!);

            var title = $"Шаблон для хэштегу #{ctx.HashtagName}\n{hashtag.TemplateText}";

            return new HashtagDetailsMenu(ctx, title);
        }

        private Menu CreateEventsMenu(MenuContext ctx)
        {
            var user = _userRepository.GetUserByTargetChat(ctx.TargetChatId!.Value);
            var chat = user.GetTargetChat(ctx.TargetChatId.Value);

            var title = $"Недавнi iвенти чату {chat.ChannelName}";

            Func<IReadOnlyCollection<IPagiable>?> getItems = () =>
                user.GetEvents(ctx.TargetChatId.Value);

            return new EventsMenu(ctx, getItems, title);
        }

        private Menu CreateEventDetailsMenu(MenuContext ctx)
        {
            var user = _userRepository.GetUserByTargetChat(ctx.TargetChatId!.Value);
            var @event = user.GetEvent(ctx.EventId!.Value);

            var title = TextFormatter.FormatRegistrationsInfo(@event);

            return new EventDetailsMenu(ctx, title);
        }

        private Menu CreateListMenu(MenuContext ctx)
        {
            var user = _userRepository.GetUserByTargetChat(ctx.TargetChatId!.Value);
            var @event = user.GetEvent(ctx.EventId!.Value);

            Func<IReadOnlyCollection<IPagiable>?> getItems = () =>
            {
                var participants = new List<ParticipantItem>();
                foreach (var slot in @event.Slots.OrderBy(s => s.Time))
                {
                    var timeStr = slot.Time.ToString(@"hh\:mm");
                    foreach (var reg in slot.GetRegistrations())
                    {
                        participants.Add(new ParticipantItem(
                            reg.Name, timeStr, reg.Name, slot.Time));
                    }
                }
                return participants;
            };

            var title = $"Видалення відбувається по імені. Вибір однієї людини, видалить всі його записи \n\n{TextFormatter.FormatRegistrationsInfo(@event)}";

            return new ListMenu(ctx, getItems, title);
        }
    }

}
