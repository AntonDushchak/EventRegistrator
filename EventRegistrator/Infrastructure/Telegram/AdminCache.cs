using Telegram.Bot;

namespace EventRegistrator.Infrastructure.Telegram
{
    public interface IAdminCache
    {
        Task InitializeAsync(IEnumerable<long> chatIds);
        bool IsAdmin(long chatId, long userId);
        Task RefreshChatAsync(long chatId);
    }

    public class AdminCache : IAdminCache
    {
        private readonly Dictionary<long, HashSet<long>> _adminsByChat = new();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly ITelegramBotClient _botClient;

        public AdminCache(ITelegramBotClient botClient)
        {
            _botClient = botClient;
        }

        public async Task InitializeAsync(IEnumerable<long> chatIds)
        {
            await _lock.WaitAsync();
            try
            {
                foreach (var chatId in chatIds)
                {
                    try
                    {
                        var admins = await _botClient.GetChatAdministrators(chatId);
                        _adminsByChat[chatId] = new HashSet<long>(admins.Select(a => a.User.Id));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load admins for chat {chatId}: {ex.Message}");
                    }
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public bool IsAdmin(long chatId, long userId)
        {
            return _adminsByChat.TryGetValue(chatId, out var admins) && admins.Contains(userId);
        }

        public async Task RefreshChatAsync(long chatId)
        {
            await _lock.WaitAsync();
            try
            {
                var admins = await _botClient.GetChatAdministrators(chatId);
                _adminsByChat[chatId] = new HashSet<long>(admins.Select(a => a.User.Id));
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}