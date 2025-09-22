using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Infrastructure.Utils;

namespace EventRegistrator.Application.States
{
    public class AddHashtagState : IState
    {
        private readonly string[] _blockList = { "back", "events", "page", "tag" };
        public async Task<List<Response>> Execute(MessageDTO message, UserAdmin user)
        {
            if (IsValid(message.Text))
            {
                user.GetTargetChat(user.CurrentContext.TargetChatId.Value).AddHashtag(new Hashtag(message.Text));
            }
            user.RevertState();
            user.LastMessageId = null;
            return [await user.State.Handle(message, user)];
        }

        private bool IsValid(string text)
        {
            foreach (var item in _blockList)
            {
                if (text.StartsWith(item))
                {
                    return false;
                }
            }
            return true;
        }

        public async Task<Response> Handle(MessageDTO message, UserAdmin user)
        {
            return new Response { ChatId = message.ChatId, Text = Constants.AskForHashtag, MessageToEditId = null };
        }
    }
}