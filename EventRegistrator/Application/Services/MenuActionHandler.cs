using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Domain.Entities;

namespace EventRegistrator.Application.Services
{
    public class MenuActionHandler : IMenuActionHandler
    {
        private readonly IStateFactory _stateFactory;
        private readonly ICommandFactory _commandFactory;

        public MenuActionHandler(IStateFactory stateFactory, ICommandFactory commandFactory)
        {
            _stateFactory = stateFactory;
            _commandFactory = commandFactory;
        }

        public async Task<List<Response>> Handle(MenuAction action, MessageDTO message, UserAdmin user)
        {
            switch (action)
            {
                case SwitchState ss:
                    var state = _stateFactory.CreateState(ss.StateType);
                    user.SetCurrentState(state);
                    return new List<Response> { await user.State.Handle(message, user) };

                case RunCommand rc:
                    var cmd = _commandFactory.CreateCommand(rc.CommandName);
                    var responses = await cmd.Execute(message, user);
                    return responses ?? new List<Response> { new Response() };

                case Noop:
                default:
                    return new List<Response>();
            }
        }
    }
}
