using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Models;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Domain.Interfaces;

namespace EventRegistrator.Application.States.Menu
{
    public class HashtagsMenu : Menu
    {
        private readonly string _chatName;

        public HashtagsMenu(MenuContext menuContext, Func<IReadOnlyCollection<IPagiable>?> getItems, string chatName)
            : base(menuContext, getItems) 
        {
            _chatName = chatName;
        }

        protected override string BuildTitle(MenuContext ctx) => $"Хэштеги каналу {_chatName}";

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
                _getItems,
                ip =>
                {
                    var tag = (Hashtag)ip;
                    return new NavigateMenu(
                        NextKey: MenuKey.HashtagDetails,
                        Ctx: ctx with { HashtagName = tag.Name });
                });
    }
}
