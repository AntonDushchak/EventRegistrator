using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Models;
using EventRegistrator.Infrastructure.Utils;

namespace EventRegistrator.Application.States.Menu
{
    public class EventDetailsMenu : Menu
    {
        private readonly string _title;

        public EventDetailsMenu(MenuContext menuContext, string title) : base(menuContext)
        {
            _title = title;
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

        protected override string BuildTitle(MenuContext ctx) => _title;
    }
}
