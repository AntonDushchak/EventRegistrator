using EventRegistrator.Application.Commands;
using EventRegistrator.Application.Factories;
using EventRegistrator.Application.Handlers;
using EventRegistrator.Application.Interfaces;
using EventRegistrator.Application.Services;
using EventRegistrator.Domain.Interfaces;
using EventRegistrator.Infrastructure.Config;
using EventRegistrator.Infrastructure.Persistence;
using EventRegistrator.Infrastructure.Telegram;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace EventRegistrator
{
    internal class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private static readonly IntPtr HWND_TOPMOST = new(-1);

        private static readonly object _saveLock = new();
        private static bool _isSaving = false;
        private static readonly TimeSpan _saveInterval = TimeSpan.FromHours(6);
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        private static Timer _saveTimer;
        private static UserRepository _userRepository;
        private static RepositoryLoader _loader;
        
        static async Task Main(string[] args)
        {
            DotNetEnv.Env.Load();

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            ConfigureLogging(env);

            try
            {
                Log.Information("Starting application (env: {Env})", env);

                var apiToken = GetRequiredEnv("API_TOKEN");

                var services = new ServiceCollection();
                services.AddLogging(b => b.ClearProviders().AddSerilog(dispose: true));

                ConfigureBotClient(services, apiToken);
                RegisterAppServices(services);

                var sp = services.BuildServiceProvider();

                var bot = sp.GetRequiredService<ITelegramBotClient>();
                var botHandler = new BotHandler(
                    sp.GetRequiredService<MessageHandler>(), 
                    sp.GetRequiredService<CallbackQueryHandler>());

                if (env == "Development")
                {
                    await bot.DeleteWebhook();
                    await RunPolling(bot, botHandler);
                }
                else
                {
                    var webhookUrl = GetRequiredEnv("WEBHOOK_URL");
                    await RunWebhook(bot, botHandler, webhookUrl);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static string GetRequiredEnv(string key) => 
            Environment.GetEnvironmentVariable(key)
            ?? throw new InvalidOperationException($"{key} not set");


        private static void ConfigureBotClient(ServiceCollection services, string apiToken)
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                MaxConnectionsPerServer = 20,
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                {
                    CertificateRevocationCheckMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck
                }
            };

            var httpClient = new HttpClient(handler);
            var bot = new TelegramBotClient(apiToken, httpClient);

            services.AddSingleton<ITelegramBotClient>(bot);
        }

        private static void ConfigureLogging(string env)
        {
            var cfg = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console();

            if (env == "Development")
                cfg.WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day);

            Log.Logger = cfg.CreateLogger();
        }

        private static void MoveConsoleToSecondMonitor()
        {
            var hWnd = GetConsoleWindow();
            int x = 400;
            int y = 1200;
            SetWindowPos(hWnd, HWND_TOPMOST, x, y, 0, 0, SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        private static async Task RunPolling(ITelegramBotClient bot, BotHandler handler)
        {
            MoveConsoleToSecondMonitor();
            using var cts = new CancellationTokenSource();
            Log.Information("Starting in polling mode...");
            bot.StartReceiving(handler.HandleUpdateAsync, handler.HandleErrorAsync, cancellationToken: cts.Token);
            StartPeriodicSaving(cts.Token);
            Log.Information("Bot is running (polling). Press Ctrl+C to exit.");
            await WaitForShutdown(cts);
            Log.Information("Polling stopped.");
        }

        private static async Task RunWebhook(ITelegramBotClient bot, BotHandler handler, string webhookUrl)
        {
            using var cts = new CancellationTokenSource();
            Log.Information("Setting webhook to {Url}", webhookUrl);

            await bot.SetWebhook(webhookUrl);

            var listener = new HttpListener();
            var port = "8080";
            listener.Prefixes.Add($"http://+:{port}/");
            listener.Start();
            Log.Information("Listening HTTP on port {Port}", port);

            StartPeriodicSaving(cts.Token);
            var httpTask = HandleHttp(listener, bot, handler, cts.Token);
            var shutdownTask = WaitForShutdown(cts);
            await Task.WhenAny(httpTask, shutdownTask);

            try { listener.Stop(); } catch { /* ignore */ }
            Log.Information("Webhook stopped.");
        }

        private static void RegisterAppServices(ServiceCollection services)
        {
            //EnvLoader.LoadDefaultUser1(userRepository);
            //EnvLoader.LoadDefaultUser2(userRepository);
            //EnvLoader.LoadDefaultUser3(userRepository);
            //loader.SaveDataAsync(userRepository);
            //userRepository.Clear();
            var loader = new RepositoryLoader(EnvLoader.GetDataPath());
            var userRepository = loader.LoadData();
            _loader = loader;
            _userRepository = userRepository;

            services.AddEventRegistrator(loader, userRepository);
        }

        private static void StartPeriodicSaving(CancellationToken token)
        {
            _saveTimer = new Timer(async _ =>
            {
                if (_isSaving) return;

                lock (_saveLock)
                {
                    if (_isSaving) return;
                    _isSaving = true;
                }

                try
                {
                    Log.Information("Выполняется плановое сохранение данных...");
                    await _loader.SaveDataAsync(_userRepository);
                    Log.Information("Плановое сохранение завершено успешно");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Ошибка при выполнении планового сохранения");
                }
                finally
                {
                    _isSaving = false;
                }
            }, null, TimeSpan.Zero, _saveInterval);

            token.Register(async () =>
            {
                _saveTimer?.Dispose();
                try
                {
                    Log.Information("Выполняется финальное сохранение данных перед завершением...");

                    using var saveTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    var saveTask = _loader.SaveDataAsync(_userRepository);

                    try
                    {
                        await Task.WhenAny(saveTask, Task.Delay(TimeSpan.FromSeconds(29), saveTimeoutCts.Token));

                        if (saveTask.IsCompleted)
                        {
                            Log.Information("Финальное сохранение завершено успешно");
                        }
                        else
                        {
                            Log.Warning("Финальное сохранение не завершилось в отведенное время");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Log.Warning("Сохранение прервано по таймауту");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Ошибка при финальном сохранении данных");
                }
            });
        }

        private static async Task HandleHttp(HttpListener listener, ITelegramBotClient bot, BotHandler handler, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext? ctx = null;
                try
                {
                    ctx = await listener.GetContextAsync();
                    if (ctx.Request.HttpMethod != "POST")
                    {
                        ctx.Response.StatusCode = 200;
                        await ctx.Response.OutputStream.FlushAsync();
                        ctx.Response.Close();
                        continue;
                    }

                    using var reader = new StreamReader(ctx.Request.InputStream);
                    var body = await reader.ReadToEndAsync();

                    try 
                    {
                        var update = JsonSerializer.Deserialize<Update>(body, _jsonOptions);
                        
                        if (update != null)
                        {
                            if (update.Message != null)
                            {
                                Log.Information(
                                    "Получено сообщение: ID={MessageId}, Чат={ChatId}, От={FromUser}, Текст={Text}",
                                    update.Message.MessageId,
                                    update.Message.Chat.Id,
                                    $"{update.Message.From?.FirstName} {update.Message.From?.LastName} (@{update.Message.From?.Username})",
                                    update.Message.Text ?? update.Message.Caption ?? "[без текста]"
                                );
                            }
                            else if (update.CallbackQuery != null)
                            {
                                Log.Information(
                                    "Получен callback: Данные={Data}, От={FromUser}",
                                    update.CallbackQuery.Data,
                                    $"{update.CallbackQuery.From.FirstName} {update.CallbackQuery.From.LastName} (@{update.CallbackQuery.From.Username})"
                                );
                            }
                            else if (update.EditedMessage != null)
                            {
                                Log.Information(
                                    "Получено отредактированное сообщение: ID={MessageId}, Чат={ChatId}, Текст={Text}",
                                    update.EditedMessage.MessageId,
                                    update.EditedMessage.Chat.Id,
                                    update.EditedMessage.Text ?? update.EditedMessage.Caption ?? "[без текста]"
                                );
                            }

                            await handler.HandleUpdateAsync(bot, update, token);
                        }
                        else
                        {
                            Log.Warning("Получен POST без валидного Update");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Ошибка при обработке обновления");
                    }

                    ctx.Response.StatusCode = 200;
                    await ctx.Response.OutputStream.FlushAsync();
                    ctx.Response.Close();
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 995)
                {
                    Log.Information("HttpListener остановлен");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Необработанная ошибка в HTTP-обработчике");
                    if (ctx != null)
                    {
                        try
                        {
                            ctx.Response.StatusCode = 500;
                            await ctx.Response.OutputStream.FlushAsync();
                            ctx.Response.Close();
                        }
                        catch { /* игнорируем */ }
                    }
                }
            }
        }
        private static async Task WaitForShutdown(CancellationTokenSource cts)
        {
            EventHandler onExit = (_, _) => CancelTokenSafely(cts);
            ConsoleCancelEventHandler onCancel = (_, e) => { e.Cancel = true; CancelTokenSafely(cts); };

            AppDomain.CurrentDomain.ProcessExit += onExit;
            Console.CancelKeyPress += onCancel;
            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (TaskCanceledException) { }
            finally
            {
                AppDomain.CurrentDomain.ProcessExit -= onExit;
                Console.CancelKeyPress -= onCancel;
            }
        }

        private static void CancelTokenSafely(CancellationTokenSource cts)
        {
            if (!cts.IsCancellationRequested)
                cts.Cancel();
        }
    }
}
