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
    public record SwitchState(StateType StateType) : MenuAction;
    public record RunCommand(string CommandName) : MenuAction;
    public record Noop(string? Reason = null) : MenuAction;
    public record MenuExtra(string Label, string Callback, Func<MenuContext, MenuAction> Action);
}
