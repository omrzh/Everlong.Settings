namespace Everlong.Settings;

/// <summary>
/// An <see cref="ISettingsStore"/> that buffers writes locally and exposes
/// explicit <see cref="Commit"/> and <see cref="Rollback"/> operations.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ISettingsStore.GetValue{T}"/> checks the local pending buffer first,
/// then falls back to the wrapped underlying store — providing "read your own writes" semantics.
/// </para>
/// <para>
/// <see cref="ISettingsStore.SetValue{T}"/> accumulates changes in memory.
/// Duplicate writes to the same key are deduplicated (last write wins).
/// No data reaches the underlying store until <see cref="Commit"/> is called.
/// </para>
/// </remarks>
public interface ICommittableSettingsStore : ISettingsStore
{
  /// <summary>
  /// Flushes all pending writes to the underlying store and clears the local buffer.
  /// </summary>
  void Commit();

  /// <summary>
  /// Discards all pending writes without touching the underlying store.
  /// </summary>
  void Rollback();
}
