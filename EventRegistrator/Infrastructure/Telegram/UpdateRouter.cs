using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace EventRegistrator.Infrastructure.Telegram
{
    public class UpdateRouter
    {
        private readonly List<IHandler> _messageHandlers;
        private readonly List<IHandler> _callbackHandlers;
        private readonly ILogger<UpdateRouter> _logger;

        public UpdateRouter(IEnumerable<IHandler> messageHandlers, IEnumerable<IHandler> callbackHandlers, ILogger<UpdateRouter> logger)
        {
            _messageHandlers = messageHandlers.ToList();
            _callbackHandlers = callbackHandlers.ToList();
            _logger = logger;
        }

        public async Task<List<Response>> RouteMessage(MessageDTO message)
        {
            var handler = _messageHandlers.FirstOrDefault(h => h.CanHandle(message));
            if (handler != null)
            {
                return await handler.HandleAsync(message);
            }

            _logger.LogError("Cant find handler for {Message}", message);

            return new List<Response>();
        }

        public async Task<List<Response>> RouteCallback(MessageDTO message)
        {
            var handler = _callbackHandlers.FirstOrDefault(h => h.CanHandle(message));
            if (handler != null)
            {
                return await handler.HandleAsync(message);
            }

            _logger.LogError("Cant find handler for {Callback}", message);

            return new List<Response>();
        }
    }
}
