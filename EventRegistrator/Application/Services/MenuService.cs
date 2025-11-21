using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.States;
using EventRegistrator.Domain.Interfaces;

namespace EventRegistrator.Application.Services
{
    public class MenuService : IMenuService
    {
        private readonly IUserRepository _userRepository;

        public MenuService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Menu Get(MenuKey key, MenuContext ctx) => key switch
        {
            MenuKey.TargetChats => new TargetChatsMenu(ctx, _userRepository),

            MenuKey.Hashtags => new HashtagsMenu(ctx, _userRepository),

            MenuKey.HashtagDetails => new HashtagDetailsMenu(ctx, _userRepository),

            MenuKey.Events => new EventsMenu(ctx, _userRepository),

            MenuKey.EventDetailts => new EventDetailtsMenu(ctx, _userRepository),

            //MenuKey.List => new ListMenu(ctx, _userRepository),

            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }

}
