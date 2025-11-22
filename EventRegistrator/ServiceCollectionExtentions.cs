using EventRegistrator.Application.Commands;
using EventRegistrator.Application.Factories;
using EventRegistrator.Application.Handlers;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.Services;
using EventRegistrator.Application.States;
using EventRegistrator.Domain.Interfaces;
using EventRegistrator.Infrastructure.Persistence;
using EventRegistrator.Infrastructure.Telegram;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace EventRegistrator
{
    public static class ServiceCollectionExtentions
    {
        public static IServiceCollection AddEventRegistrator(this IServiceCollection services, RepositoryLoader loader, UserRepository userRepository)
        {
            services.AddPersistence(loader, userRepository)
                    .AddDomainServices()
                    .AddAppFactories();

            return services;
        }

        public static IServiceCollection AddPersistence(this IServiceCollection services, RepositoryLoader loader, UserRepository userRepository)
        {
            services.AddSingleton(loader);
            services.AddSingleton<IUserRepository>(userRepository);
            return services;
        }

        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            services.AddSingleton<MessageSender>();
            services.AddSingleton<EventService>();
            services.AddSingleton<RegistrationService>();
            services.AddSingleton<ResponseManager>();
            return services;
        }

        public static IServiceCollection AddAppFactories(this IServiceCollection services)
        {
            services.AddSingleton<CommandRegistry>();
            services.AddSingleton<ICommandFactory, CommandFactory>();

            services.AddSingleton<IMenuStateFactory, MenuStateFactory>();
            services.AddSingleton<IMenuService, MenuService>();
            services.AddSingleton<IMenuActionHandler, MenuActionHandler>();
            services.AddSingleton<IStateFactory, StateFactory>();

            services.AddSingleton<PrivateMessageHandler>();
            services.AddSingleton<TargetChatMessageHandler>();
            services.AddSingleton<GeneralCallbackQueryHandler>();

            services.AddSingleton<UpdateRouter>(sp =>
                new UpdateRouter(
                    new IHandler[]
                    {
                        sp.GetRequiredService<PrivateMessageHandler>(),
                        sp.GetRequiredService<TargetChatMessageHandler>()
                    },
                    new IHandler[]
                    {
                        sp.GetRequiredService<GeneralCallbackQueryHandler>()
                    },
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UpdateRouter>>()
                ));

            services.AddSingleton<MessageHandler>();
            services.AddSingleton<CallbackQueryHandler>();

            return services;
        }
    }
}
