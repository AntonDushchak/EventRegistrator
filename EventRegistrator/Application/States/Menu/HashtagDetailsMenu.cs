using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Models;
using EventRegistrator.Infrastructure.Utils;

namespace EventRegistrator.Application.States.Menu
{
    public class HashtagDetailsMenu : Menu
    {
        private readonly string _title;

        public HashtagDetailsMenu(MenuContext menuContext, string title) : base(menuContext)
        {
            _title = title;
        }

        protected override MenuButtons BuildButtons(MenuContext ctx) =>
            new(
                new[]
                {
                    new MenuExtra("Редагувати", Constants.EditTemplateText,
                        c => new SwitchState(StateType.EditTemplateText)),
                    new MenuExtra("Видалити", Constants.DeleteHashtag,
                    c => new RunCommand(Constants.DeleteHashtag)),
                    new MenuExtra("🔙 Назад", "back",
                        _ => new NavigateMenu(MenuKey.Hashtags, ctx with { HashtagName = null }))
                },
                _maxObjPerPage
            );



        protected override string BuildTitle(MenuContext ctx) => _title;
    }
}
