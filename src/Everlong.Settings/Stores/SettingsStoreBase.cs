using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace Everlong.Settings;

/// <summary>
/// Thread-safe base implementation of <see cref="ISettingsStore"/> with an in-memory write-through
/// cache and an asynchronous, debounced write queue.
/// <para>
/// Subclasses implement <see cref="PersistAsync"/> for single-entry persistence, or override
/// <see cref="PersistBatchAsync"/> to handle an entire coalesced batch at once (preferred for
/// stores backed by a file or database).
/// </para>
/// </summary>
public abstract partial class SettingsStoreBase : ISettingsStore, IAsyncDisposable
{
  private readonly object _cacheGate = new();
  private Dictionary<string, string>? _cache;
  private Task? _flushTask;
  private bool _isCompleted;

  private readonly TimeSpan _writeDebounce;
  private readonly TimeSpan _maxBatchDelay;
  private readonly int _maxBatchSize;
  private readonly int _maxPersistRetries;

  private readonly Channel<(string key, string value)> _writeQueue =
    Channel.CreateUnbounded<(string, string)>(new UnboundedChannelOptions { SingleReader = true });

  private readonly Task _writerTask;

  /// <summary>
  /// Initializes a new instance of the <see cref="SettingsStoreBase"/> class.
  /// </summary>
  /// <param name="writeDebounce">The duration to wait for further writes before flushing the queue.</param>
  /// <param name="maxBatchDelay">The maximum delay before flushing a batch, even if the debounce period hasn't elapsed.</param>
  /// <param name="maxBatchSize">The maximum number of items to include in a single persistence batch.</param>
  /// <param name="maxPersistRetries">The maximum number of retry attempts for a failed persistence operation.</param>
  protected SettingsStoreBase(
    TimeSpan? writeDebounce = null,
    TimeSpan? maxBatchDelay = null,
    int maxBatchSize = 256,
    int maxPersistRetries = 3)
  {
    _writeDebounce = writeDebounce ?? TimeSpan.FromMilliseconds(250);
    _maxBatchDelay = maxBatchDelay ?? TimeSpan.FromSeconds(2);
    _maxBatchSize = Math.Max(1, maxBatchSize);
    _maxPersistRetries = Math.Max(0, maxPersistRetries);
    _writerTask = RunWriterAsync();
  }

  // ── Abstract / virtual persistence surface ────────────────────────────

  /// <summary>Loads all persisted key-value pairs from the underlying store.</summary>
  protected abstract IEnumerable<KeyValuePair<string, string>> LoadAll();

  /// <summary>Persists a single key-value pair. Called by the default <see cref="PersistBatchAsync"/> implementation.</summary>
  protected abstract Task PersistAsync(string key, string value, CancellationToken ct);

  /// <summary>
  /// Persists a coalesced batch of key-value pairs.
  /// Override this for stores that can write multiple entries efficiently (e.g., a single file flush
  /// or a database transaction). The default implementation persists entries one at a time via
  /// <see cref="PersistAsync"/>.
  /// </summary>
  protected virtual async Task PersistBatchAsync(IReadOnlyDictionary<string, string> batch, CancellationToken ct)
  {
    foreach (var (key, value) in batch)
      await PersistWithRetryAsync(key, value, ct).ConfigureAwait(false);
  }

  // ── Error hooks ───────────────────────────────────────────────────────

  /// <summary>Called when reading or deserializing a stored value fails.</summary>
  protected virtual void OnReadValueError(string configKey, string raw, Exception ex) { }

  /// <summary>Called when an in-memory value cannot be queued for persistence.</summary>
  protected virtual void OnWriteQueueRejected(string key, string value) { }

  /// <summary>Called when a single persist attempt fails (including retry attempts).</summary>
  protected virtual void OnPersistError(string key, string value, Exception ex, int attempt) { }

  /// <summary>Called when the background writer task terminates due to an unhandled exception.</summary>
  protected virtual void OnWriterFaulted(Exception ex) { }

  // ── ISettingsStore ────────────────────────────────────────────────────

