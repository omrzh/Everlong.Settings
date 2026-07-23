---
name: everlong-settings-workflow
description: Everlong.Settings — source-generated, store-backed config system. Rules for attributes, stores, snapshots, and the generator contract.
---

## 0. Mental Model

Everlong.Settings has three layers that work together:

```
┌──────────────────────────────────────────────────────┐
│  Attributes ([Settings], [Section], [Setting],       │
│  [Coercion]) — declarative wiring                    │
├──────────────────────────────────────────────────────┤
│  Source generator — implements ISettingsRoot /       │
│  ISettingsSection, generates backers + initializer   │
├──────────────────────────────────────────────────────┤
│  Runtime store (ISettingsStore) — where values       │
│  actually live (file, DB, registry, cloud, …)        │
└──────────────────────────────────────────────────────┘
```

The generator runs at compile time. It reads attributes on your partial classes and emits:
- A `PropertyBacker<T>` field per `[Setting]` property
- A `ISettingsSection.Initialize()` method that wires backers to the store
- A `Default` static property (on root `[Settings]` classes)
- A `Snapshot()` method for atomic read-commit-rollback

At runtime, you provide an `ISettingsStore` implementation. The generator never touches the store directly — it only calls `ISettingsStore.GetValue<T>()` / `SetValue<T>()` through the backer.

---

## 1. Attribute Rules

### 1.1 `[Settings]` — root group, `partial` required; `ISettingsRoot` is auto-implemented

```csharp
[Settings]
public partial class AppSettings  // ← ISettingsRoot is auto-implemented by the generator
{
    [Setting(true)]
    public partial bool IsDarkMode { get; set; }
}
```

| Required | Why |
|----------|-----|
| `partial` | Generator needs to add members (backing fields, `ISettingsRoot`, `Initialize()`, `Default`, `Snapshot()`) |

You do **not** need to write `: ISettingsRoot` on your partial class — the generator adds it automatically. The class will implement `ISettingsRoot` regardless.

**The real risk is not forgetting `ISettingsRoot` but forgetting to call `Initialize()` at all.** Without it, the `PropertyBacker<T>` fields are never wired to a store, and every setting returns its fallback value silently. The code compiles — but nothing works.

```csharp
var settings = new AppSettings();
// ❌ Initialize never called → all settings use fallbacks
Console.WriteLine(settings.IsDarkMode);  // always false, regardless of store contents

// ✅ Correct
((ISettingsRoot)settings).Initialize(myStore);
Console.WriteLine(settings.IsDarkMode);  // reads from store
```

### 1.2 `[Section]` — nested group, two forms

**On a class** (the section type itself):

```csharp
[Section]
public partial class AccountSettings
{
    [Setting("")]
    public partial string Email { get; set; }
}
```

**On a property in the parent** (declares that the parent holds a section):

```csharp
[Settings]
public partial class AppSettings : ISettingsRoot
{
    [Section]
    public partial AccountSettings Account { get; }
}
```

| Form | Requirements |
|------|-------------|
| Class-level `[Section]` | `partial` class; no interface needed (`ISettingsSection` is auto-implemented by the generator) |
| Property-level `[Section]` | `partial` property, **get-only** (no `set` or `init`), type must be a `[Section]` or `[Settings]` class |

**The property-level `[Section]` property must not have a setter.** If it does, the `SettingsPropertyAnalyzer` reports ELST2002 at compile time. If it also lacks `partial`, `PartialKeywordAnalyzer` reports ELST2004.

### 1.3 `[Setting]` — a single settings value

```csharp
[Setting]                       // → default(T), or default! for reference types
[Setting(true)]                 // → bool
[Setting("text")]               // → string
[Setting(42)]                   // → int
[Setting(SomeEnum.Fast)]        // → enum (boxed as object)
[Setting(new[] { "a", "b" })]  // → string[] (complex type)
```

