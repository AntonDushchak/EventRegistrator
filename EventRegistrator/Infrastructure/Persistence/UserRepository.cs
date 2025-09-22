using EventRegistrator.Domain.Entities;
using EventRegistrator.Domain.Interfaces;
using Newtonsoft.Json;

namespace EventRegistrator.Infrastructure.Persistence
{
    [Serializable]
    public class UserRepository : IUserRepository
    {
        private readonly object _lock = new();
        [JsonIgnore]
        private readonly RepositoryLoader _loader;
        [JsonProperty]
        private readonly Dictionary<long, UserAdmin> _users;

        public UserRepository(RepositoryLoader loader)
        {
            _loader = loader;
            _users = new();
        }

        public void AddUser(UserAdmin user)
        {
            lock (_lock)
            {
                if (!_users.ContainsKey(user.Id))
                {
                    _users[user.Id] = user;
                }
            }
        }

        public void AddUser(long user)
        {
            lock (_lock)
            {
                if (!_users.ContainsKey(user))
                {
                    _users[user] = new UserAdmin(user);
                }
            }
        }

        public UserAdmin? GetUser(long id)
        {
            lock (_lock)
            {
                if (_users.TryGetValue(id, out UserAdmin? value))
                {
                    return value;
                }
                return null;
            }
        }

        public UserAdmin? GetUserByTargetChat(long targetChatId)
        {
            lock (_lock)
            {
                var user = _users.FirstOrDefault(u => u.Value.ContainsTargetChat(targetChatId)).Value;
                return user;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _users.Clear();
            }
        }

        public List<UserAdmin> GetAllUsers()
        {
            lock (_lock)
            {
                return _users.Values.ToList();
            }
        }

        public async Task Save(UserAdmin user)
        {
            lock (_lock)
            {
                _users[user.Id] = user;
            }

            //await _loader.SaveDataAsync(this);
        }
    }
}
