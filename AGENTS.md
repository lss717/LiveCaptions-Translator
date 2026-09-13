# AGENTS.md

Guidance for agents working in this repository.

## Project

**LiveCaptions Translator** — a Windows-only WPF (.NET 8) desktop app that reads the
built-in Windows 11 Live Captions and translates them through a configurable
translation API (LLM or traditional). Single project: `LiveCaptionsTranslator.csproj`.

## Prerequisites

- Windows 11 22H2+ with the Live Captions feature available (the app drives
  `LiveCaptions.exe`).
- .NET SDK 8.0+.

## Common commands

Run from the repository root.

```powershell
dotnet restore

# Build
dotnet build LiveCaptionsTranslator.csproj -c Debug

# Format check (CI gate — must pass)
dotnet format ./LiveCaptionsTranslator.csproj --verify-no-changes --verbosity diagnostic

# Apply formatting
dotnet format ./LiveCaptionsTranslator.csproj

# Run (launches/manipulates Windows Live Captions — see Gotchas)
dotnet run --project LiveCaptionsTranslator.csproj

# Publish: x64 self-contained single file
dotnet publish LiveCaptionsTranslator.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o ./publish/x64/selfcontained

# Publish: x64 framework-dependent single file
dotnet publish LiveCaptionsTranslator.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ./publish/x64/framework
```

Swap `-r win-x64` for `-r win-arm64` for ARM64. There is **no test project** —
`dotnet test` is a no-op; verify changes by building and running the app manually.

## Project layout

```
LiveCaptionsTranslator.csproj   # single project; src/** holds the sources
src/
  App.xaml(.cs)                 # startup: applies language, starts background loops
  Translator.cs                 # core loops: SyncLoop / TranslateLoop / DisplayLoop
  apis/                         # translation backends + Windows interop
  models/                       # Setting, Caption, API config, history entry
  pages/                        # NavigationView pages: Caption / Setting / History / Info
  windows/                      # MainWindow, OverlayWindow, SettingWindow, WelcomeWindow
  controls/                     # custom controls (StrokeDecorator, SnackbarHost)
  utils/                        # helpers (LiveCaptionsHandler, WindowHandler, HistoryLogger)
  i18n/                         # LocalizationService + Strings.<culture>.xaml
```

## Conventions

- C#: nullable enabled, implicit usings. Namespaces are `LiveCaptionsTranslator` and
  `LiveCaptionsTranslator.<area>` (lowercase area: `.models`, `.apis`, `.i18n`, …).
  The folder name does **not** add a namespace segment — files under `src/pages/` are
  in namespace `LiveCaptionsTranslator`.
- UI: WPF with **WPF-UI** (Fluent). Use `ui:` controls, `SymbolIcon`, `Mica` backdrop;
  themes follow the system via `ApplicationThemeManager` / `SystemThemeWatcher`.
- Prefer editing existing files and matching the surrounding style.
- Run `dotnet format` before finishing — CI fails on formatting drift.
- Commits: Conventional Commits (`feat:`, `fix:`, `chore:`). Do not commit unless asked.

## Localization (i18n)

Runtime-switchable via `ResourceDictionary` + `DynamicResource` (no restart).

- Strings live in `src/i18n/Strings.<culture>.xaml`; `en-US` is the fallback. **Every
  dictionary must contain the same keys.**
- Keys are prefixed `L_` and named `L_<Area>_<Element>` (e.g. `L_Setting_TargetLanguage`).
- XAML: `Text="{DynamicResource L_Key}"` — works on `Text`, `Content`, `ToolTip`,
  `Title`, `PlaceholderText`, `OffContent`/`OnContent`, and `Run.Text`.
- Code / runtime messages: `LocalizationService.Instance.T("L_Key")` (or
  `T("L_Key", arg0)` for `string.Format`).
- `Setting.Language` persists the choice (`""` = follow system);
  `LocalizationService.Apply(code)` swaps the dictionary live and raises `LanguageChanged`.
- **Add a language:** add `Strings.<culture>.xaml` (all keys) → add the culture to
  `SupportedCultures` in `LocalizationService.cs` → add a `ComboBoxItem` (native name,
  `Tag`=culture) to `LanguageBox` in `SettingPage.xaml`.
- **Add a string:** add the key to ALL dictionaries, then reference it.

## Gotchas

- `DynamicResource` does **not** work on `DataGridColumn.Header` (the column is not a
  FrameworkElement). Set such headers from code (see `HistoryPage.ApplyLocalizedHeaders`).
- `Run.Text` **does** support `DynamicResource`. Preserve inline formatting by giving
  each `Run` its own key and putting leading line breaks (`&#x0A;`) in the value.
- Running the app launches and manipulates the Windows Live Captions window
  (`LiveCaptionsHandler`): it hides Live Captions and restores/kills it on exit. Expected.
- `setting.json` is written to the process working directory (next to the exe; the
  project dir during `dotnet run`). It is user state, not source.
- Compiled XAML resource URIs are `/LiveCaptionsTranslator;component/src/<path>`
  (project-relative, including `src/`).
- The CodeGraph index lives in `.codegraph/` (gitignored). Prefer codegraph tools for
  structural questions.

## CI

`.github/workflows/dotnet-build.yml` (windows-latest): restore →
`dotnet format --verify-no-changes` → publish (x64/arm64 × framework/self-contained) →
`dotnet test` → upload `./publish`. Tags matching `v*` create a GitHub Release.
