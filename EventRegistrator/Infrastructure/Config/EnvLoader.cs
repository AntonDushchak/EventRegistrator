using EventRegistrator.Domain.Entities;
using EventRegistrator.Infrastructure.Persistence;

namespace EventRegistrator.Infrastructure.Config
{
    public class EnvLoader
    {
        public static void Load()
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
            var envFile = environment == "Production" ? ".env.production" : ".env";
            DotNetEnv.Env.Load(envFile);
            Console.WriteLine($"[ENV] Loaded: {envFile}");
        }

        public static string GetApiToken()
        {
            string? apiToken = Environment.GetEnvironmentVariable("API_TOKEN");
            if (string.IsNullOrEmpty(apiToken)) throw new Exception("API_TOKEN is missing in environment.");
            return apiToken;
        }

        public static string GetDataPath()
        {
            return Environment.GetEnvironmentVariable("DATA_PATH") ?? "data.json";
        }

        public static string GetListenerPrefix()
        {
            return Environment.GetEnvironmentVariable("LISTENER") ?? "http://localhost:8080/";
        }

        public static void LoadDefaultUser1(UserRepository userRepository)
        {
            var userId = long.Parse(Environment.GetEnvironmentVariable("USER1_ID"));

            var targetChat = new TargetChat(
                long.Parse(Environment.GetEnvironmentVariable("USER1_TARGETCHATID")),
                long.Parse(Environment.GetEnvironmentVariable("USER1_CHANNELID")),
                Environment.GetEnvironmentVariable("USER1_CHANNELNAME"));
            var user = new UserAdmin(userId)
            {
                PrivateChatId = userId,
            };

            user.AddTargetChat(targetChat);
            //user.AddTargetChat(new TargetChat(-123, -3456, "Test1"));
            //user.AddTargetChat(new TargetChat(-124, -3456, "Test2"));
            //user.AddTargetChat(new TargetChat(-125, -3456, "Test3"));
            //user.AddTargetChat(new TargetChat(-126, -3456, "Test4"));
            //user.AddTargetChat(new TargetChat(-127, -3456, "Test5"));
            //user.AddTargetChat(new TargetChat(-128, -3456, "Test6"));
            //user.AddTargetChat(new TargetChat(-129, -3456, "Test7"));
            userRepository.AddUser(user);
        }

        public static void LoadDefaultUser2(UserRepository userRepository)
        {
            var userId = long.Parse(Environment.GetEnvironmentVariable("USER2_ID"));

            var targetChat = new TargetChat(
                long.Parse(Environment.GetEnvironmentVariable("USER2_TARGETCHATID")),
                long.Parse(Environment.GetEnvironmentVariable("USER2_CHANNELID")),
                Environment.GetEnvironmentVariable("USER2_CHANNELNAME"));
            var user = new UserAdmin(userId)
            {
                PrivateChatId = userId,
            };

            user.AddTargetChat(targetChat);
            userRepository.AddUser(user);
        }

        public static void LoadDefaultUser3(UserRepository userRepository)
        {
            var userId = long.Parse(Environment.GetEnvironmentVariable("USER3_ID"));

            var targetChat = new TargetChat(
                long.Parse(Environment.GetEnvironmentVariable("USER3_TARGETCHATID")),
                long.Parse(Environment.GetEnvironmentVariable("USER3_CHANNELID")),
                Environment.GetEnvironmentVariable("USER3_CHANNELNAME"));
            var user = new UserAdmin(userId)
            {
                PrivateChatId = userId,
            };

            user.AddTargetChat(targetChat);
            userRepository.AddUser(user);
        }

        public static long GetAdminId()
        {
            return long.Parse(Environment.GetEnvironmentVariable("ADMIN_ID"));
        }
    }
}
