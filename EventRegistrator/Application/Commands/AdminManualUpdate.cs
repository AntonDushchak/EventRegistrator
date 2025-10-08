using EventRegistrator.Application.Commands.Attributes;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Services;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Interfaces;

namespace EventRegistrator.Application.Commands
{
    [Command("/update", "Обновление состояния")]
    public class AdminManualUpdate : AdminCommandBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ResponseManager _responseManager;

        public AdminManualUpdate(IUserRepository userRepository, ResponseManager responseManager)
        {
            _userRepository = userRepository;
            _responseManager = responseManager;
        }

        protected async override Task<List<Response>> ExecuteAdminCommand(MessageDTO message)
        {
            var user = _userRepository.GetUser(691213564);
            Console.WriteLine("найдено юзер " + 691213564);
            var events = user.GetEvents(-1001338258069).Take(2);
            Console.WriteLine("найдено ивентов " + events.Count());
            var response = new List<Response>();
            foreach (var @event in events)
            {
                var s = _responseManager.PrepareNotificationMessages(user, @event);
                response.AddRange(s);
                Console.WriteLine("ответов добавлено" + s.Count);
            }
            Console.WriteLine("всего ответов " + response.Count);
            return response;
        }
    }
}
