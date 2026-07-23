# Everlong.Settings

Standalone settings/source-generated config system. Extracted from Everlong.Nester.

Provides the runtime types for Everlong's source-generated settings pattern:

- `[Settings]`, `[Section]`, `[Setting]`, `[Coercion]` attributes
- `ISettingsStore` / `ICommittableSettingsStore` persistence abstractions
- `PropertyBacker<T>` typed backing store for individual settings
- `SettingsSnapshot<TRoot>` for atomic read-compare-write-modify scenarios
- `SettingsStoreBase` thread-safe base with async debounced persistence
- `NoOpSettingsStore` for design-time / default values

## NuGet

```
dotnet add package Everlong.Settings
```

## Usage

```csharp
using Everlong.Settings;

[Settings]
public partial class AppSettings
{
    [Setting(true)]
    public partial bool IsDarkMode { get; set; }

    [Setting(14), Range(10, 24), Coercion]
    public partial int FontSize { get; set; }

    [Section]
    public partial AccountSettings Account { get; }
}

[Section]
public partial class AccountSettings
{
    [Setting("demo@example.com")]
    public partial string Email { get; set; }
}
```

The source generator (in `Everlong.Nester`) implements `ISettingsRoot` / `ISettingsSection`, backing fields, and property accessors automatically.

## Building

```pwsh
./publish.ps1
```