  /// <inheritdoc/>
  public T GetValue<T>(string key, T fallback)
  {
    string? raw;
    lock (_cacheGate)
    {
      EnsureLoadedLocked();
      if (!_cache!.TryGetValue(key, out raw))
        return fallback;
    }

    try
    {
      if (IsJsonBackedType<T>())
      {
        var array = JsonSerializer.Deserialize(raw!, SettingsStoreJsonContext.Default.StringArray) ?? [];
        return (T)(object)array;
      }

      if (typeof(T).IsEnum)
      {
        return (T)Enum.Parse(typeof(T), raw!, ignoreCase: true);
      }

      return (T)Convert.ChangeType(raw, typeof(T));
    }
    catch (Exception ex)
    {
      OnReadValueError(key, raw!, ex);
      return fallback;
    }
  }

  /// <inheritdoc/>
  public void SetValue<T>(string key, T value)
  {
    var raw = IsJsonBackedType<T>()
                ? JsonSerializer.Serialize((string[])(object)value!, SettingsStoreJsonContext.Default.StringArray)
                : Convert.ToString(value)!;

    lock (_cacheGate)
    {
      EnsureLoadedLocked();

      if (_cache!.TryGetValue(key, out var existing) && existing == raw)
        return;

      _cache[key] = raw;

      if (_isCompleted || !_writeQueue.Writer.TryWrite((key, raw)))
        OnWriteQueueRejected(key, raw);
    }
  }

  // ── Flush / dispose ───────────────────────────────────────────────────

  /// <summary>
  /// Signals the write queue to complete and waits for all pending entries to be persisted.
  /// Safe to call multiple times; subsequent calls return the same task.
  /// </summary>
  public Task FlushAsync()
  {
    lock (_cacheGate)
    {
      _flushTask ??= FlushCoreAsync();
      return _flushTask;
    }
  }

  /// <inheritdoc/>
  public ValueTask DisposeAsync() => new(FlushAsync());

  // ── Background writer ─────────────────────────────────────────────────

  private async Task RunWriterAsync()
  {
    try
    {
      await ProcessWriteQueueAsync().ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      OnWriterFaulted(ex);
    }
  }

  private async Task ProcessWriteQueueAsync()
  {
    var pending = new Dictionary<string, string>(StringComparer.Ordinal);
    var reader = _writeQueue.Reader;

    while (await reader.WaitToReadAsync().ConfigureAwait(false))
    {
      while (reader.TryRead(out var item))
        pending[item.key] = item.value;

      var sw = Stopwatch.StartNew();

      // Debounce: keep merging while new writes arrive within the window.
      // The max-batch-delay cap prevents indefinite deferral under sustained write load.
      while (pending.Count < _maxBatchSize)
      {
        var elapsed = sw.Elapsed;
        if (elapsed >= _maxBatchDelay)
          break;

        var budget = _maxBatchDelay - elapsed;
        var window = budget <= _writeDebounce ? budget : _writeDebounce;
        if (window <= TimeSpan.Zero)
          break;

        var readTask = reader.WaitToReadAsync().AsTask();
        var completed = await Task.WhenAny(readTask, Task.Delay(window)).ConfigureAwait(false);
        if (completed != readTask || !await readTask.ConfigureAwait(false))
          break;

        while (reader.TryRead(out var next))
          pending[next.key] = next.value;
      }

      await PersistBatchAsync(pending, CancellationToken.None).ConfigureAwait(false);
      pending.Clear();
    }
  }

  private async Task PersistWithRetryAsync(string key, string value, CancellationToken ct)
  {
    for (var attempt = 0; ; attempt++)
    {
      try
      {
        await PersistAsync(key, value, ct).ConfigureAwait(false);
        return;
      }
      catch (Exception ex) when (attempt < _maxPersistRetries)
      {
        OnPersistError(key, value, ex, attempt);
        var backoff = TimeSpan.FromMilliseconds(Math.Min(2000, 100 * (1 << attempt)));
        await Task.Delay(backoff, ct).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        OnPersistError(key, value, ex, attempt);
        return;
      }
    }
  }

  private async Task FlushCoreAsync()
  {
    lock (_cacheGate)
    {
      if (_isCompleted) return;
      _isCompleted = true;
      _writeQueue.Writer.Complete();
    }

    await _writerTask.ConfigureAwait(false);
  }

  // ── Helpers ───────────────────────────────────────────────────────────

  private void EnsureLoadedLocked()
  {
    if (_cache is not null) return;
    _cache = LoadAll().ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
  }

  private static bool IsJsonBackedType<T>()
  {
    var t = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
    return t == typeof(string[]);
  }

  [JsonSerializable(typeof(string[]))]
  private sealed partial class SettingsStoreJsonContext : JsonSerializerContext;
}
