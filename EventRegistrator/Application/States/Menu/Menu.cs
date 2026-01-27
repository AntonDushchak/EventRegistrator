using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Models;
using EventRegistrator.Domain.Interfaces;

namespace EventRegistrator.Application.States.Menu
{
    public abstract class Menu
    {
        protected const int _maxObjPerPage = 3;
        protected const int _participantPerPage = 9;

        protected readonly MenuContext _menuContext;
        protected readonly Func<IReadOnlyCollection<IPagiable>?> _getItems;

        protected Menu(MenuContext menuContext, Func<IReadOnlyCollection<IPagiable>?> getItems = null)
        {
            _menuContext = menuContext;
            _getItems = getItems ?? (() => null);
        }

        public string GetTitle() => BuildTitle(_menuContext);
        public MenuButtons GetButtons() => BuildButtons(_menuContext);

        protected abstract string BuildTitle(MenuContext ctx);
        protected abstract MenuButtons BuildButtons(MenuContext ctx);
    }
}
