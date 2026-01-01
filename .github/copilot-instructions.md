# GitHub Copilot Instructions for `vanosilla`

These instructions are for AI coding agents working in this repo.
Keep them concise, repo-specific, and update them when patterns change.

## Big Picture
- This repo hosts **Vanosilla**, a C# NosTale emulator based on WingsEmu/NosWings (vanilla, July 2021 snapshot).
- Main pieces:
  - `server/` – multi-service .NET 5 solution (`WingsEmu.sln`) with game, login, DB, family, bazaar, scheduler, translations, etc.
  - `server-files/` – YAML **server configuration** (gameplay, rates, act configs, etc.).
  - `server-translations/` – YAML **server-side translations** in multiple languages.
  - `client-files/` – extracted client data (.dat, lang, maps) used as static input.
- The server is heavily **plugin- and event-driven**, using DI, gRPC, Redis, PostgreSQL, MongoDB, MQTT.

## Layout & Key Projects
- `server/WingsEmu.sln` – open this for all server projects.
- `server/srcs/` main projects:
  - `GameChannel/`, `LoginServer/`, `DatabaseServer/`, `FamilyServer/`, `BazaarServer/`, `MailServer/`, `LogsServer/`, `RelationServer/`, `Scheduler/`, `TranslationsServer/` – executable services.
  - `WingsAPI.*` – shared contracts and abstractions (`Game`, `Packets`, `Packets.Handling`, `Commands`, `Data`, `Scripting`, etc.).
  - `PhoenixLib.*` – infra (DAL, caching, messaging, logging, scheduler, multilanguage).
  - `_plugins/` – game and packet logic plugins (e.g. `WingsEmu.Plugins.BasicImplementation`, `WingsEmu.Plugins.PacketHandling`, `WingsEmu.Plugins.GameEvents`).
- DI & configuration wiring:
  - `server/srcs/_plugins/WingsEmu.Plugins.BasicImplementation/GameManagersPluginCore.cs` is the **central place** where YAML configs and managers are registered.
  - New gameplay features usually require changes in this plugin for DI + config loading.

## How to Run (for build/debug-aware changes)
- Prereqs (from `server/README.md`): **.NET 5 SDK**, Docker (PostgreSQL, Redis, MongoDB, EMQX), and an IDE (VS 2022 or Rider).
- Initial setup:
  - From `server/`, run `scripts\Docker/*.ps1` to start DB/cache/broker containers.
  - From `server/`, run `scripts\update-server-files.ps1` to copy `server-files/`, `server-translations/`, and `client-files/` into `dist/`.
  - Build `Toolkit` project and run `scripts\Database\default-accounts.ps1` (or `Toolkit.exe create-accounts`) to create default users.
- Startup configuration:
  - Use the JSON from `server/README.md` with `SwitchStartupProject` (VS) or a **Compound** (Rider) to start all main services at once.
  - Each executable’s `Working directory` must point to `server/dist/<project-name>` (e.g. `dist/game-server`, `dist/login-server`).
- Game channels:
  - Additional channels and Act 4 channels are configured via **environment variables** on `GameChannel` (`GAME_SERVER_PORT`, `GAME_SERVER_CHANNEL_ID`, `GAME_SERVER_CHANNEL_TYPE`, `HTTP_LISTEN_PORT`).

## Core Architectural Patterns
- **Entity/Map ECS**:
  - `IBattleEntity` (`IPlayerEntity`, `IMonsterEntity`, `INpcEntity`, `IMateEntity`) and systems per map handle movement, AI, stats, loops.
  - Player entity is split into partials in `WingsAPI.Data` (e.g. `PlayerEntity`, `.Family`, `.Revival`, `.Skills`, `.Stats`).
  - Reusable state is stored in **components** (e.g. `IMateComponent`, `IBuffComponent`); new per-player counters/components should follow the pattern in `server/README.md` (“Entity Component System” section).
- **Configuration via YAML**:
  - All gameplay configuration lives in `server-files/*.yaml`.
  - Corresponding C# config classes live under `WingsEmu.Game.Configurations` / related namespaces.
  - Single-file configs use `services.AddFileConfiguration<T>("file_stem")` in `GameManagersPluginCore`.
  - Multi-entry configs use `services.AddMultipleConfigurationOneFile<T>("file_stem")` and often a manager interface (e.g. `ITimeSpaceConfiguration`).
  - When adding a new YAML:
    1. Add the `.yaml` to `server-files/` with a short header comment.
    2. Create a matching POCO class in an appropriate config namespace.
    3. Register it in `GameManagersPluginCore.AddDependencies` with `AddFileConfiguration` or `AddMultipleConfigurationOneFile`.
    4. Inject the config/manager via DI where needed (handlers, services).
