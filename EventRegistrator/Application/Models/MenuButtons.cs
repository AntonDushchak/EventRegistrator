using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Objects;
using EventRegistrator.Domain.Interfaces;

namespace EventRegistrator.Application.Models
{
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
