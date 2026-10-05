# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Surveil is a lightweight Windows desktop client (.NET 10 / C# 13, WinUI 3) that connects to a
UniFi Protect controller to receive real-time doorbell notifications and view live/RTSPS camera
feeds, with an eye toward supporting other camera/NVR providers later.

## Workflow

Use /implement-issue <number> to pick up a GitHub issue: plan → wait for approval → implement + tests → report done. Opening a PR is a separate, explicit step the user decides on — never done automatically.

### Automated dev-cycle

`/dev-cycle` picks the oldest `refined`-labeled issue and drives it through plan → implement → test → review via the subagents in `.claude/agents/`, stopping for approval before code and before any PR; it never merges. Run it on a loop: `/loop 5m /dev-cycle`.

`refined` is applied **by hand**; /dev-cycle moves it to `in-progress` → `in-review`. An aborted run leaves it on `in-progress` so the loop won't re-pick it.

This repo's actual labels are: `bug`, `documentation`, `enhancement`, `duplicate`, `good first issue`, `help wanted`, `invalid`, `question`, `wontfix`, `dependencies`, `github_actions`, plus bare `refined`/`in-progress`/`in-review` (no `status:`/`type:`/`area:`/`size:` prefixes). Don't invent a label scheme that doesn't exist here — check `gh label list` before applying labels to an issue.

### Branch naming

Branches follow `(main|(features|bugs|hotfix)\/[0-9]+-.+)` when tied to an issue:
- `main` — the default branch.
- `features/<issue-number>-<slug>` — new functionality.
- `bugs/<issue-number>-<slug>` — fixing incorrect/unwanted existing behavior.
- `hotfix/<issue-number>-<slug>` — urgent fixes.

Some existing branches predate this convention and don't carry an issue number (e.g.
`features/improve-workflow`) — that's historical, not something to imitate for new work tied to
an issue.

The `EnterWorktree` tool always prefixes branch names with `worktree-`, which breaks this convention. After creating a worktree, immediately rename the branch with `git branch -m <features|bugs|hotfix>/<issue-number>-<slug>` before making any commits.

## Commands

```bash
# Build the whole solution
dotnet build Surveil.slnx

# Run the WinUI app
dotnet run --project src/Surveil

# Run the tests
dotnet test tests/Surveil.Core.Tests

# Run everything except the UI tests
dotnet test Surveil.slnx -p:Platform=x64 --filter "Category!=UI"

# Run a single test
dotnet test --filter "FullyQualifiedName~ProtectEventStreamTests.SomeMethod"
```

If the MSIX signing certificate is not installed on the machine, `dotnet build Surveil.slnx`
fails at the packaging step with `SigningCertificateThumbprintNotInStore`. That failure is
environmental, not a code error. Verify builds with
`dotnet build Surveil.slnx -p:AppxPackageSigningEnabled=false` in that case, and say plainly that
packaging was not exercised.

Tests use **NUnit 5** + **Moq** (`Microsoft.NET.Test.Sdk`, `NUnit`, `NUnit3TestAdapter`, `NUnit.Analyzers`, `Moq`), not MSTest/xUnit. Use the constraint model (`Assert.That(actual, Is.EqualTo(expected))`), not `ClassicAssert`, and `[TestCase]` for data rows. In NUnit 5 `Assert.CatchAsync`/`Assert.ThrowsAsync` return a `Task` — always `await` them (NUnit2059 fails the build otherwise). `Surveil.Core.Tests` and `Surveil.Unifi.Tests` set `[assembly: FixtureLifeCycle(LifeCycle.InstancePerTestCase)]`, so constructor-initialised fields are fresh per test (NUnit otherwise reuses one fixture instance). Test data is always fake: `.invalid` hosts (e.g. `https://nvr.example.invalid`), `fake-api-key`-style keys, never LAN IPs or real settings.