- **Event-driven core**:
  - All major gameplay flows use `IAsyncEvent` + `IAsyncEventProcessor<T>` and `IAsyncEventPipeline`.
  - Generic event pattern is documented in `server/README.md` under **Events and Event Handlers**.
  - For **player-scoped events**, prefer inheriting from `PlayerEvent` and triggering via `IClientSession.EmitEventAsync(...)`.
  - Event handlers live under `_plugins` (e.g. `WingsEmu.Plugins.BasicImplementation`, `WingsEmu.Plugins.GameEvents`). New logic should be implemented as new `IAsyncEventProcessor<T>` in the relevant plugin, not inside core infrastructure.
- **Packet handling**:
  - Packet handlers inherit from `GenericGamePacketHandlerBase<TPacket>` in `_plugins/WingsEmu.Plugins.PacketHandling`.
  - Example: `WalkPacketHandler` in `Game/Basic/WalkPacketHandler.cs` shows how to:
    - Validate positions and speed.
    - Interact with managers (`ISacrificeManager`, `IMeditationManager`).
    - Use `IGameLanguageService` + translation keys for warnings.
  - When adding or modifying packet handlers, follow these patterns and keep state changes inside entities/manager abstractions.

## Translations & Language Usage
- All server-side text is in `server-translations/<lang>/*.yaml` (e.g. `game-dialog-key.yaml`).
- New keys are added by:
  - Extending `GameDialogKey` enum.
  - Running `scripts\translations-update.ps1` or `Toolkit.exe translations ...` to propagate the key.
  - Editing the English `.en/game-dialog-key.yaml` value; the script backfills other languages with English where untranslated.
- To send messages:
  - Use `session.GetLanguage(GameDialogKey.KEY)` or `session.GetLanguageFormat(GameDialogKey.KEY, args...)`.
  - For non-session contexts, use `IGameLanguageService.GetLanguage(GameDialogKey, RegionLanguageType)`.
  - Avoid hard-coded strings in handlers; always add a translation key instead.

## Commands
- Command system uses **Qmmands**; see `WingsAPI.Commands` and `WingsEmu.Plugins.Essentials`.
- Commands live in modules inheriting `SaltyModuleBase` (e.g. `MyCommandsModule`).
- To add commands:
  - Create a module class with `[Name]`, `[Description]`, `[RequireAuthority]` attributes as needed.
  - Add `[Command("name")]` methods returning `SaltyCommandResult` where possible.
  - Register the module in `EssentialsPlugin.OnLoad` via `_commands.AddModule<YourModule>();`.
  - Use type parsers (e.g. `MonsterDataTypeParser`) or write new ones for complex parameters; register them in `EssentialsPlugin`.

## Conventions & Dos/Don’ts for AI Agents
- **Do**:
  - Prefer adding logic in `_plugins` projects (BasicImplementation, PacketHandling, GameEvents, Essentials) over changing core libs.
  - Wire all new services/configs via DI in `GameManagersPluginCore` or the relevant plugin’s `AddDependencies`/`OnLoad` method.
  - Use existing managers/factories (`IMapManager`, `IGameItemInstanceFactory`, `IArenaManager`, etc.) instead of re-implementing core behavior.
  - Follow existing naming conventions: suffix `Event`, `EventHandler`, `Module`, `Configuration`, `Manager` appropriately.
  - Keep new YAML/config and translation changes **backwards-compatible** with existing usages where possible.
- **Don’t**:
  - Don’t change database/storage wiring (`PhoenixLib.DAL.*`) unless explicitly requested.
  - Don’t hardcode IPs/ports; respect **environment variables** described in `server/README.md` (`DATABASE_*`, `GAME_SERVER_*`, `HTTP_LISTEN_PORT`).
  - Don’t scatter game logic into executables; keep behavior in plugins and managers.
  - Don’t add new text directly in code without going through `GameDialogKey` and translation files.

## Good Starting Points for New Work
- For gameplay rules/balance: `server-files/*.yaml` + corresponding config classes and usages (search by file stem).
- For actions triggered by players: events under `_plugins/WingsEmu.Plugins.BasicImplementation` + `PlayerEvent` patterns.
- For networking/packet behavior: `_plugins/WingsEmu.Plugins.PacketHandling`.
- For commands/tooling: `WingsEmu.Plugins.Essentials` + `WingsAPI.Commands`.
- For translations and UX messaging: `server-translations/` + `GameDialogKey` enum and `IGameLanguageService`.
