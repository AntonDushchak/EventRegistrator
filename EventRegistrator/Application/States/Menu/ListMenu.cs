using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Models;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Domain.Interfaces;

namespace EventRegistrator.Application.States.Menu
{
    public class ListMenu : Menu
    {
        private readonly string _title;

        public ListMenu(MenuContext menuContext, Func<IReadOnlyCollection<IPagiable>?> getItems, string title) 
            : base(menuContext, getItems)
        {
            _title = title;
        }

        protected override MenuButtons BuildButtons(MenuContext ctx) =>
            new(
                new[]
                {
                    new MenuExtra("🔙 Назад", "back",
                        _ => new NavigateMenu(MenuKey.EventDetailts, ctx))
                },
                _maxObjPerPage,
                _getItems,
                (ip) =>
                {
                    var participant = (ParticipantItem)ip;

                    return new RunCommand("DeleteRegistrationsByNameInPrivate");
                }
            );

        protected override string BuildTitle(MenuContext ctx) => _title;
    }
}
