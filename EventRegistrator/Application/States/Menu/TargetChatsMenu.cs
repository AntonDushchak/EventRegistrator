using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Models;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Domain.Interfaces;

namespace EventRegistrator.Application.States.Menu
{
    public class TargetChatsMenu : Menu
    {
        public TargetChatsMenu(MenuContext menuContext, Func<IReadOnlyCollection<IPagiable>?> getItems)
            : base(menuContext, getItems) { }

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
                _getItems,
                ip =>
                {
                    var chat = (TargetChat)ip;
                    return new NavigateMenu(
                        NextKey: MenuKey.Hashtags,
                        Ctx: ctx with { TargetChatId = chat.Id },
                        StartPage: 0);
                });
    }
}
