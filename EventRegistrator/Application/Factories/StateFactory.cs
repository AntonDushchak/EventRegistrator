using EventRegistrator.Application.Commands;
using EventRegistrator.Application.Enums;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.Services;
using EventRegistrator.Application.States;
using Microsoft.Extensions.DependencyInjection;

namespace EventRegistrator.Application.Factories
{
    public class StateFactory : IStateFactory
    {
        private readonly ResponseManager _responseManager;

        public StateFactory(ResponseManager responseManager)
        {
            _responseManager = responseManager;
        }

        public IState CreateState(StateType stateType)
        {
            return stateType switch
            {
                StateType.EditTemplateText => new EditTemplateTextState(_responseManager),
                StateType.AddChat => new AddChatState(),
                StateType.AddHashtag => new AddHashtagState(),

                _ => throw new ArgumentException($"Неизвестный тип состояния: {stateType}")
            };
        }
    }

    public class CommandFactory : ICommandFactory
    {
        private readonly CommandRegistry _registry;
        private readonly IServiceProvider _serviceProvider;

        public CommandFactory(CommandRegistry registry, IServiceProvider serviceProvider)
        {
            _registry = registry;
            _serviceProvider = serviceProvider;
        }

        public ICommand CreateCommand(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            var type = _registry.GetSlashCommand(name) ?? _registry.GetCallbackCommand(name);
            if (type is null)
                throw new InvalidOperationException($"Команда с именем '{name}' не зарегистрирована в CommandRegistry.");

            if (!typeof(ICommand).IsAssignableFrom(type))
                throw new InvalidOperationException($"Тип '{type.FullName}' не реализует {nameof(ICommand)}.");

            try
            {
                return (ICommand)ActivatorUtilities.CreateInstance(_serviceProvider, type);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Не удалось создать экземпляр команды '{name}' (тип: {type.FullName}). Проверьте регистрацию зависимостей и доступность конструктора. Inner: {ex.Message}",
                    ex);
            }
        }
    }
}
