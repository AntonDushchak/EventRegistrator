# Event Registrator

Event Registrator is a .NET 8 service that automates attendee registration via a Telegram bot and streamlines event management for channel administrators.

## Architecture and Key Components
- **Layered structure**: the solution is split into `Domain`, `Application`, and `Infrastructure`, which keeps responsibilities clear and makes testing easier.
  - `Domain` defines entities (`Event`, `Registration`, `TargetChat`, etc.) and domain interfaces.
  - `Application` implements business logic: command handlers (`CreateEventCommand`, `RegisterCommand`), services (`EventService`, `RegistrationService`), factories, and the menu state machine.
  - `Infrastructure` integrates with Telegram (`BotHandler`, `UpdateRouter`, `MessageSender`), loads configuration, and persists data.
- **Telegram update pipeline**: the `Telegram.Bot` SDK receives updates, `BotHandler` passes them to `UpdateRouter`, which dispatches requests to the proper handlers (`MessageHandler`, `CallbackQueryHandler`). Handlers execute commands via `CommandRegistry`/`CommandFactory`, and menu-driven flows rely on states like `MenuState`, `AddChatState`, etc.
- **Data storage**: `RepositoryLoader` serializes `UserRepository` to a JSON file configured by `DATA_PATH`. Periodic and final saves prevent data loss.
- **Configuration**: `DotNetEnv` loads variables from `.env` or `.env.production`. Required parameters include `API_TOKEN`, `WEBHOOK_URL` (production), and channel identifiers.
- **Logging**: Serilog writes to the console (and to `logs/log.txt` in `Development`).

## Technologies
- .NET 8, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging
- Telegram.Bot 22.x
- Serilog (Console, File sinks)
- DotNetEnv
- Newtonsoft.Json for persistence

## Testing
- `CommandTests` verify command handlers and registration flows.
- `TimeSlotParserTests` ensure time slot parsing logic works correctly.

## Quick Start
1. Create a `.env` file with `API_TOKEN`, `DATA_PATH`, and other required variables.
2. Run `dotnet run --project EventRegistrator`.
3. In `Development` the bot runs in polling mode; in production configure `WEBHOOK_URL` for webhook mode.

## Docker
- **Build the image**: `docker build -t event-registrator .` (Dockerfile is in the repo root).
- **Run the container**: `docker run --rm -e API_TOKEN=... -e WEBHOOK_URL=... -e DATA_PATH=/app/data/data.json -v $(pwd)/data:/app/data event-registrator`.
- **Environment variables**: mirror the `.env` values; the bot uses polling or webhook depending on the provided variables.
- **Persistent storage**: mount a volume to the path from `DATA_PATH` to keep registrations between restarts.


