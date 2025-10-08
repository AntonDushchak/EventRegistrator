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
                
                response.AddRange(_responseManager.PrepareNotificationMessages(user, @event));
                var slots = @event.Slots.ToList();
                Console.WriteLine("найдено слотов " + slots.Count);
                foreach (var slot in slots)
                {
                    
                    var regs = slot.GetRegistrations();
                    Console.WriteLine("найдено рег " + regs.Count);
                    foreach (var reg in regs)
                    {
                        
                        response.Add(_responseManager.CreateLikeMessage(-1001338258069, reg.MessageId));
                    }
                }
            }
            Console.WriteLine("всего ответов " + response.Count);
            return response;
        }
    }
}
