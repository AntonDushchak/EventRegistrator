using EventRegistrator.Domain.Entities;

namespace EventRegistrator.Domain.Interfaces
{
    public interface IUserRepository
    {
        void AddUser(long user);
        void AddUser(UserAdmin user);
        void Clear();
        UserAdmin GetUser(long id);
        UserAdmin GetUserByTargetChat(long targetChatId);
        Task Save(UserAdmin user);
    }
}