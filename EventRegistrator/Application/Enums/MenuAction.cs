using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Domain.Interfaces;

namespace EventRegistrator.Application.Enums
{
    public abstract record MenuAction;
    public record MenuContext(
        long ChatId,
        long? TargetChatId = null,
        string? HashtagName = null,
        Guid? EventId = null);

    public record NavigateMenu(MenuKey NextKey, MenuContext Ctx, int StartPage = 0) : MenuAction;
    public record SwitchState(Func<IState> Factory) : MenuAction;
    public record RunCommand(Func<MessageDTO, UserAdmin, Task<List<Response>>> Action) : MenuAction;
    public record Noop(string? Reason = null) : MenuAction;
    public record MenuExtra(string Label, string Callback, Func<MenuContext, MenuAction> Action);

    public record MenuDescriptor(
        Func<MenuContext, string> Title,
        Func<IReadOnlyCollection<IPagiable>>? GetItems,
        int PageSize,
        IReadOnlyList<MenuExtra> Extras,
        Func<IPagiable, MenuAction>? OnItem,
        int RowSize = 3);
}