`tests/Surveil.Tests/Surveil.UiTests.csproj` holds UI tests that launch the built, unpackaged `Surveil.exe` and drive it through [WinUia](https://github.com/buning-software/win-uia) (`WinUia`, `WinUia.NUnit`). `<AssemblyAttribute Include="WinUia.NUnit.UiTestAttribute" />` in that csproj applies `[UiTest]` to the whole assembly, which puts every test in category `UI` (the CI `unit-test`/`ui-test` jobs split on it) and serialises them on the desktop, so they need an interactive, unlocked desktop and `src/Surveil` built for the same configuration/platform (the exe path is baked in at build time; `SURVEIL_EXE` overrides it). Page objects live in `Application/`. `SurveilApp` (a WinUia `App`) owns the process: it launches each test against a fresh temp `SURVEIL_DATA_DIR` and exposes the window as `Shell`. `Layout` is a plain class over the main window holding the menu and window chrome; each page (e.g. `SettingsPage`) derives from `Layout`, so the menu is reachable from every page. Pages are not `App`s and are never disposed — only `SurveilApp` is. That env var makes the app read and write settings there instead of `%LOCALAPPDATA%\Surveil`, and use a separate single-instance key, so the tests run alongside a normal Surveil instance and never touch real settings. Find elements by `x:Name` (WinUI uses it as AutomationId) or an explicit `AutomationProperties.AutomationId`.

Each `Surveil.Core` internal type under test is exposed to the test project via `InternalsVisibleTo` in `Surveil.Core.csproj` (also `InternalsVisibleTo` to `DynamicProxyGenAssembly2` for Moq's dynamic proxies) rather than making everything public.

Package versions are centrally managed in `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`) — never add a `Version` attribute to a `PackageReference` in a `.csproj`; add/bump the version in `Directory.Packages.props` instead.

`Nullable` is enabled and `ImplicitUsings` is **disabled** (explicit `using`s only) on every project — match this in new projects.

## Architecture

**Layered, provider-oriented modular structure.** Currently two projects, with a provider module
(`Surveil.Unifi`) planned:

```
Surveil.Core     ← Domain/Application/Infrastructure/Services layers (see below); provider-agnostic ports live here
Surveil          ← WinUI 3 host: Views (XAML), ViewModels, composition (App.xaml.cs); depends on Surveil.Core
Surveil.Unifi    ← (planned) UniFi Protect-specific provider implementation, depends on Surveil.Core's ports
```

**What belongs in Core vs. a provider project.** Core owns the capabilities the app offers —
show a notification, show video, display cameras — and the generic shapes those capabilities
operate on, including a generic event type. A provider project owns the *details* of each
capability: its event types, their parsing and wire format, and their wording. UniFi's event
vocabulary has no place in Core.

Being generic in shape is not sufficient reason to live in Core. If only one provider needs a
type, it belongs to that provider; promote it to Core when a second provider actually needs it.

The intent going forward: `Surveil.Core` stays provider-agnostic (ports + domain + generic
services), and each concrete camera/NVR provider gets its own project (starting with
`Surveil.Unifi`) that implements those ports. `Surveil` (the app) references `Surveil.Core` and
every provider module directly. If a provider module later grows too large for one project (e.g.
it needs its own internal layering), split out a `{Provider}.Core` the same way `Surveil.Core`
was split from `Surveil` — don't do that split preemptively.

### `Surveil.Core` internal layering

```
Domain/          ← plain entities, no dependencies on anything else (Cameras/, Events/)
Application/     ← the use-case layer: Ports/ (interfaces like ICameraProvider, IProtectEventStream —
                   what the app needs, not how), plus Options/ and Settings/ (config shapes/records)
Infrastructure/  ← concrete implementations of Application/Ports/ interfaces (Http/, WebSocket/,
                   Settings/) — e.g. UnifiProtectApiClient implements ICameraProvider over HTTP
Services/        ← app-level services with no port/interface split (VLC playback, notifications,
                   snapshotting) — used directly by the app, not swapped via DI abstraction
```

Dependency rule: `Infrastructure` depends on and implements `Application/Ports` interfaces, never
the other way around. A type belongs in `Application/Options` or `Application/Settings` if it's a
config *contract* the application layer depends on; it belongs in `Infrastructure` if it's a
concrete, swappable implementation detail.

`ReloadableCameraProvider` (`Infrastructure/Http`) is the one exception worth knowing: it wraps
whatever `ICameraProvider` matches the current `VideoProviderType` setting and hot-swaps it when
settings change, so callers never need the app restarted after a settings save.

### `Surveil` (the WinUI host)

`Views/` (XAML + code-behind), `ViewModels/` (CommunityToolkit.Mvvm), and `App.xaml.cs` for
composition — DI registration, settings loading, and host startup. `App.xaml.cs` is the only
place services get registered; there's no separate composition-root project.

## Key conventions

- **No comments. At all.** No inline `//`, no `///` XML doc comments, no `<!-- -->` in XAML, no
  `// ── Section ──` dividers. Make the code self-explanatory instead; if something genuinely
  needs explaining, put it in this file or the commit message. Older files still carry XML docs
  from before this rule — leave them, but do not imitate them in new code.
- `sealed` on every class not designed for inheritance; `record` for domain entities, settings, and options types (e.g. `Camera`, `RtspsStream`, `ProtectEvent`, `AppSettings`, `VideoProviderOption`).
- Async methods take a `CancellationToken` where cancellation is meaningful (network/IO calls).
- Interfaces for anything DI needs to swap or mock (`ICameraProvider`, `IProtectEventStream`, `IAppSettingsRepository`, `ISettingsChangeNotifier`, `IDesktopNotifier`, `IVlcPlayerFactory`) — colocated with their implementation, not in a separate `Interfaces/` folder except the existing `Services/Interfaces/` (don't add new interfaces there; colocate with the concrete class instead, matching the newer `Application/Ports` pattern).
- Prefer `var` by default; no suppressing nullable warnings.
- `ImplicitUsings` disabled — write explicit `using` directives.

## UI conventions (WinUI)

Settings UI follows Microsoft's [Guidelines for app settings](https://learn.microsoft.com/en-us/windows/apps/design/app-settings/guidelines-for-app-settings):
build rows with `SettingsCard` / `SettingsExpander` from the Windows Community Toolkit, group
them under section headers in one scrolling column (**not** tabs), cap the width around
1000-1100px, apply changes immediately rather than behind a Save button, and explain a disabled
setting in its card `Description`.

### `SettingsPage.xaml` layout is load-bearing

The nesting there looks redundant and is not. Do not flatten it:

- `ScrollViewer` sets `HorizontalScrollMode="Disabled"`. Without it the ScrollViewer measures its
  content with unbounded width, the panel sizes to its children instead of stretching, and the
  page jumps sideways whenever a section is shown or hidden.
- The width cap lives on an inner `Grid` wrapper, never on the `StackPanel`. `MaxWidth` plus
  `HorizontalAlignment="Stretch"` centres once available width exceeds `MaxWidth` — that is the
  intended centring, but only works when the wrapper's width comes from the available space
  rather than from its children.
- Gutter padding goes on the outer `Grid`, not the `ScrollViewer`: `ScrollViewer.Padding` only
  applies reliably to the leading edge, leaving the right side flush against the window.

## Local secrets / hooks

**Never commit or push.** The user commits their own work. Do not run `git commit`, `git push`
or `git add`, and do not offer to — report what changed and leave the working tree for them.
Read-only git (`status`, `diff`, `log`, `show`) is fine.

A `PreToolUse` hook (`.claude/hooks/block-secrets.py`) and a `Stop` hook
(`.claude/hooks/verify-no-secrets.sh`) already guard against committing secrets in this repo —
don't bypass or disable them. `git commit` and `git push` are permission-gated (`ask`) in
`.claude/settings.json`, so they'll always prompt rather than run silently.
