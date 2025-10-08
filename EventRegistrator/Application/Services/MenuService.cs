using EventRegistrator.Application.Commands;
using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.States;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Domain.Interfaces;
using EventRegistrator.Infrastructure.Utils;
using System.Reflection.Metadata.Ecma335;

namespace EventRegistrator.Application.Services
{
    public class MenuService : IMenuService
    {
        private readonly IUserRepository _userRepository;
        private readonly IStateFactory _stateFactory;
        private const int _maxObjPerPage = 3;
        private const int _participantPerPage = 9;
        public MenuService(IUserRepository userRepository, IStateFactory stateFactory)
        {
            _userRepository = userRepository;
            _stateFactory = stateFactory;
        }

        public MenuDescriptor Get(MenuKey key, MenuContext ctx) => key switch
        {
            MenuKey.TargetChats => new MenuDescriptor(
                Title: (ctx) => "Виберiть канал",
                GetItems: () => _userRepository.GetUser(ctx.ChatId).GetAllTargetChats(),
                PageSize: _maxObjPerPage,
                Extras: new[]
                {
                new MenuExtra("➕ Додати чат", "chat_add",
                    _ => new SwitchState(() => new AddChatState())),
                },
                OnItem: (ip) =>
                {
                    var chat = (TargetChat)ip;
                    return new NavigateMenu(
                        NextKey: MenuKey.Hashtags,
                        Ctx: ctx with { TargetChatId = chat.Id },
                        StartPage: 0
                    );
                }
            ),

            MenuKey.Hashtags => new MenuDescriptor(
                Title: (ctx) => $"Хэштеги каналу {_userRepository.GetUserByTargetChat(ctx.TargetChatId.Value).GetTargetChat(ctx.TargetChatId.Value).Name}",
                GetItems: () => _userRepository.GetUserByTargetChat(ctx.TargetChatId.Value).GetAllHashtags(ctx.TargetChatId.Value),
                PageSize: _maxObjPerPage,
                Extras: new[]
                {
                new MenuExtra("➕ Додати хэштег", "tag_add",
                    c => new SwitchState(() => new AddHashtagState())),
                new MenuExtra("Iвенти", "events",
                    _ => new NavigateMenu(MenuKey.Events, ctx)),
                //new MenuExtra("🔙 Назад", "back",
                //    _ => new NavigateMenu(MenuKey.TargetChats, ctx with { TargetChatId = null }))
                },
                OnItem: (ip) =>
                {
                    var tag = (Hashtag)ip;
                    return new NavigateMenu(
                        NextKey: MenuKey.HashtagDetails,
                        Ctx: ctx with { HashtagName = tag.Name }
                    );
                }
            ),

            MenuKey.HashtagDetails => new MenuDescriptor(
                Title: ctx =>
        $"Шаблон для хэштегу #{ctx.HashtagName}\n{_userRepository.GetUserByTargetChat(ctx.TargetChatId.Value).GetTargetChat(ctx.TargetChatId.Value).GetHashtagByName(ctx.HashtagName).TemplateText}",
                GetItems: null,
                PageSize: _maxObjPerPage,
                Extras: new[]
                {
                new MenuExtra("Редагувати", Constants.EditTemplateText,
                    c => new SwitchState(() => _stateFactory.CreateState(StateType.EditTemplateText))),
                new MenuExtra("Видалити", Constants.DeleteHashtag,
                c => new RunCommand((message, user) => new DeleteHashtag().Execute(message, user))),
                new MenuExtra("🔙 Назад", "back",
                    _ => new NavigateMenu(MenuKey.Hashtags, ctx with { HashtagName = null }))
                },
                OnItem: null
            ),

            MenuKey.Events => new MenuDescriptor(
                Title: ctx =>
        $"Недавнi iвенти чату {_userRepository.GetUserByTargetChat(ctx.TargetChatId.Value).GetTargetChat(ctx.TargetChatId.Value).ChannelName}",
                GetItems: () => _userRepository.GetUserByTargetChat(ctx.TargetChatId.Value).GetEvents(ctx.TargetChatId.Value),
                PageSize: _maxObjPerPage,
                Extras: new[]
                {
                new MenuExtra("🔙 Назад", "back",
                    _ => new NavigateMenu(MenuKey.Hashtags, ctx))
                },
                OnItem: (ip) =>
                {
                    var @event = (Event)ip;
                    return new NavigateMenu(
                        NextKey: MenuKey.EventDetailts,
                        Ctx: ctx with { EventId = @event.Id }
                    );
                }
            ),

            MenuKey.EventDetailts => new MenuDescriptor(
                Title: ctx => $"{TextFormatter.FormatRegistrationsInfo(_userRepository.GetUserByTargetChat(ctx.TargetChatId.Value).GetEvent(ctx.EventId.Value))}",
                GetItems: null,
                PageSize: _maxObjPerPage,
                Extras: new[]
                {
                new MenuExtra("Редагувати шаблон", Constants.EditTemplateText,
                    c => new SwitchState(() => _stateFactory.CreateState(StateType.EditTemplateText))),
                new MenuExtra("Редагувати список", Constants.EditList,
                    c => new NavigateMenu(MenuKey.List, ctx)),
                new MenuExtra("🔙 Назад", "back",
                    _ => new NavigateMenu(MenuKey.Events, ctx with { EventId = null }))
                },
                OnItem: null
            ),

            MenuKey.List => new MenuDescriptor(
                Title: ctx =>
                {
                    var @event = _userRepository.GetUserByTargetChat(ctx.TargetChatId.Value).GetEvent(ctx.EventId.Value);
                    return $"Видалення відбувається по імені. Вибір однієї людини, видалить всі його записи \n\n{TextFormatter.FormatRegistrationsInfo(@event)}";
                },
                GetItems: () =>
                {
                    var @event = _userRepository.GetUserByTargetChat(ctx.TargetChatId.Value).GetEvent(ctx.EventId.Value);
                    var participants = new List<ParticipantItem>();

                    foreach (var slot in @event.Slots.OrderBy(s => s.Time))
                    {
                        var timeStr = slot.Time.ToString(@"hh\:mm");
                        var registrations = slot.GetRegistrations();

                        foreach (var reg in registrations)
                        {
                            participants.Add(new ParticipantItem(
                                reg.Name,
                                timeStr,
                                reg.Name,
                                slot.Time
                            ));
                        }
                    }

                    return participants;
                },
                PageSize: _participantPerPage,
                Extras: new[]
                {
                    new MenuExtra("🔙 Назад", "back",
                        _ => new NavigateMenu(MenuKey.EventDetailts, ctx))
                },
                OnItem: (ip) =>
                {
                    var participant = (ParticipantItem)ip;

                    return new RunCommand((message, user) => {
                        var responseManager = new ResponseManager();
                        var registrationService = new RegistrationService();
                        var command = new DeleteReigstrationsByNameCommand(responseManager, registrationService);

                        var deleteMessage = new MessageDTO
                        {
                            ChatId = user.CurrentContext.TargetChatId.Value,
                            Text = participant.ParticipantName + "-",
                            ThreadId = user.CurrentContext?.EventId != null
                                ? user.GetEvent(user.CurrentContext.EventId.Value).ThreadId : 0,
                            Id = message.Id
                        };

                        var deleteResponses = command.Execute(deleteMessage, user).Result;
                        user.SetCurrentState(new MenuState(this, MenuKey.List, user.CurrentContext, 0));
                        var updatedResponse = user.State.Handle(message, user).Result;
                        deleteResponses.Add(updatedResponse);
                        return Task.FromResult(deleteResponses);
                    });
                }
            ),

            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }

}
