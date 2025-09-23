using EventRegistrator.Application.Commands;
using EventRegistrator.Application.DTOs;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.Services;
using EventRegistrator.Domain.DTO;
using EventRegistrator.Domain.Entities;
using EventRegistrator.Infrastructure.Utils;

namespace EventRegistrator.Application.States
{
    public class EditTemplateTextState : IState
    {
        private readonly ResponseManager _responseManager;
        public EditTemplateTextState(ResponseManager responseManager)
        {
            _responseManager = responseManager;
        }

        public async Task<List<Response>> Execute(MessageDTO message, UserAdmin user)
        {
            if (user.CurrentContext.EventId.HasValue)
            {
                var @event = user.GetEvent(user.CurrentContext.EventId.Value);
                if (@event != null)
                {
                    @event.EditTemplate(message.Text);
                    user.RevertState();
                    user.LastMessageId = null;

                    var uiUpdateResponse = _responseManager.PrepareCommentUpdateMessage(@event);
                    var response = await user.State.Handle(message, user);
                    return [uiUpdateResponse, response];
                }
            }
            else
            {
                var hashtag = user.GetTargetChat(user.CurrentContext.TargetChatId.Value).GetHashtagByName(user.CurrentContext.HashtagName);
                if (hashtag != null)
                {
                    hashtag.EditTemplateText(message.Text);
                    user.RevertState();
                    user.LastMessageId = null;
                    return [await user.State.Handle(message, user)];
                }
            }
            return new List<Response>();
        }

        public async Task<Response> Handle(MessageDTO message, UserAdmin user)
        {
            return new Response { ChatId = message.ChatId, Text = Constants.AskForNewTemplate, MessageToEditId = null };
        }
    }
}