| Fallback type | Supported | Notes |
|---------------|-----------|-------|
| `bool` | ✅ | |
| `int`, `long` | ✅ | |
| `float`, `double` | ✅ | |
| `decimal` | ✅ | |
| `string` | ✅ | |
| Enum (`object` overload) | ✅ | Generator validates the argument is actually an enum constant at compile time |
| `string[]` | ✅ | Complex type — no fallback via attribute; provide via `static partial GetXxxDefault()` |

**Complex types** (`string[]`, or any type that isn't a primitive/enum) cannot specify a fallback in the attribute constructor. Instead, the generator emits:

```csharp
private static partial string[] GetTagsDefault();  // you implement this
```

If you pass a primitive fallback to a complex-typed `[Setting]`, the generator reports ELST2001.

### 1.4 `[Coercion]` — custom value transformation

```csharp
[Setting(14), Range(10, 24), Coercion]
public partial int FontSize { get; set; }

// Implement in your own file:
private static partial int CoerceFontSize(int value)
    => value % 2 == 0 ? value : value + 1;  // snap to even
```

**Execution order in the generated setter:**

```
raw value → [Range] clamp → [MaxLength]/[MinLength] clamp → CoerceXxx() → stored
```

The `[Coercion]` attribute only emits a `partial` method declaration. If you don't implement it, you get a compile error — there's no default body. This is intentional: if coercion is trivial, omit `[Coercion]` entirely.

### 1.5 `[Range]`, `[MaxLength]`, `[MinLength]`, `[Length]` — inline validation from `System.ComponentModel.Annotations`

These attributes are **not** part of the `Everlong.Settings` NuGet package. They live in `System.ComponentModel.Annotations`, which the generator resolves at compile time. You can annotate any `[Setting]` property with them:

```csharp
[Setting(50), Range(0, 100)]
public partial int Volume { get; set; }

[Setting(""), MaxLength(200)]
public partial string Bio { get; set; }

[Setting(""), Length(2, 50)]
public partial string Nickname { get; set; }
```

The generator reads these at compile time and emits inline clamp code **before** the `[Coercion]` call (if present). No runtime dependency on `System.ComponentModel.Annotations` is needed.

---

## 2. Store Rules

### 2.1 `ISettingsStore` is the only contract the runtime depends on

```csharp
public interface ISettingsStore
{
    T GetValue<T>(string key, T fallback);
    void SetValue<T>(string key, T value);
}
```

Everything — `PropertyBacker<T>`, `SettingsStoreBase`, `CommittableSettingsStore` — works through this interface. If you implement it yourself, you only need these two methods.

**Keys are dot-separated paths** built from section nesting: `"Account.Email"` for a `[Setting]` named `Email` inside a `[Section]` property named `Account`. The root level has no prefix.

### 2.2 Choose the right store base

| Need | Use |
|------|-----|
| In-memory only, no persistence | `NoOpSettingsStore` (returns fallback on read, discards writes) |
| File / DB / API with async debounced writes | `SettingsStoreBase` (see §4) |
| Preview changes before persisting | Wrap with `CommittableSettingsStore` (see §3) |
| Fully custom | Implement `ISettingsStore` directly |

### 2.3 `SettingsStoreBase` threading contract

```csharp
public sealed class JsonSettingsStore : SettingsStoreBase
{
    protected override IEnumerable<KeyValuePair<string, string>> LoadAll() { … }
    protected override Task PersistAsync(string key, string value, CancellationToken ct) { … }
}
```

`SettingsStoreBase` is thread-safe for reads and writes (lock on cache + single-reader channel for persistence). Subclass responsibilities:

| Responsibility | Details |
|----------------|---------|
| `LoadAll()` | Called once on first read. Returns all persisted key-value pairs as strings. Called under lock. |
| `PersistAsync(key, value, ct)` | Single-entry persistence. Default `PersistBatchAsync` calls this per entry. |
| `PersistBatchAsync(batch, ct)` | Override for batch-efficient stores (e.g., write entire JSON file at once). Default calls `PersistAsync` per entry. |
| `OnReadValueError` / `OnPersistError` / `OnWriteQueueRejected` / `OnWriterFaulted` | Optional error hooks — noop by default. |

**Do not call `PersistAsync` directly.** Writes go through `SetValue<T>()`, which queues work on a background channel. The channel debounces (250ms default, configurable) and coalesces duplicate keys before flushing.

### 2.4 Always call `FlushAsync()` or `DisposeAsync()` before the process exits

```csharp
await store.FlushAsync();
// or
await store.DisposeAsync();
```

`SettingsStoreBase` uses a background writer. If the process exits without flushing, the most recent writes may be lost. `DisposeAsync()` calls `FlushAsync()` internally.

---

## 3. Snapshot & Commit Semantics

### 3.1 `SettingsSnapshot<TRoot>` = shadow copy + buffered store

```csharp
var snap = settings.Snapshot();           // shadow copy with CommittableSettingsStore
snap.Shadow.IsDarkMode = true;            // buffered in the store
snap.Shadow.FontSize = 16;
snap.Store.Commit();                      // flush to the real store
// snap.Store.Rollback();                 // or discard
```

Snapshot provides **read-your-own-writes** — after `SetValue`, `GetValue` on the same key returns the buffered value, not the underlying store's.

### 3.2 Commit reinitializes the root

`Commit()` flushes buffered writes to the underlying store, then calls `ISettingsRoot.Initialize(_store)` on the original root object. This re-pulls all values from the store (which now includes the committed writes).

**Rollback does not reinitialize.** It simply clears the pending buffer. The shadow retains its current in-memory values; a subsequent `Commit()` would flush whatever is in the buffer at that point.

### 3.3 Snapshot is not a transaction

`CommittableSettingsStore` is purely in-memory. There is no two-phase commit, no distributed transaction coordinator, and no rollback of the underlying store. If another actor writes to the underlying store between `Snapshot()` and `Commit()`, those writes are **overwritten** by the commit.

For genuine atomicity across processes, implement the commit logic yourself at the `ISettingsStore` level (e.g., file rename + fsync).

---

## 4. Property Backer Internals (for debugging)

### 4.1 Backer lifetime mirrors the settings object

Each `[Setting]` gets one `PropertyBacker<T>` field, created lazily on first `Initialize()` call. Subsequent `Initialize()` calls reuse the same backer — they only update the store reference and re-pull values.

### 4.2 Backer equality check prevents unnecessary change notifications

```csharp
public bool TrySet(T value)
{
    if (EqualityComparer<T>.Default.Equals(Value, value))
        return false;  // ← no PropertyChanged fired

    Value = value;
    _store.SetValue(_configKey, value);
    return true;
}
```

This means setting a property to its current value is a no-op: no store write, no `PropertyChanged`. This is correct for both runtime use and snapshot scenarios.

---

## 5. Testing

### 5.1 Unit test a settings class with `NoOpSettingsStore`

```csharp
var settings = new AppSettings();
((ISettingsRoot)settings).Initialize(new NoOpSettingsStore());

// Values come from fallbacks
Assert.False(settings.IsDarkMode);
Assert.Equal(14, settings.FontSize);
```

`NoOpSettingsStore` returns the fallback for every read and discards writes. This is the simplest way to test that the generated code compiles and initializes correctly.

### 5.2 Test store integration with a custom `ISettingsStore`

```csharp
// A minimal in-memory store for testing
var dict = new Dictionary<string, string>();
var store = new ReadWriteStore(dict);

var settings = new AppSettings();
((ISettingsRoot)settings).Initialize(store);

settings.IsDarkMode = true;
Assert.Equal("True", dict["IsDarkMode"]);

// Helper: ISettingsStore backed by a Dictionary<string, string>
public sealed class ReadWriteStore : ISettingsStore
{
    private readonly Dictionary<string, string> _dict;
    public ReadWriteStore(Dictionary<string, string> dict) => _dict = dict;

    public T GetValue<T>(string key, T fallback)
    {
        if (_dict.TryGetValue(key, out var raw))
            return (T)Convert.ChangeType(raw, typeof(T));
        return fallback;
    }

    public void SetValue<T>(string key, T value)
        => _dict[key] = Convert.ToString(value!)!;
}
```

### 5.3 Test `SettingsSnapshot` round-trip

```csharp
var snap = settings.Snapshot();
snap.Shadow.FontSize = 20;
Assert.Equal(20, snap.Shadow.FontSize);  // read-your-own-writes
Assert.Equal(14, settings.FontSize);     // original unchanged

snap.Store.Commit();
Assert.Equal(20, settings.FontSize);     // now propagated
```

### 5.4 Generator snapshot tests (Verify)

Snapshot tests in `tests/Everlong.Settings.Generators.Tests/Settings/` use Verify to capture generated source code. When you change the generator output:

```
1. dotnet test --filter "FullyQualifiedName~Settings"
2. Review *.received.cs files in Verified/
3. If output is correct: copy *.received.cs → *.verified.cs
4. Re-run tests to confirm green
```

---

## 6. CLI / Tooling

```bash
dotnet build                                    # Generates code + compiles
dotnet pack src/Everlong.Settings -c Release    # Produce the NuGet package
./publish.ps1                                   # Full build + pack (Release), outputs to ./publish/
dotnet test                                     # Run unit tests (25 tests covering generators, analyzers, code fixers)
```

No CLI tool is bundled with `Everlong.Settings` — settings are configured purely through attributes. The source generator runs automatically during `dotnet build`. There is no equivalent of `dotnet ef migrations add` or `dotnet elg gen` for settings; the attribute markup is the single source of truth.

---

## 7. Red Lines (Never Do This)

| Forbidden | Why |
|-----------|-----|
| `[Settings]` or `[Section]` on a non-`partial` class | Generator can't inject code; compilation fails with CS0260. The analyzer catches this at IDE time (ELST0007). |
| Never calling `((ISettingsRoot)settings).Initialize(store)` | The generator implements `ISettingsRoot` automatically, but if nobody calls `Initialize()`, backers never wire to a store. All settings silently return fallbacks at runtime. |
| `[Section]` property with a setter | The generator expects get-only partial properties. ELST2002 reports this at compile time. |
| `[Section]` property without `partial` | Generator can't emit the accessor body. ELST2004. |
| Passing a primitive fallback to a complex-typed `[Setting]` | Generator emits ELST2001 diagnostic. Complex types use `static partial GetXxxDefault()`. |
| Calling `ISettingsSection.Initialize()` manually | The generator emits `Initialize()` that wires backers correctly. Calling it yourself mid-lifecycle re-pulls values from the store and overwrites any unsaved changes. Use `Snapshot()` + `Commit()` for controlled updates. |
| Holding a reference to `PropertyBacker<T>` directly | `PropertyBacker` is an implementation detail of the generated code. Read/write settings through the partial property. |
| Subclassing `SettingsStoreBase` without calling `FlushAsync()` on shutdown | Background writes may be lost. Always `DisposeAsync()` the store when the application exits. |
| Sharing a single `ISettingsStore` instance across multiple unrelated setting roots without key prefix isolation | Keys collide silently. Use separate store instances, or prefix keys manually in your store implementation. |
| Hand-editing generated `*.Settings.g.cs` files | Overwritten on every build. Change attributes instead. |
| Using `SettingsSnapshot` across threads | `CommittableSettingsStore` is not thread-safe. Create snapshots on the same thread that uses them. |
| Writing a custom `ISettingsStore` that throws on every `SetValue` | The `PropertyBacker.TrySet` call goes unchecked — the exception surfaces at the point of write, not at a later flush. This is correct behaviour; catch it at the call site if you expect transient failures. |
| Relying on `NoOpSettingsStore` for anything beyond testing / design-time defaults | Writes are silently discarded. No data survives beyond the process lifetime. |
| Nesting `[Settings]` inside another `[Settings]` | A root settings class cannot be a nested section. Use `[Section]` for nesting. The generator treats `[Settings]` and `[Section]` on classes as mutually exclusive entry points. |
