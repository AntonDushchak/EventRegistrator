using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Entities;

namespace EventRegistrator.Application.Commands
{
    public class DeleteHashtag : ICommand
    {
        public async Task<List<Response>> Execute(MessageDTO message, UserAdmin user)
        {
            user.GetTargetChat(user.CurrentContext.TargetChatId.Value).RemoveHashtag(user.CurrentContext.HashtagName);
            var state = user.RevertState();
            ArgumentNullException.ThrowIfNull(state);
            return [await state.Handle(message, user)];
        }
    }
}
