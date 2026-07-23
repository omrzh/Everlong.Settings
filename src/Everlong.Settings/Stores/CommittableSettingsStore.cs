namespace Everlong.Settings;

/// <summary>
/// Default implementation of <see cref="ICommittableSettingsStore"/>.
/// Wraps an existing <see cref="ISettingsStore"/> and buffers all writes locally
/// until <see cref="Commit"/> or <see cref="Rollback"/> is called.
/// </summary>
public sealed class CommittableSettingsStore : ICommittableSettingsStore
{
  private readonly ISettingsStore _store;
  private readonly ISettingsRoot _main;

  // Keyed by configKey. Stores the boxed in-memory value
  // (for GetValue reads) and a delegate that calls _store.SetValue<T> on Commit.
  private readonly Dictionary<string, (object? BoxedValue, Action CommitAction)>
    _pending = [];

  /// <param name="main"></param>
  /// <param name="store">The underlying store to which <see cref="Commit"/> will flush writes.</param>
  public CommittableSettingsStore(ISettingsRoot main, ISettingsStore store)
  {
    _main = main;
    _store = store;
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Checks the local pending buffer first (providing read-your-own-writes semantics),
  /// then falls back to the wrapped store.
  /// </remarks>
  public T GetValue<T>(string key, T fallback)
  {
    if (_pending.TryGetValue(key, out var entry))
    {
      try
      {
        return entry.BoxedValue is T typed ? typed : fallback;
      }
      catch
      {
        return fallback;
      }
    }

    return _store.GetValue(key, fallback);
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Buffers the writes locally. Duplicate writes to the same key are deduplicated (last write wins).
  /// The underlying store is not touched until <see cref="Commit"/> is called.
  /// </remarks>
  public void SetValue<T>(string key, T value)
  {
    _pending[key] = (value, () => _store.SetValue(key, value));
  }

  /// <inheritdoc/>
  public void Commit()
  {
    foreach (var (_, (_, commitAction)) in _pending)
      commitAction();

    _pending.Clear();
    _main.Initialize(_store);
  }

  /// <inheritdoc/>
  public void Rollback()
  {
    _pending.Clear();
  }
}
